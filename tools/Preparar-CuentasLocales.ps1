#requires -Version 7.0
# OPCIONAL, instalacion NUEVA. No ejecutar en el entorno activo.
[CmdletBinding()]
param([switch]$Aplicar)
$ErrorActionPreference = 'Stop'
if (-not $Aplicar) { throw 'No se ejecuto nada. Revisar README; -Aplicar solo para una instalacion nueva.' }
$privado = Join-Path $env:USERPROFILE 'puntomarket-demo-secrets'
$reportesRuta = Join-Path $privado 'reportes-connection.txt'
$usuariosRuta = Join-Path $privado 'usuarios-connection.txt'
if (-not (Test-Path -LiteralPath $privado) -or (Test-Path -LiteralPath $reportesRuta) -or (Test-Path -LiteralPath $usuariosRuta)) {
    throw 'Falta preparacion privada o ya existen conexiones. No se sobrescribe un estado parcial.'
}
$acl = Get-Acl -LiteralPath $privado
$sid = [Security.Principal.WindowsIdentity]::GetCurrent().User.Value
if (-not $acl.AreAccessRulesProtected -or @($acl.Access | Where-Object {
    $_.AccessControlType -eq 'Allow' -and $_.IdentityReference.Translate([Security.Principal.SecurityIdentifier]).Value -notin @($sid,'S-1-5-18','S-1-5-32-544')
}).Count -gt 0) { throw 'ACL privada no verificable. Sin cambios.' }
$salud = & docker inspect --format '{{.State.Status}}|{{if .State.Health}}{{.State.Health.Status}}{{end}}' puntomarket-ventas-demo-mysql-demo-1
if ($LASTEXITCODE -ne 0 -or $salud -ne 'running|healthy') { throw 'Contenedor Demo no healthy.' }
$binRaiz = Join-Path $PSScriptRoot '../Servicio2.Usuarios/tools/PuntoMarket.Usuarios.DemoLocal/bin'
$dll = Get-ChildItem -LiteralPath $binRaiz -Filter MySql.Data.dll -Recurse -File | Select-Object -First 1
if (-not $dll) { throw 'Compile primero PuntoMarket.slnx.' }
foreach ($nombre in @('System.Configuration.ConfigurationManager','System.Security.Permissions','System.Security.Cryptography.ProtectedData','Google.Protobuf','K4os.Compression.LZ4','K4os.Compression.LZ4.Streams','K4os.Hash.xxHash','ZstdSharp','BouncyCastle.Cryptography','MySql.Data')) {
    $ruta = Join-Path $dll.DirectoryName ($nombre + '.dll')
    if (Test-Path -LiteralPath $ruta) { [void][Reflection.Assembly]::LoadFrom($ruta) }
}
function MySql-Administrador([string]$Sql) {
    $pi = [Diagnostics.ProcessStartInfo]::new('docker')
    $pi.UseShellExecute = $false; $pi.CreateNoWindow = $true
    $pi.RedirectStandardInput = $true; $pi.RedirectStandardOutput = $true; $pi.RedirectStandardError = $true
    $pi.StandardInputEncoding = [Text.UTF8Encoding]::new($false)
    $shell = 'export MYSQL_PWD="$(cat /run/secrets/ventas_demo_root_password)"; exec mysql --user=root --database=puntomarket_ventas_demo_local --batch --skip-column-names --skip-reconnect'
    foreach ($a in @('exec','-i','puntomarket-ventas-demo-mysql-demo-1','sh','-c',$shell)) { $pi.ArgumentList.Add($a) }
    $p = [Diagnostics.Process]::new(); $p.StartInfo = $pi
    try {
        [void]$p.Start(); $salida = $p.StandardOutput.ReadToEndAsync(); $errorSql = $p.StandardError.ReadToEndAsync()
        $p.StandardInput.Write($Sql); $p.StandardInput.Close(); $p.WaitForExit()
        $texto = $salida.GetAwaiter().GetResult(); [void]$errorSql.GetAwaiter().GetResult()
        if ($p.ExitCode -ne 0) { throw 'SQL administrativo fallo. Revisar estado parcial sin reintentar; salida privada omitida.' }
        return $texto.Trim()
    } finally { $p.Dispose() }
}
function Escribir-Nuevo([string]$Ruta,[string]$Valor) {
    $f = [IO.File]::Open($Ruta,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
    try { $bytes = [Text.UTF8Encoding]::new($false).GetBytes($Valor); $f.Write($bytes,0,$bytes.Length) }
    finally { if ($bytes) {[Array]::Clear($bytes)}; $f.Dispose() }
}
$conexion = $null; $sql = $null
try {
    $cs = [MySql.Data.MySqlClient.MySqlConnectionStringBuilder]::new()
    $cs.Server='127.0.0.1'; $cs.Port=3308; $cs.Database='puntomarket_ventas_demo_local'; $cs.UserID='ventas_demo'
    $cs.Password=[IO.File]::ReadAllText((Join-Path $privado 'mysql-ventas.txt')); $cs.SslMode='Required'; $cs.ConnectionTimeout=10
    $conexion = [MySql.Data.MySqlClient.MySqlConnection]::new($cs.ConnectionString); $conexion.Open()
    $cmd = $conexion.CreateCommand(); $cmd.CommandText="SELECT SUBSTRING_INDEX(USER(),'@',-1)"; $hostMySql = [string]$cmd.ExecuteScalar(); $cmd.Dispose()
    if ($hostMySql -notmatch '^[A-Za-z0-9_.:-]+$') { throw 'Host Windows no verificable; no usar comodines.' }
    $cmd = $conexion.CreateCommand(); $cmd.CommandText="SHOW SESSION STATUS LIKE 'Ssl_cipher'"; $lector=$cmd.ExecuteReader()
    if (-not $lector.Read() -or [string]::IsNullOrWhiteSpace($lector.GetString(1))) { throw 'TLS no confirmado.' }
    $lector.Dispose(); $cmd.Dispose()
    # Bloquea cuentas previas bajo cualquier host y logs SQL/auditoria activos.
    $previo = MySql-Administrador @'
SELECT IF(DATABASE()='puntomarket_ventas_demo_local' AND VERSION() LIKE '8.4.%'
 AND @@GLOBAL.general_log=0 AND @@GLOBAL.slow_query_log=0 AND @@GLOBAL.log_raw=0
 AND (SELECT COUNT(*) FROM mysql.user WHERE User IN ('usuarios_demo','reportes_demo'))=0
 AND (SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME IN ('USUARIO','ROL'))=2
 AND (SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='USUARIO' AND COLUMN_NAME='idUsuarioActualizacion')=1
 AND (SELECT COUNT(*) FROM information_schema.PLUGINS WHERE PLUGIN_TYPE='AUDIT' AND PLUGIN_STATUS='ACTIVE' AND PLUGIN_NAME NOT IN ('sha2_cache_cleaner','mysqlx_cache_cleaner'))=0,1,0);
'@
    if ($previo -ne '1') { throw 'Cuenta existente, esquema inesperado o logs activos. Sin cambios.' }
    $passwordUsuarios = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(36))
    $passwordReportes = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(36))
    $cs.UserID='usuarios_demo'; $cs.Password=$passwordUsuarios; Escribir-Nuevo $usuariosRuta $cs.ConnectionString
    $cs.UserID='reportes_demo'; $cs.Password=$passwordReportes; Escribir-Nuevo $reportesRuta $cs.ConnectionString
    # Credenciales Base64 sin comillas; host validado. Solo stdin, no argumentos/logs.
    $sql = @"
CREATE USER 'usuarios_demo'@'$hostMySql' IDENTIFIED BY '$passwordUsuarios' REQUIRE SSL;
GRANT SELECT,INSERT,UPDATE ON puntomarket_ventas_demo_local.USUARIO TO 'usuarios_demo'@'$hostMySql';
GRANT SELECT ON puntomarket_ventas_demo_local.ROL TO 'usuarios_demo'@'$hostMySql';
CREATE USER 'reportes_demo'@'$hostMySql' IDENTIFIED BY '$passwordReportes' REQUIRE SSL;
GRANT SELECT ON puntomarket_ventas_demo_local.* TO 'reportes_demo'@'$hostMySql';
"@
    [void](MySql-Administrador $sql)
    Write-Output 'Cuentas independientes creadas con TLS; conexiones privadas preparadas. Sin cambios en registros de negocio.'
} catch { throw 'Preparacion detenida. Puede existir estado parcial: conservar archivos privados y revisar antes de reintentar. Detalles omitidos.' }
finally { if ($conexion) {$conexion.Dispose()}; $sql=$null; $passwordUsuarios=$null; $passwordReportes=$null; $cs=$null }
