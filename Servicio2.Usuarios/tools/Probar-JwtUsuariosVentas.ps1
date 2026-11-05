#requires -Version 7.0
# Prueba manual autorizada: no recibe contraseñas ni tokens por parámetros.
# No ejecutar con transcripción, depuración, trazas o captura externa del tráfico.
function Solicitar-PasswordCajero {
    return Read-Host 'Contraseña existente del Cajero (entrada oculta)' -AsSecureString
}
function Enviar-HttpJwtSeguro([Net.Http.HttpClient]$Cliente, [string]$Metodo, [string]$Url, [string]$Cuerpo, [string]$Token) {
    $permitidas = @{
        'GET http://127.0.0.1:5003/api/usuarios' = $true
        'GET http://127.0.0.1:5002/health' = $true
        'GET http://127.0.0.1:5002/api/productos/1/precio' = $true
        'POST http://127.0.0.1:5003/api/autenticacion/login' = $true
    }
    if (-not $permitidas.ContainsKey("$Metodo $Url")) { throw 'Destino no permitido.' }
    $peticion = [Net.Http.HttpRequestMessage]::new([Net.Http.HttpMethod]::new($Metodo), $Url)
    $respuesta = $null
    try {
        if ($Token) { $peticion.Headers.Authorization = [Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer',$Token) }
        if ($Cuerpo) { $peticion.Content = [Net.Http.StringContent]::new($Cuerpo,[Text.Encoding]::UTF8,'application/json') }
        $respuesta = $Cliente.SendAsync($peticion).GetAwaiter().GetResult()
        # Solo el login necesita cuerpo; errores y respuestas de negocio nunca se publican.
        $contenido = $null
        if ($Metodo -eq 'POST' -and [int]$respuesta.StatusCode -eq 200) {
            if ($respuesta.Content.Headers.ContentLength -gt 65536) { throw 'Respuesta no válida.' }
            $contenido = $respuesta.Content.ReadAsStringAsync().GetAwaiter().GetResult()
            if ($contenido.Length -gt 65536) { throw 'Respuesta no válida.' }
        }
        return @{ Codigo = [int]$respuesta.StatusCode; Contenido = $contenido }
    } finally { if ($respuesta) { $respuesta.Dispose() }; $peticion.Dispose() }
}
function Invoke-PruebaJwtUsuariosVentas {
    $ErrorActionPreference = 'Stop'
    $VerbosePreference = 'SilentlyContinue'; $DebugPreference = 'SilentlyContinue'
    $segura=$null; $puntero=[IntPtr]::Zero; $password=$null; $json=$null; $token=$null; $respuesta=$null; $datos=$null
    $handler=$null; $cliente=$null; $etapa='disponibilidad'
    try {
        $handler=[Net.Http.HttpClientHandler]::new()
        $handler.AllowAutoRedirect=$false; $handler.UseCookies=$false; $handler.UseProxy=$false
        $cliente=[Net.Http.HttpClient]::new($handler)
        $cliente.Timeout=[TimeSpan]::FromSeconds(15)
        # Comprueba disponibilidad y protección sin enviar credenciales.
        foreach ($prueba in @(
            @{ Nombre='Usuarios protegido'; Url='http://127.0.0.1:5003/api/usuarios'; Esperado=401 },
            @{ Nombre='Ventas salud'; Url='http://127.0.0.1:5002/health'; Esperado=200 },
            @{ Nombre='Ventas sin JWT'; Url='http://127.0.0.1:5002/api/productos/1/precio'; Esperado=401 }
        )) {
            $respuesta=Enviar-HttpJwtSeguro $cliente 'GET' $prueba.Url $null $null
            Write-Host ($prueba.Nombre+': HTTP '+$respuesta.Codigo)
            if ($respuesta.Codigo -ne $prueba.Esperado) {
                if ($respuesta.Codigo -eq 404) { Write-Host 'Resultado: endpoint de comprobación inexistente.' }
                else { Write-Host 'Resultado: disponibilidad o protección distinta de la esperada; prueba detenida.' }
                return 1
            }
        }
        $etapa='login'
        $segura=Solicitar-PasswordCajero
        if ($null -eq $segura -or $segura.Length -eq 0) { Write-Host 'Resultado: login cancelado; no se enviaron credenciales.'; return 1 }
        $puntero=[Runtime.InteropServices.Marshal]::SecureStringToBSTR($segura)
        $password=[Runtime.InteropServices.Marshal]::PtrToStringBSTR($puntero)
        $json=@{ correo='cajero@example.invalid'; contrasena=$password } | ConvertTo-Json -Compress
        $respuesta=Enviar-HttpJwtSeguro $cliente 'POST' 'http://127.0.0.1:5003/api/autenticacion/login' $json $null
        $password=$null; $json=$null
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($puntero); $puntero=[IntPtr]::Zero
        $segura.Dispose(); $segura=$null
        Write-Host ('Usuarios login: HTTP '+$respuesta.Codigo)
        if ($respuesta.Codigo -ne 200) {
            if ($respuesta.Codigo -eq 401) { Write-Host 'Resultado: login rechazado por Usuarios.' }
            elseif ($respuesta.Codigo -eq 404) { Write-Host 'Resultado: endpoint de login inexistente.' }
            else { Write-Host 'Resultado: Usuarios no completó el login.' }
            return 1
        }
        $datos=ConvertFrom-Json -InputObject $respuesta.Contenido -ErrorAction Stop
        if ($datos.token -isnot [string] -or [string]::IsNullOrWhiteSpace($datos.token) -or $datos.token -notmatch '^[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+$') {
            Write-Host 'Resultado: respuesta de login inválida; no se envió token a Ventas.'; return 1
        }
        $token=$datos.token; $datos=$null; $respuesta=$null
        $etapa='consulta protegida de Ventas'
        $respuesta=Enviar-HttpJwtSeguro $cliente 'GET' 'http://127.0.0.1:5002/api/productos/1/precio' $null $token
        Write-Host ('Ventas con JWT: HTTP '+$respuesta.Codigo)
        switch ($respuesta.Codigo) {
            200 { Write-Host 'Resultado: Ventas aceptó el JWT y autorizó la consulta.'; return 0 }
            401 { Write-Host 'Resultado: token rechazado por Ventas.' }
            403 { Write-Host 'Resultado: acceso denegado por permisos o cambio obligatorio de contraseña; no equivale a firma JWT inválida.' }
            404 { Write-Host 'Resultado: recurso o endpoint no encontrado; no se confirma éxito de la consulta.' }
            default { Write-Host 'Resultado: Ventas no completó la consulta; integración no confirmada.' }
        }
        return 1
    } catch {
        # No publicar excepción, cuerpo, objetos de petición ni argumentos.
        if ($etapa -eq 'login' -and $respuesta -and $respuesta.Codigo -eq 200) { Write-Host 'Resultado: respuesta de login inválida.' }
        else { Write-Host ("Resultado: error de conexión o procesamiento en $etapa; detalles privados omitidos.") }
        return 1
    } finally {
        if ($puntero -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($puntero) }
        if ($segura) { $segura.Dispose() }
        if ($cliente) { $cliente.Dispose() } elseif ($handler) { $handler.Dispose() }
        $password=$null; $json=$null; $token=$null; $datos=$null; $respuesta=$null
    }
}
# Dot-source permite probar las funciones con transporte simulado, sin activar la prueba real.
if ($MyInvocation.InvocationName -ne '.') { exit (Invoke-PruebaJwtUsuariosVentas) }
