# Solo lecturas sobre el contenedor/base Demo fijos. No importa ni reintenta SQL.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$contenedor = 'puntomarket-ventas-demo-mysql-demo-1'
function Leer-MySql([string]$sql) {
    $shell = @'
set -eu
export MYSQL_PWD="$(cat /run/secrets/ventas_demo_password)"
exec mysql --protocol=TCP --host=127.0.0.1 --user="$MYSQL_USER" --database=puntomarket_ventas_demo_local --default-character-set=utf8mb4 --batch --raw --skip-column-names --binary-mode --skip-reconnect
'@
    $p = [Diagnostics.Process]::new()
    $p.StartInfo = [Diagnostics.ProcessStartInfo]::new()
    $p.StartInfo.FileName = (Get-Command docker).Source
    $p.StartInfo.UseShellExecute = $false
    $p.StartInfo.CreateNoWindow = $true
    $p.StartInfo.RedirectStandardInput = $true
    $p.StartInfo.RedirectStandardOutput = $true
    $p.StartInfo.RedirectStandardError = $true
    $p.StartInfo.StandardInputEncoding = [Text.UTF8Encoding]::new($false)
    $p.StartInfo.StandardOutputEncoding = [Text.UTF8Encoding]::new($false)
    foreach ($arg in @('exec', '-i', $contenedor, 'sh', '-c', $shell)) { $p.StartInfo.ArgumentList.Add($arg) }
    try {
        [void]$p.Start()
        $salida = $p.StandardOutput.ReadToEndAsync()
        $errorPrivado = $p.StandardError.ReadToEndAsync()
        $p.StandardInput.Write($sql)
        $p.StandardInput.Close()
        $p.WaitForExit()
        $texto = $salida.GetAwaiter().GetResult()
        $errorTexto = $errorPrivado.GetAwaiter().GetResult()
        if ($p.ExitCode -ne 0) {
            $codigo = [regex]::Match($errorTexto, 'ERROR\s+\d+\s+\([A-Z0-9]+\)\s+at line\s+\d+').Value
            throw "Fallo de verificacion MySQL ($codigo). Sin reimportacion ni cambios."
        }
        return @($texto -split '\r?\n' | Where-Object { $_.Length -gt 0 })
    } finally { $p.Dispose() }
}
$raw = & docker inspect $contenedor
if ($LASTEXITCODE -ne 0) { throw 'No se pudo inspeccionar el contenedor.' }
$c = @(($raw -join "`n") | ConvertFrom-Json)[0]
if ($c.State.Status -ne 'running' -or $c.State.Health.Status -ne 'healthy' -or $c.Config.Labels.'com.docker.compose.project' -ne 'puntomarket-ventas-demo') { throw 'Estado o proyecto incorrecto.' }
$destino = @(Leer-MySql 'SELECT DATABASE(),CURRENT_USER(),@@session.foreign_key_checks,VERSION(),CURDATE();')
$d = $destino[0] -split "`t"
if ($d[0] -ne 'puntomarket_ventas_demo_local' -or $d[1] -notlike 'ventas_demo@*' -or $d[2] -ne '1') { throw 'Destino, cuenta o comprobacion de FK incorrectos.' }
$copia = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'sql/PuntoMarket.Ventas.Demo.sql'))
$objetos = @(Leer-MySql @'
SELECT IF(TABLE_TYPE='BASE TABLE','TABLE','VIEW'),TABLE_NAME FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE()
UNION ALL SELECT 'TRIGGER',TRIGGER_NAME FROM information_schema.TRIGGERS WHERE TRIGGER_SCHEMA=DATABASE()
UNION ALL SELECT ROUTINE_TYPE,ROUTINE_NAME FROM information_schema.ROUTINES WHERE ROUTINE_SCHEMA=DATABASE();
'@)
$conteosObjetos = [ordered]@{}
foreach ($tipo in @('TABLE','VIEW','TRIGGER','PROCEDURE')) {
    $esperados = @([regex]::Matches($copia, ('(?m)^CREATE ' + $tipo + ' `([^`]+)`')) | ForEach-Object { $_.Groups[1].Value } | Sort-Object)
    $actuales = @($objetos | ForEach-Object { $partes = $_ -split "`t"; if ($partes[0] -eq $tipo) { $partes[1] } } | Sort-Object)
    if (($esperados -join '|') -cne ($actuales -join '|')) { throw "Objetos $tipo diferentes a la copia original adaptada." }
    $conteosObjetos[$tipo] = $actuales.Count
}
if ($objetos.Count -ne 39) { throw 'Objetos adicionales inesperados.' }
$filasEsperadas = [ordered]@{ ROL=2;USUARIO=2;CATEGORIAS=8;PROVEEDOR=7;EMPAQUE=9;PRODUCTO=21;LOTE=22;HISTORIAL_PRECIO=3;CLIENTE=3;VENTA=0;DETALLE_VENTA=0 }
$conteos = [ordered]@{}
foreach ($tabla in $filasEsperadas.Keys) {
    $n = @(Leer-MySql ('SELECT COUNT(*) FROM `' + $tabla + '`;'))
    $conteos[$tabla] = [int]$n[0]
    if ($conteos[$tabla] -ne $filasEsperadas[$tabla]) { throw "Conteo de filas inesperado: $tabla." }
}

# Auditar cada FK, incluidas las compuestas, mediante anti-joins de solo lectura.
$metadatos = @(Leer-MySql @'
SELECT CONSTRAINT_NAME,TABLE_NAME,COLUMN_NAME,REFERENCED_TABLE_NAME,REFERENCED_COLUMN_NAME,ORDINAL_POSITION
FROM information_schema.KEY_COLUMN_USAGE WHERE CONSTRAINT_SCHEMA=DATABASE() AND REFERENCED_TABLE_NAME IS NOT NULL
ORDER BY TABLE_NAME,CONSTRAINT_NAME,ORDINAL_POSITION;
'@)
$grupos = @{}
foreach ($linea in $metadatos) {
    $f = $linea -split "`t"
    $clave = $f[1] + '.' + $f[0]
    if (-not $grupos.ContainsKey($clave)) { $grupos[$clave] = @() }
    $grupos[$clave] += ,$f
}
$esperadasFk = [regex]::Matches($copia, 'FOREIGN KEY').Count
if ($grupos.Count -ne $esperadasFk) { throw 'Cantidad inesperada de claves foraneas.' }
foreach ($grupo in $grupos.Values) {
    $tabla = $grupo[0][1]
    $referenciada = $grupo[0][3]
    $condiciones = @($grupo | ForEach-Object { 'c.`' + $_[2] + '`=r.`' + $_[4] + '`' }) -join ' AND '
    $noNulos = @($grupo | ForEach-Object { 'c.`' + $_[2] + '` IS NOT NULL' }) -join ' AND '
    $consultaFk = 'SELECT COUNT(*) FROM `' + $tabla + '` c LEFT JOIN `' + $referenciada + '` r ON ' + $condiciones + ' WHERE ' + $noNulos + ' AND r.`' + $grupo[0][4] + '` IS NULL;'
    $huerfanos = @(Leer-MySql $consultaFk)
    if ($huerfanos[0] -ne '0') { throw 'Se detectaron referencias huerfanas. No modificar automaticamente.' }
}
$vars = @{}
foreach ($linea in [IO.File]::ReadAllLines((Join-Path $PSScriptRoot '.env'))) {
    if ($linea -match '^(VENTAS_DEMO_[A-Z_]+)=(.*)$') { $vars[$Matches[1]] = $Matches[2] }
}
$a = [Convert]::ToHexString([IO.File]::ReadAllBytes($vars.VENTAS_DEMO_ADMIN_HASH_FILE))
$b = [Convert]::ToHexString([IO.File]::ReadAllBytes($vars.VENTAS_DEMO_CAJERO_HASH_FILE))
$usuarios = @(Leer-MySql "SELECT COUNT(*) FROM USUARIO WHERE (id=1 AND username='admin' AND HEX(passwordHash)='$a' OR id=2 AND username='cajero1' AND HEX(passwordHash)='$b') AND estaActivo=1 AND estado='ACTIVO';")
$a = $null
$b = $null
if ($usuarios[0] -ne '2') { throw 'Usuarios, estado activo o hashes distintos de las semillas privadas.' }
$negocio = @(Leer-MySql @'
SELECT 'stock_incoherente',COUNT(*) FROM (SELECT p.id FROM PRODUCTO p LEFT JOIN LOTE l ON l.idProducto=p.id AND l.estaActivo=1 GROUP BY p.id,p.stockActual HAVING p.stockActual<>COALESCE(SUM(l.cantidadDisponible),0)) x;
SELECT 'cantidades_lote_incorrectas',COUNT(*) FROM LOTE WHERE cantidadDisponible<>cantidadInicial OR cantidadDisponible<0 OR cantidadDisponible>cantidadInicial;
SELECT 'productos_activos',COUNT(*) FROM PRODUCTO WHERE estaActivo=1;
SELECT 'clientes_activos',COUNT(*) FROM CLIENTE WHERE estaActivo=1;
SELECT 'lotes_activos',COUNT(*) FROM LOTE WHERE estaActivo=1;
SELECT 'lotes_sin_vencimiento',COUNT(*) FROM LOTE WHERE fechaVencimiento IS NULL;
SELECT 'lotes_vencidos',COUNT(*) FROM LOTE WHERE fechaVencimiento<CURDATE();
SELECT 'stock_total',SUM(cantidadDisponible) FROM LOTE;
SELECT 'producto_1',precioVenta,stockActual FROM PRODUCTO WHERE id=1;
SELECT 'eventos',COUNT(*) FROM information_schema.EVENTS WHERE EVENT_SCHEMA=DATABASE();
'@)
$resultados = [ordered]@{}
foreach ($linea in $negocio) { $f = $linea -split "`t"; $resultados[$f[0]] = @($f | Select-Object -Skip 1) }
foreach ($par in @(@('stock_incoherente','0'),@('cantidades_lote_incorrectas','0'),@('productos_activos','21'),@('clientes_activos','3'),@('lotes_activos','22'),@('lotes_sin_vencimiento','3'),@('lotes_vencidos','0'),@('eventos','0'))) {
    if ($resultados[$par[0]][0] -ne $par[1]) { throw "Resultado inesperado: $($par[0])." }
}
if ($resultados.producto_1[0] -ne '6.50' -or $resultados.producto_1[1] -ne '120') { throw 'Precio o stock del producto 1 inesperado.' }
# Comprobar que todas las vistas y el procedimiento originales pueden leerse.
foreach ($vista in @('vw_productos_con_stock','vw_resumen_ventas','vw_comprobante_venta','vw_lotes_por_vencer')) {
    [void](Leer-MySql ('SELECT COUNT(*) FROM `' + $vista + '`;'))
}
[void](Leer-MySql 'CALL sp_arqueo_stock(30);')
$salud = & docker inspect --format '{{.State.Status}}|{{.State.Health.Status}}' $contenedor
if ($LASTEXITCODE -ne 0 -or $salud -ne 'running|healthy') { throw 'Salud final inesperada.' }
$resumen = [ordered]@{
    estado = 'VERIFICADA'; base = $d[0]; versionMySql = $d[3]; fechaBase = $d[4]
    contenedor = $contenedor; salud = $salud; objetos = $conteosObjetos; filas = $conteos
    totalFilas = ($conteos.Values | Measure-Object -Sum).Sum
    clavesForaneasComprobadas = $grupos.Count; referenciasHuerfanas = 0
    usuariosActivosConHashesOriginales = 2; comprobacionesStock = $resultados
    vistasConsultables = 4; procedimientoConsultable = $true
}
$json = $resumen | ConvertTo-Json -Depth 6
[IO.File]::WriteAllText((Join-Path $PSScriptRoot '.verificacion.local.json'), $json, [Text.UTF8Encoding]::new($false))
Write-Output $json
