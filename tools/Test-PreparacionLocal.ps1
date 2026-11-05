#requires -Version 7.0
# AST y biblioteca con datos sinteticos: no ejecuta inicializadores ni usa secretos.
$ErrorActionPreference = 'Stop'
$ruta = Join-Path $PSScriptRoot 'Preparar-CuentasLocales.ps1'
$tokens = $null; $errores = $null
$ast = [Management.Automation.Language.Parser]::ParseFile($ruta,[ref]$tokens,[ref]$errores)
if ($errores.Count) { throw 'Sintaxis incorrecta.' }
$guarda = $ast.Find({param($n) $n -is [Management.Automation.Language.IfStatementAst] -and $n.Extent.Text.StartsWith('if (-not $Aplicar)')},$true)
if (-not $guarda) { throw 'Falta guarda explicita.' }
$Aplicar = $false; $rechazo = $false
try { & ([scriptblock]::Create($guarda.Extent.Text)) } catch { $rechazo = $true }
if (-not $rechazo) { throw 'El preparador permite ejecucion sin autorizacion.' }
$Aplicar = $true
& ([scriptblock]::Create($guarda.Extent.Text))
$bin = Join-Path $PSScriptRoot '../Servicio2.Usuarios/tools/PuntoMarket.Usuarios.DemoLocal/bin'
$dll = Get-ChildItem -LiteralPath $bin -Filter BCrypt-Net-Next.dll -Recurse -File | Select-Object -First 1
if (-not $dll) { throw 'Compile PuntoMarket.slnx antes de este test.' }
[void][Reflection.Assembly]::LoadFrom($dll.FullName)
$hash = [BCrypt.Net.BCrypt]::HashPassword('SoloFixtureNoReal!123',12)
if (-not [BCrypt.Net.BCrypt]::Verify('SoloFixtureNoReal!123',$hash) -or
    $hash -notmatch '^\$2[aby]\$\d{2}\$[./A-Za-z0-9]{53}$') { throw 'Formato BCrypt incompatible.' }
$hash = $null
Write-Output 'OK: guarda de ejecucion y BCrypt compatible. Sin Docker, MySQL, archivos privados ni credenciales reales.'
