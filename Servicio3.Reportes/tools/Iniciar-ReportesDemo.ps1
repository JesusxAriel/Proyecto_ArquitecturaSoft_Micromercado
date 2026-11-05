# Ruta externa de conexión SELECT + TLS. No pasar secretos en la línea de comandos.
[CmdletBinding()]
param([string]$ConexionArchivo = $env:REPORTES_DEMO_CONNECTION_FILE)
$ErrorActionPreference = 'Stop'
$anterior = $env:REPORTES_DEMO_CONNECTION_FILE
try {
    if ($ConexionArchivo) { $env:REPORTES_DEMO_CONNECTION_FILE = $ConexionArchivo }
    $proyecto = Join-Path $PSScriptRoot 'PuntoMarket.Reportes.DemoLocal/PuntoMarket.Reportes.DemoLocal.csproj'
    & dotnet run --project $proyecto --no-launch-profile
    $codigo = $LASTEXITCODE
} finally { $env:REPORTES_DEMO_CONNECTION_FILE = $anterior }
exit $codigo
