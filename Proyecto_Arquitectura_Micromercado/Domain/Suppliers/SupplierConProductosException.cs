namespace Proyecto_Arquitectura_Micromercado.Domain.Suppliers;

// Se lanza al intentar dar de baja un proveedor que todavía tiene productos activos.
// Es una regla de negocio, no un fallo de infraestructura: la página muestra su mensaje.
public sealed class SupplierConProductosException : InvalidOperationException
{
    public const string Mensaje =
        "No se puede eliminar el proveedor porque tiene productos asociados.";

    public SupplierConProductosException()
        : base(Mensaje)
    {
    }
}
