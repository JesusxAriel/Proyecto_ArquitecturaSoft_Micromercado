[CmdletBinding()]
param([switch]$OmitirCompose)
$ErrorActionPreference = 'Stop'
function Exigir([bool]$condicion, [string]$mensaje) {
    if (-not $condicion) { throw $mensaje }
}
$origen = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../bdMicroMercadoArqui.sql'))
$rutaCopia = Join-Path $PSScriptRoot 'sql/PuntoMarket.Ventas.Demo.sql'
$original = [IO.File]::ReadAllText($origen).Replace("`r`n", "`n")
$copia = [IO.File]::ReadAllText($rutaCopia).Replace("`r`n", "`n")
$manifest = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'manifest.json') -Raw | ConvertFrom-Json
Exigir ((Get-FileHash -LiteralPath $origen).Hash -eq $manifest.fuenteSha256) 'Cambio la fuente original: revisar antes de regenerar.'
Exigir ((Get-FileHash -LiteralPath $rutaCopia).Hash -eq $manifest.copiaSha256) 'La copia no coincide con el manifiesto.'
Exigir ($copia -notmatch '(?im)^\s*(DROP\s|CREATE\s+DATABASE|ALTER\s+DATABASE|CREATE\s+OR\s+REPLACE)') 'Se encontro una instruccion de sustitucion o borrado.'
Exigir ($copia -notmatch 'bdMicroMercadoArqui`|\$2[aby]\$') 'Referencia al esquema original o hash privado incrustado.'
$uses = [regex]::Matches($copia, '(?im)^USE `([^`]+)`;')
Exigir ($uses.Count -eq 1 -and $uses[0].Groups[1].Value -eq 'puntomarket_ventas_demo_local') 'Destino SQL incorrecto.'

# Comparar definiciones completas, no solo el numero de objetos.
foreach ($tipo in @('TABLE', 'VIEW', 'TRIGGER', 'PROCEDURE')) {
    $fuenteNormalizada = $original.Replace('CREATE OR REPLACE VIEW', 'CREATE VIEW')
    $patron = switch ($tipo) {
        'TABLE' { '(?ms)^CREATE TABLE `[^`]+`.*?ENGINE=InnoDB[^;]*;' }
        'VIEW' { '(?ms)^CREATE VIEW `[^`]+`.*?;' }
        default { '(?ms)^CREATE ' + $tipo + ' `[^`]+`.*?END\$\$' }
    }
    $objetosOrigen = @([regex]::Matches($fuenteNormalizada, $patron) | ForEach-Object Value)
    $objetosCopia = @([regex]::Matches($copia, $patron) | ForEach-Object Value)
    $esperados = switch ($tipo) { 'TABLE' {11} 'VIEW' {4} 'TRIGGER' {23} 'PROCEDURE' {1} }
    Exigir ($objetosOrigen.Count -eq $esperados -and $objetosCopia.Count -eq $esperados) "Conteo inesperado: $tipo."
    Exigir (($objetosOrigen -join "`n") -ceq ($objetosCopia -join "`n")) "Cambio una definicion de $tipo."
    Write-Output "$tipo : $esperados definiciones conservadas."
}
# Comparar cada bloque de semillas, permitiendo solo externalizar los dos hashes.
$normalizado = $original.Replace('@ventas_demo_admin_password_hash', "'<HASH_PRIVADO>'").Replace('@ventas_demo_cajero_password_hash', "'<HASH_PRIVADO>'")
$normalizadoCopia = $copia.Replace('@ventas_demo_admin_password_hash', "'<HASH_PRIVADO>'").Replace('@ventas_demo_cajero_password_hash', "'<HASH_PRIVADO>'")
$patronInsert = '(?ms)^INSERT INTO `[^`]+`.*?;'
$semillasOrigen = @([regex]::Matches($normalizado, $patronInsert) | ForEach-Object Value)
$semillasCopia = @([regex]::Matches($normalizadoCopia, $patronInsert) | ForEach-Object Value)
Exigir ($semillasOrigen.Count -eq 9) 'Conteo inesperado de bloques de semillas.'
Exigir (($semillasOrigen -join "`n") -ceq ($semillasCopia -join "`n")) 'Cambio una semilla sintetica respecto a la fuente.'
foreach ($bloque in $semillasCopia) {
    $tabla = [regex]::Match($bloque, '^INSERT INTO `([^`]+)`').Groups[1].Value
    $filas = [regex]::Matches($bloque.Substring($bloque.IndexOf('VALUES') + 6), '(?m)^\(').Count
    # HISTORIAL_PRECIO tiene la lista de columnas en otra linea.
    Write-Output "Semillas $tabla : $filas filas sinteticas."
}

if (-not $OmitirCompose) {
    # Validacion del modelo sin motor Docker, sin archivos privados y sin crear recursos.
    $modeloJson = & docker compose --project-name puntomarket-ventas-demo --env-file (Join-Path $PSScriptRoot '.env.example') -f (Join-Path $PSScriptRoot 'compose.yaml') config --format json
    Exigir ($LASTEXITCODE -eq 0) 'Docker Compose no pudo validar el modelo.'
    $modelo = ($modeloJson -join "`n") | ConvertFrom-Json
    Exigir ($modelo.name -eq 'puntomarket-ventas-demo') 'Proyecto Compose incorrecto.'
    Exigir (@($modelo.services.PSObject.Properties).Count -eq 1) 'Se esperaba un unico servicio.'
    $mysql = $modelo.services.'mysql-demo'
    Exigir ($mysql.image -eq 'mysql:8.4') 'Imagen inesperada.'
    Exigir ($mysql.environment.MYSQL_DATABASE -eq 'puntomarket_ventas_demo_local') 'Base inesperada.'
    Exigir ($mysql.environment.MYSQL_USER -eq 'ventas_demo') 'Usuario de ejemplo incorrecto.'
    Exigir ($mysql.environment.MYSQL_ROOT_HOST -eq 'localhost') 'Root no debe publicarse remotamente.'
    Exigir (@($mysql.ports).Count -eq 1 -and $mysql.ports[0].host_ip -eq '127.0.0.1' -and $mysql.ports[0].published -eq '3308' -and $mysql.ports[0].target -eq 3306) 'Puerto no aislado.'
    Exigir (@($mysql.volumes).Count -eq 1 -and $mysql.volumes[0].type -eq 'volume' -and $mysql.volumes[0].source -eq 'ventas-demo-data' -and $mysql.volumes[0].target -eq '/var/lib/mysql') 'Montaje inesperado.'
    Exigir ($modelo.volumes.'ventas-demo-data'.name -eq 'puntomarket-ventas-demo_ventas-demo-data') 'Volumen inesperado.'
    Exigir ($modelo.networks.'ventas-demo-net'.name -eq 'puntomarket-ventas-demo_ventas-demo-net') 'Red inesperada.'
    Exigir (-not $modelo.volumes.'ventas-demo-data'.external -and -not $modelo.networks.'ventas-demo-net'.external) 'No usar recursos externos.'
    Exigir (@($mysql.networks.PSObject.Properties).Count -eq 1 -and @($mysql.networks.PSObject.Properties)[0].Name -eq 'ventas-demo-net') 'Red del servicio incorrecta.'
    Exigir (@($mysql.secrets).Count -eq 2) 'Se esperaban dos secretos externos.'
    Write-Output 'Compose valido: un MySQL 8.4, loopback 3308, base/red/volumen exclusivos, sin SQL automatico.'
}
Write-Output 'Validacion estatica completa. No se abrieron conexiones ni se ejecuto SQL.'
