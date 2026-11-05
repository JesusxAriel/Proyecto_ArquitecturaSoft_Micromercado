#requires -Version 7.0
$ErrorActionPreference='Stop'
$ruta=Join-Path $PSScriptRoot 'Probar-JwtUsuariosVentas.ps1'
$tokens=$null; $errores=$null
$ast=[Management.Automation.Language.Parser]::ParseFile($ruta,[ref]$tokens,[ref]$errores)
if ($errores.Count -or $ast.ScriptRequirements.RequiredPSVersion -ne [version]'7.0') { throw 'Sintaxis o requisito de versión incorrecto.' }
# Dot-source define funciones; la guarda final NO ejecuta la prueba real.
. $ruta
function Solicitar-PasswordCajero {
    $script:solicitudes++
    if ($script:modo -eq 'cancelado') { return [Security.SecureString]::new() }
    return ConvertTo-SecureString 'DatoSinteticoSoloTest123!' -AsPlainText -Force
}
function Enviar-HttpJwtSeguro($Cliente,$Metodo,$Url,$Cuerpo,$Token) {
    $script:llamadas.Add(@{Metodo=$Metodo;Url=$Url;ConToken=([bool]$Token)})
    if ($script:modo -eq 'red' -and $script:llamadas.Count -eq 1) { throw [Net.Http.HttpRequestException]::new('DatoSinteticoSoloTest123! aaa.bbb.ccc') }
    if ($Url -like '*/health') { return @{Codigo=200} }
    if ($Url -like '*/api/usuarios') { return @{Codigo=401} }
    if ($Metodo -eq 'POST') {
        $obj=$Cuerpo | ConvertFrom-Json
        if ($obj.correo -ne 'cajero@example.invalid' -or $obj.contrasena -ne 'DatoSinteticoSoloTest123!' -or $Token) { throw 'Contrato de login incorrecto.' }
        switch ($script:modo) {
            'rechazo-login' { return @{Codigo=401} }
            'login-404' { return @{Codigo=404} }
            'json-invalido' { return @{Codigo=200;Contenido='{'} }
            'sin-token' { return @{Codigo=200;Contenido='{}'} }
            default { return @{Codigo=200;Contenido='{"token":"aaa.bbb.ccc"}'} }
        }
    }
    if (-not $Token) {
        if ($script:modo -eq 'anonimo') { return @{Codigo=200} }
        if ($script:modo -eq 'ruta-404') { return @{Codigo=404} }
        return @{Codigo=401}
    }
    if ($Token -ne 'aaa.bbb.ccc' -or $Cuerpo) { throw 'Token no enviado solo como cabecera.' }
    switch ($script:modo) {
        'rechazo-token' { return @{Codigo=401} }
        'prohibido' { return @{Codigo=403} }
        'producto-404' { return @{Codigo=404} }
        default { return @{Codigo=200} }
    }
}
$casos=@(
    @('cancelado',1,'login cancelado',3,1),
    @('ok',0,'Ventas aceptó el JWT',5,1),
    @('rechazo-login',1,'login rechazado por Usuarios',4,1),
    @('rechazo-token',1,'token rechazado por Ventas',5,1),
    @('prohibido',1,'acceso denegado',5,1),
    @('producto-404',1,'recurso o endpoint no encontrado',5,1),
    @('ruta-404',1,'endpoint de comprobación inexistente',3,0),
    @('login-404',1,'endpoint de login inexistente',4,1),
    @('anonimo',1,'protección distinta de la esperada',3,0),
    @('red',1,'error de conexión',1,0),
    @('json-invalido',1,'respuesta de login inválida',4,1),
    @('sin-token',1,'respuesta de login inválida',4,1)
)
foreach ($caso in $casos) {
    $script:modo=$caso[0]; $script:llamadas=[Collections.Generic.List[object]]::new(); $script:solicitudes=0
    $salida=@(Invoke-PruebaJwtUsuariosVentas 6>&1)
    $codigo=$salida[-1]; $texto=($salida | ForEach-Object { $_.ToString() }) -join "`n"
    if ($codigo -ne $caso[1] -or -not $texto.Contains($caso[2]) -or $script:llamadas.Count -ne $caso[3] -or $script:solicitudes -ne $caso[4]) { throw ('Falló caso '+$caso[0]) }
    if ($texto.Contains('DatoSinteticoSoloTest123!') -or $texto.Contains('aaa.bbb.ccc') -or $texto.Contains('carlos.mamani')) { throw 'Se publicó un dato privado simulado.' }
    if ($script:llamadas | Where-Object { $_.Metodo -notin @('GET','POST') -or ($_.Metodo -eq 'POST' -and $_.Url -notlike '*/api/autenticacion/login') }) { throw 'Operación no permitida.' }
}
# Verificar la allowlist real sin red: cargar de nuevo solo el transporte original.
$funcion=$ast.Find({param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -eq 'Enviar-HttpJwtSeguro'},$true)
. ([scriptblock]::Create($funcion.Extent.Text))
foreach ($url in @('http://example.invalid/login','http://127.0.0.1:5003/api/usuarios?token=secreto')) {
    $rechazo=$false
    try { Enviar-HttpJwtSeguro $null 'POST' $url $null $null } catch { $rechazo=$true }
    if (-not $rechazo) { throw 'La allowlist aceptó destino inesperado.' }
}
'OK: 12 escenarios simulados, no exposición de secretos y dos destinos rechazados. Sin login real ni red.'
