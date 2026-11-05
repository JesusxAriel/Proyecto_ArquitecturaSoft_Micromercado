#requires -Version 7.0
# PREPARADO, NO EJECUTADO. Ejecutar únicamente después de autorización.
[CmdletBinding()]
param([switch]$Ejecutar, [Parameter(Mandatory=$true)][string]$DirectorioPrivado)
$ErrorActionPreference = 'Stop'
if (-not $Ejecutar) { throw 'No se ejecutó nada. Se requiere autorización y -Ejecutar.' }
function MysqlPrivado([string]$Sql) {
    $inicio = [Diagnostics.ProcessStartInfo]::new()
    $inicio.FileName = (Get-Command docker).Source
    $inicio.UseShellExecute = $false
    $inicio.CreateNoWindow = $true
    $inicio.RedirectStandardInput = $true
    $inicio.RedirectStandardOutput = $true
    $inicio.RedirectStandardError = $true
    foreach ($arg in @('exec','-i','puntomarket-ventas-demo-mysql-demo-1','sh','-c', 'export MYSQL_PWD="$(cat /run/secrets/ventas_demo_root_password)"; exec mysql --user=root --database=puntomarket_ventas_demo_local --batch --skip-column-names')) { $inicio.ArgumentList.Add($arg) }
    $proceso = [Diagnostics.Process]::new(); $proceso.StartInfo=$inicio
    try {
        [void]$proceso.Start()
        $salida=$proceso.StandardOutput.ReadToEndAsync()
        $errores=$proceso.StandardError.ReadToEndAsync()
        $proceso.StandardInput.Write($Sql); $proceso.StandardInput.Close()
        $proceso.WaitForExit()
        $resultado=$salida.GetAwaiter().GetResult(); [void]$errores.GetAwaiter().GetResult()
        if ($proceso.ExitCode -ne 0) { throw 'Error SQL. Detenido sin reintentos; no se publica salida privada.' }
        return $resultado.Trim()
    } finally { $proceso.Dispose() }
}
function Test-PermisosUsuariosDemo([string]$Metadatos) {
    $usuario = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $rol = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $lineas = @($Metadatos -split "\r?\n" | Where-Object { $_.Trim().Length -gt 0 } | ForEach-Object { $_.Trim() })
    # TLS procede de mysql.user.ssl_type, no del texto de SHOW GRANTS.
    if (@($lineas | Where-Object { $_ -eq 'TLS_TYPE=ANY' }).Count -ne 1) { return $false }
    if (@($lineas | Where-Object { $_ -eq 'ROLES_OK=1' }).Count -ne 1) { return $false }
    foreach ($linea in $lineas) {
        if ($linea -in @('TLS_TYPE=ANY','ROLES_OK=1')) { continue }
        if ($linea -eq 'GRANT USAGE ON *.* TO `usuarios_demo`@`172.20.0.1`') { continue }
        $m = [regex]::Match($linea, '^GRANT ([A-Z, ]+) ON `puntomarket_ventas_demo_local`\.`(USUARIO|ROL)` TO `usuarios_demo`@`172\.20\.0\.1`$', [Text.RegularExpressions.RegexOptions]::IgnoreCase)
        if (-not $m.Success) { return $false }
        $permisos = $usuario
        if ($m.Groups[2].Value -ieq 'ROL') { $permisos = $rol }
        foreach ($permiso in $m.Groups[1].Value.Split(',', [StringSplitOptions]::TrimEntries)) { [void]$permisos.Add($permiso) }
    }
    return ($usuario.SetEquals([string[]]@('SELECT','INSERT','UPDATE')) -and $rol.SetEquals([string[]]@('SELECT')))
}
$etapa = 'validar-ruta'
$directorioCreado = $false
$sqlActivacionEnviado = $false
try {
    $workspace=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..')) + [IO.Path]::DirectorySeparatorChar
    if (-not [IO.Path]::IsPathFullyQualified($DirectorioPrivado)) { throw 'Use una ruta absoluta privada.' }
    $privado=[IO.Path]::GetFullPath($DirectorioPrivado)
    if ($privado.StartsWith($workspace,[StringComparison]::OrdinalIgnoreCase)) { throw 'La carpeta debe estar fuera del workspace.' }
    # Carpeta NUEVA para no sobrescribir ACL ni secretos de otras cuentas.
    $etapa = 'comprobar-directorio-existente'
    if (Test-Path -LiteralPath $privado) { throw 'La carpeta ya existe: detener para no sobrescribir secretos o permisos.' }
    $etapa = 'comprobar-contenedor'
    $salud=& docker inspect --format '{{.State.Status}}|{{if .State.Health}}{{.State.Health.Status}}{{end}}' puntomarket-ventas-demo-mysql-demo-1
    if ($LASTEXITCODE -ne 0 -or $salud -ne 'running|healthy') { throw 'Contenedor no healthy.' }
    $etapa = 'verificar-origen-windows-tls'
    $origen=& dotnet run --project (Join-Path $PSScriptRoot 'VerificarOrigen') --no-build --no-restore
    if ($LASTEXITCODE -ne 0 -or -not ($origen -match 'Origen Windows visto por MySQL: 172\.20\.0\.1;')) { throw 'Host Windows distinto o no verificable.' }
    $etapa = 'comprobar-mysql-cuenta-logs-y-tablas'
    $comprobacion=MysqlPrivado @"
SELECT IF(VERSION() LIKE '8.4.%' AND DATABASE()='puntomarket_ventas_demo_local'
 AND @@GLOBAL.general_log=0 AND @@GLOBAL.slow_query_log=0 AND @@GLOBAL.log_raw=0
 AND (SELECT COUNT(*) FROM mysql.user WHERE User='usuarios_demo')=0
 AND (SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME IN ('USUARIO','ROL'))=2
 AND (SELECT COUNT(*) FROM information_schema.PLUGINS WHERE PLUGIN_TYPE='AUDIT' AND PLUGIN_STATUS='ACTIVE' AND PLUGIN_NAME NOT IN ('sha2_cache_cleaner','mysqlx_cache_cleaner'))=0,1,0);
"@
    if ($comprobacion -ne '1') { throw 'Cuenta existente, logs/auditoría o precondiciones incompatibles. Sin cambios.' }
    $etapa = 'cargar-bibliotecas-mysql'
    $bin=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../tools/PuntoMarket.Usuarios.DemoLocal/bin/Debug/net10.0/win-x64'))
    foreach ($nombre in @('System.Configuration.ConfigurationManager','System.Security.Permissions','System.Security.Cryptography.ProtectedData','Google.Protobuf','K4os.Compression.LZ4','K4os.Compression.LZ4.Streams','K4os.Hash.xxHash','ZstdSharp','BouncyCastle.Cryptography','MySql.Data')) {
        $dll=Join-Path $bin ($nombre+'.dll'); if (Test-Path -LiteralPath $dll) { [void][Reflection.Assembly]::LoadFrom($dll) }
    }
    $etapa = 'validar-plantilla-sql'
    $sql=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Preparar-CuentaDemo.sql') -Raw
    if (-not $sql.Contains('BY __PM_PASSWORD_SQL_LITERAL__ REQUIRE SSL')) { throw 'Plantilla inesperada.' }
    $etapa = 'crear-directorio-privado'
    [void](New-Item -ItemType Directory -Path $privado)
    $directorioCreado = $true
    $etapa = 'proteger-directorio-acl'
    $acl=[Security.AccessControl.DirectorySecurity]::new()
    $acl.SetAccessRuleProtection($true,$false)
    $sid=[Security.Principal.WindowsIdentity]::GetCurrent().User
    $acl.SetOwner($sid)
    $regla=[Security.AccessControl.FileSystemAccessRule]::new($sid,'FullControl','ContainerInherit,ObjectInherit','None','Allow')
    $acl.AddAccessRule($regla); Set-Acl -LiteralPath $privado -AclObject $acl
    # Generada en memoria: no se introduce en consola/historial ni en archivos SQL.
    $etapa = 'generar-secreto-en-memoria'
    $password=[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
    $segura=ConvertTo-SecureString $password -AsPlainText -Force
    $etapa = 'guardar-secreto-dpapi'
    ConvertFrom-SecureString $segura | Set-Content -LiteralPath (Join-Path $privado 'usuarios-password.dpapi') -NoNewline
    $etapa = 'construir-conexion-privada'
    $cadena=[MySql.Data.MySqlClient.MySqlConnectionStringBuilder]::new()
    $cadena.Server='127.0.0.1'; $cadena.Port=3308; $cadena.Database='puntomarket_ventas_demo_local'
    $cadena.UserID='usuarios_demo'; $cadena.Password=$password; $cadena.SslMode=[MySql.Data.MySqlClient.MySqlSslMode]::Required; $cadena.ConnectionTimeout=10
    $etapa = 'guardar-archivo-conexion'
    [IO.File]::WriteAllText((Join-Path $privado 'usuarios-connection.txt'),$cadena.ConnectionString)
    # Base64 no contiene comillas ni barras invertidas; literal SQL delimitado seguro.
    $etapa = 'preparar-sql-en-memoria'
    $sql=$sql.Replace('BY __PM_PASSWORD_SQL_LITERAL__ REQUIRE SSL',("BY '"+$password+"' REQUIRE SSL"))
    $etapa = 'ejecutar-creacion-y-permisos'
    $sqlActivacionEnviado = $true
    $resultado=MysqlPrivado $sql
    $etapa = 'verificar-permisos-resultantes'
    $resultado = MysqlPrivado @"
SHOW GRANTS FOR 'usuarios_demo'@'172.20.0.1';
SELECT CONCAT('TLS_TYPE=',ssl_type) FROM mysql.user WHERE User='usuarios_demo' AND Host='172.20.0.1';
SELECT CONCAT('ROLES_OK=',IF(@@GLOBAL.mandatory_roles='' AND
 (SELECT COUNT(*) FROM mysql.role_edges WHERE TO_USER='usuarios_demo')=0 AND
 (SELECT COUNT(*) FROM mysql.default_roles WHERE USER='usuarios_demo')=0 AND
 (SELECT COUNT(*) FROM mysql.global_grants WHERE USER='usuarios_demo')=0 AND
 (SELECT COUNT(*) FROM mysql.proxies_priv WHERE User='usuarios_demo')=0,1,0));
"@
    if (-not (Test-PermisosUsuariosDemo $resultado)) { throw 'Revisar metadatos de permisos y TLS; detener sin reintentos.' }
    Write-Output 'Cuenta creada y grants verificados. Conexión guardada en carpeta privada. No se muestran secretos.'
} catch {
    # Solo etiquetas fijas y estado del flujo. Nunca publicar $_, Message, stack,
    # SQL, stderr del cliente ni objetos que puedan contener secretos.
    $motivos = @{
        'validar-ruta' = 'Use una ruta absoluta fuera del workspace.'
        'comprobar-directorio-existente' = 'El directorio ya existe o no se pudo comprobar. No se sobrescribe.'
        'comprobar-contenedor' = 'Docker no accesible o contenedor no healthy.'
        'verificar-origen-windows-tls' = 'Comprobador no disponible, conexión TLS fallida o dirección de origen distinta.'
        'comprobar-mysql-cuenta-logs-y-tablas' = 'Falló la conexión administrativa o alguna precondición: cuenta existente, logs, auditoría, versión, base o tablas.'
        'cargar-bibliotecas-mysql' = 'Bibliotecas MySQL no disponibles o incompatibles; revisar build del lanzador.'
        'validar-plantilla-sql' = 'Plantilla SQL no disponible o distinta de la esperada.'
        'crear-directorio-privado' = 'No se pudo crear el directorio; revisar ruta y permisos Windows.'
        'proteger-directorio-acl' = 'No se pudieron establecer las ACL privadas. No se generó el secreto.'
        'generar-secreto-en-memoria' = 'Falló la generación criptográfica o conversión segura.'
        'guardar-secreto-dpapi' = 'Falló la protección DPAPI o escritura del archivo privado.'
        'construir-conexion-privada' = 'No se pudo construir la conexión; revisar biblioteca MySQL.'
        'guardar-archivo-conexion' = 'No se pudo guardar la conexión privada; revisar ACL y almacenamiento.'
        'preparar-sql-en-memoria' = 'No se pudo preparar la plantilla en memoria.'
        'ejecutar-creacion-y-permisos' = 'Falló el cliente administrativo o la ejecución SQL; puede existir estado parcial.'
        'verificar-permisos-resultantes' = 'La salida no confirma todos los permisos esperados; revisar estado parcial.'
    }
    [Console]::Error.WriteLine("Activación detenida. Etapa: $etapa. " + $motivos[$etapa])
    if ($sqlActivacionEnviado) {
        [Console]::Error.WriteLine('Se intentó enviar SQL de activación. Consultar cuenta y permisos antes de reintentar; no cambiar contraseñas ni borrar archivos privados.')
    } else {
        [Console]::Error.WriteLine('No se envió SQL de activación: este intento no ejecutó CREATE USER ni GRANT.')
    }
    if ($directorioCreado) { [Console]::Error.WriteLine('Se creó el directorio privado. Conservarlo e inspeccionar sus permisos y archivos antes de reintentar.') }
    exit 1
}
finally { $password=$null; $sql=$null; $cadena=$null; $resultado=$null; if ($segura) { $segura.Dispose() } }
