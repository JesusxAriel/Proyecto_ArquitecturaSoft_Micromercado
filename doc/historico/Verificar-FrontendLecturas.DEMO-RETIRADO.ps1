# HISTORICO: usaba perfiles /Demo retirados. No usar como verificacion actual.
# Comprobacion explicita de lectura. POST solo para sesion local, nunca ventas.
$ErrorActionPreference = 'Stop'
function Snapshot {
    $shell = @'
set -eu
export MYSQL_PWD="$(cat /run/secrets/ventas_demo_password)"
exec mysql --protocol=TCP --host=127.0.0.1 --user="$MYSQL_USER" --database=puntomarket_ventas_demo_local --batch --raw --skip-column-names --execute="CHECKSUM TABLE ROL,USUARIO,CATEGORIAS,PROVEEDOR,EMPAQUE,PRODUCTO,LOTE,HISTORIAL_PRECIO,CLIENTE,VENTA,DETALLE_VENTA EXTENDED;"
'@
    $salida = @(& docker exec puntomarket-ventas-demo-mysql-demo-1 sh -c $shell)
    if ($LASTEXITCODE -ne 0 -or $salida.Count -ne 11) { throw 'No se pudo comprobar la huella Demo.' }
    return ($salida -join "`n")
}
$raw = & docker inspect puntomarket-ventas-demo-mysql-demo-1
if ($LASTEXITCODE -ne 0) { throw 'Demo no disponible.' }
$c = @(($raw -join "`n") | ConvertFrom-Json)[0]
if ($c.Config.Labels.'com.docker.compose.project' -ne 'puntomarket-ventas-demo' -or $c.State.Health.Status -ne 'healthy') { throw 'Destino Docker inesperado.' }
$antes = Snapshot
$handler = [Net.Http.HttpClientHandler]::new()
$handler.AllowAutoRedirect = $false
$handler.CookieContainer = [Net.CookieContainer]::new()
$http = [Net.Http.HttpClient]::new($handler)
$http.BaseAddress = [Uri]'http://localhost:5100'
function Get([string]$ruta, [int]$esperado = 200) {
    $r = $http.GetAsync($ruta).GetAwaiter().GetResult()
    try {
        if ([int]$r.StatusCode -ne $esperado) { throw "Estado inesperado en $ruta." }
        Write-Host "GET $ruta -> $esperado"
        return $r.Content.ReadAsStringAsync().GetAwaiter().GetResult()
    } finally { $r.Dispose() }
}
try {
    [void](Get '/Sales?handler=BuscarProductos&termino=leche' 401)
    $html = Get '/Demo'
    $csrf = [Net.WebUtility]::HtmlDecode([regex]::Match($html, 'name="__RequestVerificationToken"[^>]*value="([^"]+)"').Groups[1].Value)
    if (-not $csrf) { throw 'Falta antiforgery local.' }
    $campos = [Collections.Generic.Dictionary[string,string]]::new()
    $campos.Add('__RequestVerificationToken', $csrf)
    $formulario = [Net.Http.FormUrlEncodedContent]::new($campos)
    try { $r = $http.PostAsync('/Demo', $formulario).GetAwaiter().GetResult(); if ([int]$r.StatusCode -ne 302) { throw 'No se pudo iniciar la sesion local.' }; $r.Dispose() } finally { $formulario.Dispose() }
    $html = Get '/Sales'
    if ($html -match 'Bearer |eyJhbGci') { throw 'Posible token expuesto en HTML.' }
    $productos = (Get '/Sales?handler=BuscarProductos&termino=leche') | ConvertFrom-Json
    if (@($productos.productos | Where-Object id -eq 1).Count -ne 1) { throw 'Falta producto real 1.' }
    $precio = (Get '/Sales?handler=Precio&id=1') | ConvertFrom-Json
    $stock = (Get '/Sales?handler=Stock&id=1') | ConvertFrom-Json
    if ($precio.precioVenta -ne 6.50 -or $stock.stockDisponible -ne 120) { throw 'Precio o stock distintos de las semillas esperadas.' }
    $clientes = @((Get '/Sales?handler=BuscarClientes&termino=JUAN') | ConvertFrom-Json)
    if (@($clientes | Where-Object id -eq 2).Count -ne 1) { throw 'Falta cliente real 2.' }
    [void](Get '/Sales?handler=BuscarClientes&termino=4545451226')
    [void](Get '/Sales?handler=Precio&id=2147483647' 404)
    $vacio = (Get '/Sales?handler=BuscarProductos&termino=ZZZ_SIN_COINCIDENCIAS_DEMO') | ConvertFrom-Json
    if (@($vacio.productos).Count -ne 0) { throw 'Busqueda sin resultados inesperada.' }
    if ((Snapshot) -cne $antes) { throw 'Cambiaron datos Demo durante las lecturas.' }
    Write-Output 'Razor -> cliente HTTP tipado -> API JWT -> MySQL Demo verificado. Once tablas sin cambios; ningun JWT expuesto al navegador.'
} finally { $http.Dispose() }
