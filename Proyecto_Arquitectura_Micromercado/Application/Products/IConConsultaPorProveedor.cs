namespace Proyecto_Arquitectura_Micromercado.Application.Products;

public interface IConConsultaPorProveedor
{
    // Responde con una consulta directa, sin traer los productos: la usa el servicio de
    // proveedores para impedir que se dé de baja un proveedor que aún tiene productos.
    Task<bool> ExistenProductosActivosDelProveedorAsync(
        int idProveedor,
        CancellationToken cancellationToken = default);
}
