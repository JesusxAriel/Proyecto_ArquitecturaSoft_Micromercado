#requires -Version 7.0
# Pruebas de AST y guardas aisladas: NO ejecutan el script de activación.
$ErrorActionPreference = 'Stop'
$ruta = Join-Path $PSScriptRoot 'Activar-CuentaDemo.ps1'
$tokens = $null; $errores = $null
$ast = [System.Management.Automation.Language.Parser]::ParseFile($ruta,[ref]$tokens,[ref]$errores)
if ($errores.Count) { throw 'Sintaxis inválida.' }
if ($ast.ScriptRequirements.RequiredPSVersion -ne [version]'7.0') { throw 'Falta requisito PowerShell 7.0.' }
$ifs = $ast.FindAll({ param($n) $n -is [System.Management.Automation.Language.IfStatementAst] },$true)
function Guarda([string]$Prefijo) {
    $nodos = @($ifs | Where-Object { $_.Extent.Text.StartsWith($Prefijo) })
    if ($nodos.Count -ne 1) { throw 'Guarda no identificable de forma única.' }
    return [scriptblock]::Create($nodos[0].Extent.Text)
}
function DebeRechazar([scriptblock]$Bloque) {
    $rechazado = $false
    try { & $Bloque } catch { $rechazado = $true }
    if (-not $rechazado) { throw 'La guarda no detuvo el flujo.' }
}
# Solo se evalúa cada IF, sin ejecutar try, funciones MySQL, Docker, ACL o escrituras.
$Ejecutar = $false
DebeRechazar (Guarda 'if (-not $Ejecutar)')
$privado = $PSScriptRoot # Directorio existente no privado, usado solo para Test-Path.
DebeRechazar (Guarda 'if (Test-Path -LiteralPath $privado)')
$DirectorioPrivado = 'ruta-relativa'
DebeRechazar (Guarda 'if (-not [IO.Path]::IsPathFullyQualified')
$comprobacion = '0' # Resultado de preflight ante cuenta existente u otra incompatibilidad.
DebeRechazar (Guarda "if (`$comprobacion -ne '1')")
$comprobacion = '1'
& (Guarda "if (`$comprobacion -ne '1')")
$texto = $ast.Extent.Text
if ($texto -notmatch "SELECT COUNT\(\*\) FROM mysql.user WHERE User='usuarios_demo'\)=0") { throw 'No se conserva la consulta de existencia.' }
if ($texto.IndexOf("if (`$comprobacion -ne '1')") -gt $texto.IndexOf('[void](New-Item')) { throw 'Guarda SQL posterior a escrituras privadas.' }
if ($texto.IndexOf('if (Test-Path -LiteralPath $privado)') -gt $texto.IndexOf('$salud=& docker')) { throw 'Guarda de carpeta tardía.' }
$catch = $ast.FindAll({param($n) $n -is [System.Management.Automation.Language.CatchClauseAst]},$true)
if ($catch.Count -ne 1) { throw 'Catch inesperado.' }
# Verifica que las instrucciones del catch no referencien la excepción original ni secretos.
$prohibidas = @('_','password','sql','cadena','resultado','segura')
$variables = $catch[0].FindAll({param($n) $n -is [System.Management.Automation.Language.VariableExpressionAst]},$true)
foreach ($variable in $variables) {
    if ($variable.VariablePath.UserPath -in $prohibidas) { throw 'Catch referencia datos potencialmente privados.' }
}
'OK: parser, #requires, bloqueo sin -Ejecutar, carpeta existente, ruta relativa, cuenta/preflight incompatible, orden de guardas y catch sin secretos.'
'No se ejecutaron Docker, MySQL, activación ni escrituras privadas.'

# Cargar únicamente la función pura desde AST, nunca el script de activación.
$funcion = $ast.Find({param($n) $n -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -eq 'Test-PermisosUsuariosDemo'},$true)
. ([scriptblock]::Create($funcion.Extent.Text))
$valido = @'
GRANT USAGE ON *.* TO `usuarios_demo`@`172.20.0.1`
GRANT SELECT ON `puntomarket_ventas_demo_local`.`ROL` TO `usuarios_demo`@`172.20.0.1`
GRANT SELECT, INSERT, UPDATE ON `puntomarket_ventas_demo_local`.`USUARIO` TO `usuarios_demo`@`172.20.0.1`
TLS_TYPE=ANY
ROLES_OK=1
'@
if (-not (Test-PermisosUsuariosDemo $valido)) { throw 'Regresión: grants correctos sin REQUIRE SSL textual deben aceptarse.' }
$casos = @(
    $valido.Replace('TLS_TYPE=ANY','TLS_TYPE='),
    $valido.Replace('TLS_TYPE=ANY',''),
    $valido.Replace('ROLES_OK=1','ROLES_OK=0'),
    $valido.Replace('SELECT, INSERT, UPDATE','SELECT, INSERT, UPDATE, DELETE'),
    $valido.Replace('SELECT, INSERT, UPDATE','SELECT, INSERT'),
    $valido.Replace('`ROL` TO','`OTRA_TABLA` TO'),
    ($valido + "`nGRANT ALL PRIVILEGES ON *.* TO ``usuarios_demo``@``172.20.0.1``"),
    ($valido + "`nGRANT SELECT ON *.* TO ``usuarios_demo``@``172.20.0.1`` WITH GRANT OPTION")
)
foreach ($caso in $casos) { if (Test-PermisosUsuariosDemo $caso) { throw 'Se aceptaron permisos/TLS no autorizados.' } }
'OK: grants sin REQUIRE SSL textual aceptados y ocho variantes inseguras rechazadas. Sin MySQL ni activación.'
