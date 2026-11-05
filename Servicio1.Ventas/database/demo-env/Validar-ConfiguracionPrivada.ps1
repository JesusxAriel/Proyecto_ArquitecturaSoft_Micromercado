# Solo validaciones: no abre conexiones, no inicia Docker y no ejecuta SQL.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$rutaEnv = Join-Path $PSScriptRoot '.env'
if (-not (Test-Path -LiteralPath $rutaEnv -PathType Leaf)) { throw 'Falta .env local.' }
$variables = @{}
foreach ($linea in [IO.File]::ReadAllLines($rutaEnv)) {
    if ([string]::IsNullOrWhiteSpace($linea) -or $linea.TrimStart().StartsWith('#')) { continue }
    if ($linea -notmatch '^(VENTAS_DEMO_[A-Z_]+)=(.*)$') { throw 'Formato .env no admitido; no se muestra su contenido.' }
    if ($variables.ContainsKey($Matches[1])) { throw 'Variable duplicada en .env.' }
    $variables[$Matches[1]] = $Matches[2]
}
$nombres = @('VENTAS_DEMO_USER', 'VENTAS_DEMO_ROOT_PASSWORD_FILE', 'VENTAS_DEMO_PASSWORD_FILE', 'VENTAS_DEMO_ADMIN_HASH_FILE', 'VENTAS_DEMO_CAJERO_HASH_FILE')
if ($variables.Count -ne $nombres.Count) { throw 'Se requieren exactamente cinco variables de configuracion, sin secretos literales.' }
foreach ($nombre in $nombres) {
    if ([string]::IsNullOrWhiteSpace($variables[$nombre])) { throw "Falta variable $nombre." }
}
if ($variables.VENTAS_DEMO_USER -ne 'ventas_demo') { throw 'Usuario inesperado para esta configuracion local.' }
$privado = [IO.Path]::GetFullPath((Join-Path $env:USERPROFILE 'puntomarket-demo-secrets')) + [IO.Path]::DirectorySeparatorChar
$faltantes = @()
foreach ($nombre in $nombres | Where-Object { $_ -ne 'VENTAS_DEMO_USER' }) {
    $ruta = $variables[$nombre]
    if (-not [IO.Path]::IsPathRooted($ruta) -or -not [IO.Path]::GetFullPath($ruta).StartsWith($privado, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Ruta privada externa inesperada para $nombre."
    }
    if (-not (Test-Path -LiteralPath $ruta -PathType Leaf)) { $faltantes += $nombre; continue }
    $item = Get-Item -LiteralPath $ruta
    if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'No se aceptan enlaces en los archivos privados.' }
    if ($item.Length -eq 0) { throw "Archivo privado vacio: $nombre." }
    Write-Output "$nombre : archivo presente y no vacio."
}
if ($faltantes.Count -gt 0) {
    Write-Output ('Configuracion incompleta. Faltan archivos de: ' + ($faltantes -join ', '))
    exit 2
}
# Cada clon genera hashes propios: no se comparan con credenciales publicadas.
foreach ($par in @('VENTAS_DEMO_ADMIN_HASH_FILE','VENTAS_DEMO_CAJERO_HASH_FILE')) {
    $hashPrivado = [IO.File]::ReadAllText($variables[$par])
    if ($hashPrivado -notmatch '^\$2[aby]\$\d{2}\$[./A-Za-z0-9]{53}$') { throw 'Hash BCrypt privado invalido.' }
    $hashPrivado = $null
}
$rootPassword = [IO.File]::ReadAllText($variables.VENTAS_DEMO_ROOT_PASSWORD_FILE)
$ventasPassword = [IO.File]::ReadAllText($variables.VENTAS_DEMO_PASSWORD_FILE)
try {
    if ([string]::IsNullOrWhiteSpace($rootPassword) -or [string]::IsNullOrWhiteSpace($ventasPassword) -or $rootPassword -ceq $ventasPassword) { throw 'Se requieren contrasenas no vacias y distintas.' }
} finally { $rootPassword = $null; $ventasPassword = $null }
# Solo el modelo resuelto: no necesita contactar el motor ni imprimir secretos.
$modeloJson = & docker compose --project-name puntomarket-ventas-demo --env-file $rutaEnv -f (Join-Path $PSScriptRoot 'compose.yaml') config --format json
if ($LASTEXITCODE -ne 0) { throw 'Compose no pudo resolver la configuracion privada.' }
$modelo = ($modeloJson -join "`n") | ConvertFrom-Json
if ($modelo.name -ne 'puntomarket-ventas-demo' -or @($modelo.services.PSObject.Properties).Count -ne 1) { throw 'Proyecto o servicios inesperados.' }
$mysql = $modelo.services.'mysql-demo'
if ($mysql.environment.MYSQL_DATABASE -ne 'puntomarket_ventas_demo_local' -or $mysql.environment.MYSQL_USER -ne 'ventas_demo') { throw 'Destino o usuario inesperados.' }
if (@($mysql.ports).Count -ne 1 -or $mysql.ports[0].host_ip -ne '127.0.0.1' -or $mysql.ports[0].published -ne '3308') { throw 'Publicacion de puerto inesperada.' }
if ([IO.Path]::GetFullPath($modelo.secrets.ventas_demo_root_password.file) -ne [IO.Path]::GetFullPath($variables.VENTAS_DEMO_ROOT_PASSWORD_FILE) -or [IO.Path]::GetFullPath($modelo.secrets.ventas_demo_password.file) -ne [IO.Path]::GetFullPath($variables.VENTAS_DEMO_PASSWORD_FILE)) { throw 'Compose no resolvio las rutas privadas esperadas.' }
Write-Output 'Hashes privados BCrypt validos; contrasenas privadas presentes; Compose resuelve ambas rutas externas.'
Write-Output 'Configuracion lista para revisar antes del arranque. No se iniciaron contenedores ni se ejecuto SQL.'
