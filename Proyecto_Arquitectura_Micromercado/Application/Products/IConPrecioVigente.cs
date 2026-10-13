namespace Proyecto_Arquitectura_Micromercado.Application.Products;

public interface IConPrecioVigente
{
    Task<decimal?> ObtenerPrecioVigenteAsync(
        int idProducto,
        CancellationToken cancellationToken = default);
}
