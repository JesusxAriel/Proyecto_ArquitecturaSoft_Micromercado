namespace Proyecto_Arquitectura_Micromercado.Domain.Suppliers;

// Entidad del negocio. No lleva atributos de validación de pantalla: eso es del modelo del
// formulario (Pages/Suppliers/SupplierFormModel) y las reglas de negocio las aplica
// SupplierService.
public sealed class Supplier
{
    public int Id { get; set; }

    public string NombreEmpresa { get; set; } = string.Empty;

    public string NumeroEmpresa { get; set; } = string.Empty;

    public string? CorreoReferencia { get; set; }

    public bool EsAutogestionado { get; set; }

    public bool EstaActivo { get; set; } = true;
}

public sealed class SupplierListItem
{
    public int Id { get; init; }
    public string NombreEmpresa { get; init; } = string.Empty;
    public string NumeroEmpresa { get; init; } = string.Empty;
    public string CorreoReferencia { get; init; } = string.Empty;
    public bool EsAutogestionado { get; init; }
}