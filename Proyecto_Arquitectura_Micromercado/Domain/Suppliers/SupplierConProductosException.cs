namespace Proyecto_Arquitectura_Micromercado.Domain.Suppliers;

public sealed class SupplierConProductosException : InvalidOperationException
{
    public const string Mensaje =
        "No se puede eliminar el proveedor porque tiene productos asociados.";

    public SupplierConProductosException()
        : base(Mensaje)
    {
    }
}
