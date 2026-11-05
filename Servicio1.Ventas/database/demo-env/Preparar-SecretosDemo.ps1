#requires -Version 7.0
# Ejecutar contrasenas interactivamente en PowerShell local, nunca en logs/chat.
[CmdletBinding()]
param([Parameter(Mandatory)][ValidateSet('Hashes', 'Jwt', 'PasswordRoot', 'PasswordVentas')][string]$Paso)
$ErrorActionPreference = 'Stop'
$privado = Join-Path $env:USERPROFILE 'puntomarket-demo-secrets'
if (-not (Test-Path -LiteralPath $privado)) {
    New-Item -ItemType Directory -Path $privado | Out-Null
    $acl = Get-Acl -LiteralPath $privado
    $acl.SetAccessRuleProtection($true, $false)
    $usuario = [Security.Principal.WindowsIdentity]::GetCurrent().User
    $regla = [Security.AccessControl.FileSystemAccessRule]::new($usuario, 'FullControl', 'ContainerInherit,ObjectInherit', 'None', 'Allow')
    $acl.AddAccessRule($regla)
    Set-Acl -LiteralPath $privado -AclObject $acl
}
function Escribir-Nuevo([string]$ruta, [string]$valor) {
    $stream = [IO.File]::Open($ruta, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
    try {
        $bytes = [Text.UTF8Encoding]::new($false).GetBytes($valor)
        $stream.Write($bytes, 0, $bytes.Length)
    } finally {
        if ($null -ne $bytes) { [Array]::Clear($bytes, 0, $bytes.Length) }
        $stream.Dispose()
    }
}
if ($Paso -eq 'Jwt') {
    $ruta = Join-Path $privado 'jwt-demo-key.txt'
    if (Test-Path -LiteralPath $ruta) { throw 'La clave ya existe; no se modifica.' }
    Escribir-Nuevo $ruta ([Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48)))
    Write-Output 'Clave JWT aleatoria creada fuera del repositorio; no se imprime.'
    return
}
if ($Paso -eq 'Hashes') {
    foreach ($archivo in @('admin-password-hash.txt','cajero-password-hash.txt')) {
        if (Test-Path -LiteralPath (Join-Path $privado $archivo)) { throw 'Ya existen hashes privados: no se sobrescriben.' }
    }
    $bin = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../Servicio2.Usuarios/tools/PuntoMarket.Usuarios.DemoLocal/bin'))
    $dll = Get-ChildItem -LiteralPath $bin -Filter 'BCrypt-Net-Next.dll' -Recurse -File | Select-Object -First 1
    if ($null -eq $dll) { throw 'Compile PuntoMarket.slnx antes de preparar hashes privados.' }
    [void][Reflection.Assembly]::LoadFrom($dll.FullName)
    $hashes = @{}
    try {
        foreach ($nombre in @('admin','cajero1')) {
            $secreto = Read-Host "Contrasena NUEVA de bootstrap para $nombre (oculta)" -AsSecureString
            $confirmacion = Read-Host 'Confirmacion (oculta)' -AsSecureString
            $texto = [Net.NetworkCredential]::new('', $secreto).Password
            $repetido = [Net.NetworkCredential]::new('', $confirmacion).Password
            if ($texto -cne $repetido -or [string]::IsNullOrWhiteSpace($texto) -or $texto.Length -lt 8 -or
                -not ($texto.ToCharArray() | Where-Object { [char]::IsUpper($_) }) -or
                -not ($texto.ToCharArray() | Where-Object { [char]::IsLower($_) }) -or
                -not ($texto.ToCharArray() | Where-Object { [char]::IsDigit($_) }) -or
                -not ($texto.ToCharArray() | Where-Object { -not [char]::IsLetterOrDigit($_) })) {
                throw 'Las contrasenas no coinciden o no cumplen la politica de Usuarios.'
            }
            $hashes[$nombre] = [BCrypt.Net.BCrypt]::HashPassword($texto, 12)
            $texto = $null; $repetido = $null; $secreto.Dispose(); $confirmacion.Dispose()
        }
        Escribir-Nuevo (Join-Path $privado 'admin-password-hash.txt') $hashes.admin
        Escribir-Nuevo (Join-Path $privado 'cajero-password-hash.txt') $hashes.cajero1
        Write-Output 'Hashes propios de bootstrap generados privadamente. No se conecto a MySQL.'
    } catch { throw 'No se prepararon todos los hashes. Revisar archivos privados sin sobrescribirlos; detalles omitidos.' }
    finally { $hashes.Clear(); $texto=$null; $repetido=$null; if ($secreto) {$secreto.Dispose()}; if ($confirmacion) {$confirmacion.Dispose()} }
    return
}
$archivo = if ($Paso -eq 'PasswordRoot') {'mysql-root.txt'} else {'mysql-ventas.txt'}
$ruta = Join-Path $privado $archivo
if (Test-Path -LiteralPath $ruta) { throw 'El archivo ya existe. No se sobrescribio.' }
$secreto = Read-Host 'Introduce una contrasena NUEVA exclusiva de Demo (entrada oculta)' -AsSecureString
$confirmacion = Read-Host 'Repite la misma contrasena (entrada oculta)' -AsSecureString
$ptr = [IntPtr]::Zero
$ptrConfirmacion = [IntPtr]::Zero
try {
    $ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secreto)
    $ptrConfirmacion = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($confirmacion)
    $texto = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr)
    $repetido = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptrConfirmacion)
    if ([string]::IsNullOrWhiteSpace($texto) -or $texto.Contains("`n") -or $texto.Contains("`r")) { throw 'Contrasena vacia o con saltos de linea: no se guardo.' }
    if ($texto -cne $repetido) { throw 'Las entradas no coinciden: no se guardo.' }
    $otroArchivo = if ($Paso -eq 'PasswordRoot') {'mysql-ventas.txt'} else {'mysql-root.txt'}
    $otro = Join-Path $privado $otroArchivo
    if ((Test-Path -LiteralPath $otro) -and [IO.File]::ReadAllText($otro) -ceq $texto) { throw 'Root y usuario Demo deben tener contrasenas distintas.' }
    Escribir-Nuevo $ruta $texto
    Write-Output 'Contrasena guardada solo en el archivo privado externo, sin mostrarla.'
} finally {
    if ($ptr -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr) }
    if ($ptrConfirmacion -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptrConfirmacion) }
    $texto = $null
    $repetido = $null
    $secreto.Dispose()
    $confirmacion.Dispose()
}
