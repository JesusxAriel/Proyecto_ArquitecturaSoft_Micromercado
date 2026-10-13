namespace Proyecto_Arquitectura_Micromercado.Domain.Suppliers;

public static class SupplierValidation
{
    public const string TelefonoPattern = @"^(\+591\s?)?[67]\d{7}$";

    public const string TelefonoMessage =
        "Ingrese un celular boliviano de 8 dígitos que empiece con 6 o 7. Ej: 71234567 o +591 71234567";
    public const string CorreoMessage =
        "Formato inválido. Ej: ventas@empresa.com.bo";
    public static string NormalizeTelefono(string? value)
    {
        var telefono = (value ?? string.Empty).Trim();
        return telefono.StartsWith("+591", StringComparison.Ordinal)
            ? telefono[4..].Trim()
            : telefono;
    }

    public const string NombreDuplicadoMessage =
        "Ya existe un proveedor con ese nombre.";
    public const string CorreoRequeridoParaAutogestionadoMessage =
        "Un proveedor autogestionado necesita un correo para coordinar sus reposiciones.";
}
