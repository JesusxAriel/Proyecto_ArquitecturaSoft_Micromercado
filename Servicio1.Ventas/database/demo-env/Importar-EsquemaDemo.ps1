# Importacion explicitamente autorizada, una sola vez. Nunca crea ni borra bases.
[CmdletBinding()]
param([Parameter(Mandatory)][switch]$Importar)
$ErrorActionPreference = 'Stop'
if (-not $Importar) { throw 'Se requiere autorizacion explicita de importacion.' }
$contenedor = 'puntomarket-ventas-demo-mysql-demo-1'
$base = 'puntomarket_ventas_demo_local'
$estadoLocal = Join-Path $PSScriptRoot '.importacion-iniciada.local.json'
if (Test-Path -LiteralPath $estadoLocal) { throw 'Ya existe registro de intento. No se reimportara automaticamente.' }

function Consultar-Docker([string[]]$Argumentos) {
    $salida = & docker @Argumentos
    if ($LASTEXITCODE -ne 0) { throw 'Fallo una inspeccion Docker. No continuar.' }
    return ($salida -join "`n")
}
function Ejecutar-MySql([string]$Entrada) {
    # Password solo dentro del contenedor; SQL privado via stdin, nunca argumentos/archivos.
    $shell = @'
set -eu
export MYSQL_PWD="$(cat /run/secrets/ventas_demo_password)"
exec mysql --protocol=TCP --host=127.0.0.1 --user="$MYSQL_USER" --database=puntomarket_ventas_demo_local --default-character-set=utf8mb4 --batch --raw --skip-column-names --binary-mode --skip-reconnect
'@
    $inicio = [Diagnostics.ProcessStartInfo]::new()
    $inicio.FileName = (Get-Command docker -ErrorAction Stop).Source
    $inicio.UseShellExecute = $false
    $inicio.CreateNoWindow = $true
    $inicio.RedirectStandardInput = $true
    $inicio.RedirectStandardOutput = $true
    $inicio.RedirectStandardError = $true
    $inicio.StandardInputEncoding = [Text.UTF8Encoding]::new($false)
    $inicio.StandardOutputEncoding = [Text.UTF8Encoding]::new($false)
    $inicio.StandardErrorEncoding = [Text.UTF8Encoding]::new($false)
    foreach ($arg in @('exec', '-i', $contenedor, 'sh', '-c', $shell)) { $inicio.ArgumentList.Add($arg) }
    $proceso = [Diagnostics.Process]::new()
    $proceso.StartInfo = $inicio
    try {
        [void]$proceso.Start()
        $lecturaSalida = $proceso.StandardOutput.ReadToEndAsync()
        $lecturaError = $proceso.StandardError.ReadToEndAsync()
        $proceso.StandardInput.Write($Entrada)
        $proceso.StandardInput.Close()
        $proceso.WaitForExit()
        $salida = $lecturaSalida.GetAwaiter().GetResult()
        $errorSql = $lecturaError.GetAwaiter().GetResult()
        if ($proceso.ExitCode -ne 0) {
            # Nunca emitir el error bruto: podria incluir valores privados de una sentencia.
            $ubicacion = [regex]::Match($errorSql, 'ERROR\s+\d+\s+\([A-Z0-9]+\)\s+at line\s+\d+')
            $detalle = if ($ubicacion.Success) { $ubicacion.Value } else { 'Error de cliente/conexion; salida privada retenida, no publicada' }
            throw "MySQL fallo (exit=$($proceso.ExitCode)): $detalle. Detenerse sin reintentar."
        }
        return [PSCustomObject]@{ Salida = $salida; Advertencias = $errorSql }
    } finally { $proceso.Dispose() }
}
function Filas([string]$Texto) { return @($Texto -split '\r?\n' | Where-Object { $_.Length -gt 0 }) }

$contexto = Consultar-Docker @('context', 'show')
if ($contexto.Trim() -ne 'desktop-linux') { throw 'Contexto Docker inesperado.' }
$c = @((Consultar-Docker @('inspect', $contenedor)) | ConvertFrom-Json)[0]
if ($c.State.Status -ne 'running' -or $c.State.Health.Status -ne 'healthy' -or $c.Config.Labels.'com.docker.compose.project' -ne 'puntomarket-ventas-demo' -or $c.Config.Labels.'com.docker.compose.service' -ne 'mysql-demo') { throw 'Identidad o salud del contenedor incorrectas.' }
if ([IO.Path]::GetFullPath($c.Config.Labels.'com.docker.compose.project.working_dir') -ne [IO.Path]::GetFullPath($PSScriptRoot)) { throw 'Directorio Compose inesperado.' }
$puertos = @($c.NetworkSettings.Ports.'3306/tcp')
if ($puertos.Count -ne 1 -or $puertos[0].HostIp -ne '127.0.0.1' -or $puertos[0].HostPort -ne '3308' -or @($c.NetworkSettings.Ports.PSObject.Properties | Where-Object { $null -ne $_.Value }).Count -ne 1) { throw 'Puerto no aislado.' }
if (@($c.NetworkSettings.Networks.PSObject.Properties).Count -ne 1 -or @($c.NetworkSettings.Networks.PSObject.Properties)[0].Name -ne 'puntomarket-ventas-demo_ventas-demo-net') { throw 'Red inesperada.' }
$datos = @($c.Mounts | Where-Object Destination -eq '/var/lib/mysql')
if ($datos.Count -ne 1 -or $datos[0].Name -ne 'puntomarket-ventas-demo_ventas-demo-data') { throw 'Volumen inesperado.' }
& (Join-Path $PSScriptRoot 'Validar-ConfiguracionPrivada.ps1')
if ($LASTEXITCODE -ne 0) { throw 'Configuracion privada invalida.' }
& (Join-Path $PSScriptRoot 'Validar-Preparacion.ps1') -OmitirCompose
if ($LASTEXITCODE -ne 0) { throw 'Copia SQL invalida.' }
$vars = @{}
foreach ($linea in [IO.File]::ReadAllLines((Join-Path $PSScriptRoot '.env'))) {
    if ($linea -match '^(VENTAS_DEMO_[A-Z_]+)=(.*)$') { $vars[$Matches[1]] = $Matches[2] }
}
$admin = [IO.File]::ReadAllText($vars.VENTAS_DEMO_ADMIN_HASH_FILE)
$cajero = [IO.File]::ReadAllText($vars.VENTAS_DEMO_CAJERO_HASH_FILE)
$hexAdmin = [Convert]::ToHexString([Text.Encoding]::UTF8.GetBytes($admin))
$hexCajero = [Convert]::ToHexString([Text.Encoding]::UTF8.GetBytes($cajero))
$sql = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'sql/PuntoMarket.Ventas.Demo.sql'))
$comprobarVacia = @'
SELECT DATABASE(),CURRENT_USER(),(SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE()),(SELECT COUNT(*) FROM information_schema.TRIGGERS WHERE TRIGGER_SCHEMA=DATABASE()),(SELECT COUNT(*) FROM information_schema.ROUTINES WHERE ROUTINE_SCHEMA=DATABASE()),(SELECT COUNT(*) FROM information_schema.EVENTS WHERE EVENT_SCHEMA=DATABASE()),@@global.log_bin_trust_function_creators,@@session.foreign_key_checks,@@global.general_log,@@global.slow_query_log;
SHOW GRANTS FOR CURRENT_USER;
'@
$previo = Ejecutar-MySql $comprobarVacia
$lineas = Filas $previo.Salida
$f = $lineas[0] -split "`t"
if ($f.Count -ne 10 -or $f[0] -ne $base -or $f[1] -notlike 'ventas_demo@*' -or @($f[2..5] | Where-Object { $_ -ne '0' }).Count -gt 0) { throw 'Base inesperada o no vacia: no importar.' }
if ($f[6] -ne '1' -or $f[7] -ne '1' -or $f[8] -ne '0' -or $f[9] -ne '0') { throw 'Configuracion de permisos, FK o logs incompatible con la importacion privada.' }
$grant = @($lineas | Where-Object { ($_ -replace '\\+_', '_') -match '^GRANT ALL PRIVILEGES ON `puntomarket_ventas_demo_local`\.\* TO ' })
if ($grant.Count -ne 1) { throw 'Permisos de importacion no confirmados.' }
Write-Output 'Preimportacion correcta: contenedor healthy, destino Demo vacio, FK activas y variables privadas listas.'

# Marca persistente antes del unico intento. No contiene secretos y esta ignorada.
$registro = [ordered]@{ estado = 'INICIADA'; contenedor = $contenedor; base = $base; fechaUtc = [DateTime]::UtcNow.ToString('o'); copiaSha256 = (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot 'sql/PuntoMarket.Ventas.Demo.sql')).Hash }
$marca = [IO.File]::Open($estadoLocal, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
try {
    $bytes = [Text.Encoding]::UTF8.GetBytes(($registro | ConvertTo-Json))
    $marca.Write($bytes, 0, $bytes.Length)
} finally { $marca.Dispose() }
try {
    # Una sola llamada de importacion; los SET y el archivo viajan por el mismo stdin/sesion.
    $entrada = "SET @ventas_demo_admin_password_hash=CONVERT(0x$hexAdmin USING utf8mb4);`nSET @ventas_demo_cajero_password_hash=CONVERT(0x$hexCajero USING utf8mb4);`n" + $sql
    $importacion = Ejecutar-MySql $entrada
    $registro.estado = 'IMPORTADA'
    $registro.advertenciasCliente = -not [string]::IsNullOrWhiteSpace($importacion.Advertencias)
    $registro.salidaCliente = -not [string]::IsNullOrWhiteSpace($importacion.Salida)
    [IO.File]::WriteAllText($estadoLocal, ($registro | ConvertTo-Json), [Text.UTF8Encoding]::new($false))
    Write-Output 'Importacion unica completada: codigo de salida 0. No se usaron --force ni reintentos.'
    Write-Output ('Advertencias de cliente presentes: ' + $registro.advertenciasCliente + '; salida adicional presente: ' + $registro.salidaCliente)
} catch {
    $registro.estado = 'FALLIDA_NO_REINTENTAR'
    $registro.errorSeguro = $_.Exception.Message
    [IO.File]::WriteAllText($estadoLocal, ($registro | ConvertTo-Json), [Text.UTF8Encoding]::new($false))
    throw
} finally { $entrada = $null; $admin = $null; $cajero = $null; $hexAdmin = $null; $hexCajero = $null; $sql = $null }
