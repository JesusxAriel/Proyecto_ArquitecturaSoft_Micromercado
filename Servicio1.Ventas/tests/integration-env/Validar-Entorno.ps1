$ErrorActionPreference = 'Stop'
# Solo analisis de archivos y docker compose config. No usa el daemon, SQL ni up/down.
$composePath = Join-Path $PSScriptRoot 'compose.yaml'
$ejemploPath = Join-Path $PSScriptRoot '.env.example'
$schemaPath = Join-Path $PSScriptRoot '../PuntoMarket.Ventas.Tests/Integration/EsquemaPruebas.sql'
$sql = [IO.File]::ReadAllText((Resolve-Path -LiteralPath $schemaPath).Path)
if ($sql -match '(?im)\b(?:DROP|CREATE)\s+DATABASE\b|\bDEFINER\s*=|\bSET\s+GLOBAL\b') {
    throw 'El esquema contiene operaciones globales o un DEFINER explicito; requiere revision.'
}
$bases = [regex]::Matches($sql, '(?im)^\s*USE\s+`([^`]+)`\s*;')
if ($bases.Count -ne 1 -or $bases[0].Groups[1].Value -cne 'puntomarket_ventas_test_local') {
    throw 'El esquema debe seleccionar exclusivamente la base de pruebas local.'
}
foreach ($trigger in @('trg_venta_bi','trg_venta_bu','trg_venta_bd','trg_detalle_venta_bi','trg_lote_ai','trg_lote_au','trg_lote_bu')) {
    if (-not $sql.Contains('CREATE TRIGGER ' + [char]96 + $trigger + [char]96)) {
        throw "Falta el trigger requerido: $trigger"
    }
}
if ($sql -notmatch 'CREATE TABLE PUNTOMARKET_TEST_GUARD' -or $sql -notmatch "VALUES\(1, 'SOLO_PRUEBAS_VENTAS'\)") {
    throw 'Falta el marcador esperado por las pruebas.'
}
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw 'Validacion estatica SQL correcta. Docker no esta disponible para validar Compose.'
}

# Valores temporales de interpolacion, no credenciales. No se crean ni se leen estos archivos.
$variables = @{
    VENTAS_TEST_USER = 'validacion_sin_login'
    VENTAS_TEST_ROOT_PASSWORD_FILE = (Join-Path $PSScriptRoot '.secrets/validation-only-root.txt')
    VENTAS_TEST_PASSWORD_FILE = (Join-Path $PSScriptRoot '.secrets/validation-only-user.txt')
}
$anteriores = @{}
try {
    foreach ($nombre in $variables.Keys) {
        $anteriores[$nombre] = [Environment]::GetEnvironmentVariable($nombre, 'Process')
        [Environment]::SetEnvironmentVariable($nombre, $variables[$nombre], 'Process')
    }
    $salida = & docker compose --project-name puntomarket-ventas-integration --env-file $ejemploPath -f $composePath config --format json
    if ($LASTEXITCODE -ne 0) { throw 'Docker Compose no pudo validar el modelo.' }
    $modelo = ($salida -join [Environment]::NewLine) | ConvertFrom-Json
    $servicios = @($modelo.services.PSObject.Properties)
    if ($servicios.Count -ne 1 -or $servicios[0].Name -ne 'mysql-test') { throw 'Debe existir solo mysql-test.' }
    $mysql = $modelo.services.'mysql-test'
    if ($mysql.image -ne 'mysql:8.4' -or $mysql.environment.MYSQL_DATABASE -ne 'puntomarket_ventas_test_local') { throw 'Imagen o base inesperada.' }
    if ($mysql.environment.MYSQL_ROOT_HOST -ne 'localhost') { throw 'Root no debe habilitarse para hosts remotos.' }
    $puertos = @($mysql.ports)
    if ($puertos.Count -ne 1 -or $puertos[0].host_ip -ne '127.0.0.1' -or
        $puertos[0].published -ne '3307' -or $puertos[0].target -ne 3306) { throw 'Publicacion de puertos insegura.' }
    $volumenes = @($mysql.volumes)
    if ($volumenes.Count -ne 1 -or $volumenes[0].type -ne 'volume' -or $volumenes[0].target -ne '/var/lib/mysql') { throw 'Montajes inesperados.' }
    foreach ($coleccion in @($modelo.volumes, $modelo.networks)) {
        if (@($coleccion.PSObject.Properties).Count -ne 1) { throw 'Recursos inesperados.' }
        foreach ($propiedad in $coleccion.PSObject.Properties) {
            if ($propiedad.Value.external -or -not $propiedad.Value.name.StartsWith('puntomarket-ventas-integration_')) { throw 'Recurso compartido o externo.' }
        }
    }
    if ($mysql.environment.MYSQL_PASSWORD -or $mysql.environment.MYSQL_ROOT_PASSWORD -or @($mysql.secrets).Count -ne 2) {
        throw 'Las passwords deben recibirse exclusivamente mediante archivos secrets.'
    }
    if ($mysql.environment.PUNTOMARKET_MYSQL_TESTS -or $mysql.environment.PUNTOMARKET_MYSQL_TEST_CONNECTION) { throw 'Compose no debe activar las pruebas.' }
    Write-Output 'Compose valido: un MySQL, 127.0.0.1:3307, base fija, volumen/red propios, secrets externos.'
    Write-Output 'Esquema revisado estaticamente: base dedicada, siete triggers del fixture y marcador presentes.'
    Write-Output 'No se conecto al daemon ni se iniciaron contenedores, bases, importaciones o pruebas.'
}
finally {
    foreach ($nombre in $anteriores.Keys) {
        [Environment]::SetEnvironmentVariable($nombre, $anteriores[$nombre], 'Process')
    }
}
