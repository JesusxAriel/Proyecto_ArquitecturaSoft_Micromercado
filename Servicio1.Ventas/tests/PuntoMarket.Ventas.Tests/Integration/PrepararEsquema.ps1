param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^puntomarket_ventas_test_[a-z0-9_]{1,40}$')]
    [string]$DatabaseName
)

$ErrorActionPreference = 'Stop'
# Solo genera un archivo. No abre conexiones ni ejecuta SQL.
$originalPath = Join-Path $PSScriptRoot '../../../../bdMicroMercadoArqui.sql'
$sqlOriginal = [IO.File]::ReadAllText((Resolve-Path -LiteralPath $originalPath).Path)
$inicio = $sqlOriginal.IndexOf('CREATE TABLE `ROL`')
$fin = $sqlOriginal.IndexOf('INSERT INTO `ROL`', $inicio)
if ($inicio -lt 0 -or $fin -le $inicio) {
    throw 'El SQL original no tiene los límites esperados; revisar antes de generar el esquema.'
}
$esquema = $sqlOriginal.Substring($inicio, $fin - $inicio)
if ($esquema -match '(?im)^\s*(DROP|CREATE)\s+DATABASE\b|(?im)^\s*USE\s+') {
    throw 'El segmento extraído contiene operaciones de selección/creación/borrado de bases.'
}
$encabezado = 'SET NAMES utf8mb4;' + [Environment]::NewLine + 'USE ' + [char]96 + $DatabaseName + [char]96 + ';' + [Environment]::NewLine
$marcador = @'

CREATE TABLE PUNTOMARKET_TEST_GUARD (
    id INT NOT NULL PRIMARY KEY,
    proposito VARCHAR(50) NOT NULL
) ENGINE=InnoDB;
INSERT INTO PUNTOMARKET_TEST_GUARD(id, proposito) VALUES(1, 'SOLO_PRUEBAS_VENTAS');
'@
$destinoSql = Join-Path $PSScriptRoot 'EsquemaPruebas.sql'
[IO.File]::WriteAllText($destinoSql, $encabezado + $esquema + $marcador)
Write-Output "Archivo generado: $destinoSql. No se ha ejecutado SQL."
