namespace Proyecto_Arquitectura_Micromercado.Application.Products;

public interface IConConsultaPorProveedor
{
    Task<bool> ExistenProductosActivosDelProveedorAsync(
        int idProveedor,
        CancellationToken cancellationToken = default);
}
