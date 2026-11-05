# Solo transforma archivos. No abre conexiones ni ejecuta SQL.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$origen = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../bdMicroMercadoArqui.sql'))
$sql = [IO.File]::ReadAllText($origen).Replace("`r`n", "`n")
$sql = $sql -replace '(?m)^DROP DATABASE IF EXISTS `bdMicroMercadoArqui`;\n', ''
$sql = $sql -replace '(?ms)^CREATE DATABASE `bdMicroMercadoArqui`\n.*?;\n', ''
$sql = $sql.Replace('USE `bdMicroMercadoArqui`;', 'USE `puntomarket_ventas_demo_local`;')
$sql = $sql.Replace('CREATE OR REPLACE VIEW', 'CREATE VIEW')
$sql = $sql -replace '(?m)^DROP PROCEDURE IF EXISTS `sp_arqueo_stock`\$\$\n', ''
# El stock de las semillas ya lo calculan los triggers originales de LOTE.
$sql = $sql -replace '(?ms)^UPDATE `PRODUCTO` p\n.*?WHERE p\.`id` > 0;\n', ''
# Consultas diagnosticas finales: no forman parte de la inicializacion.
$inicioDiagnostico = $sql.IndexOf('SELECT * FROM `vw_resumen_ventas`;')
if ($inicioDiagnostico -lt 0) { throw 'Fuente inesperada: faltan consultas finales.' }
$sql = $sql.Substring(0, $inicioDiagnostico)
# No duplicar los hashes de autenticacion originales en archivos versionados.
# El importador futuro debera cargar estas variables desde archivos privados,
# dentro de la misma sesion MySQL. No se generan credenciales alternativas.
if ($sql -match '\$2[aby]\$' -or -not $sql.Contains('@ventas_demo_admin_password_hash') -or -not $sql.Contains('@ventas_demo_cajero_password_hash')) {
    throw 'La fuente debe externalizar hashes privados, nunca incrustarlos.'
}
$cabecera = @'
-- Derivado exclusivamente de bdMicroMercadoArqui.sql; ver manifest.json.
-- SOLO importar una vez, en base VACIA puntomarket_ventas_demo_local.
-- No ejecutar directamente sin validar destino, permisos y ausencia de objetos.
-- Requiere @ventas_demo_admin_password_hash y @ventas_demo_cajero_password_hash
-- cargados previamente desde archivos privados en ESTA MISMA sesion.
-- Sin estas variables las semillas USUARIO fallaran (passwordHash NOT NULL).
-- No usar --force; el DDL MySQL no es atomico para el archivo completo.

'@
$destino = Join-Path $PSScriptRoot 'sql'
[IO.Directory]::CreateDirectory($destino) | Out-Null
$rutaCopia = Join-Path $destino 'PuntoMarket.Ventas.Demo.sql'
[IO.File]::WriteAllText($rutaCopia, $cabecera.Replace("`r`n", "`n") + $sql, [Text.UTF8Encoding]::new($false))
$manifest = [ordered]@{
    fuente = 'bdMicroMercadoArqui.sql'
    fuenteSha256 = (Get-FileHash -LiteralPath $origen -Algorithm SHA256).Hash
    copia = 'sql/PuntoMarket.Ventas.Demo.sql'
    copiaSha256 = (Get-FileHash -LiteralPath $rutaCopia -Algorithm SHA256).Hash
    base = 'puntomarket_ventas_demo_local'
}
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'manifest.json'), ($manifest | ConvertTo-Json) + "`n", [Text.UTF8Encoding]::new($false))
Write-Output 'Copia SQL y manifiesto preparados. No se ejecuto SQL.'
