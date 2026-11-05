namespace PuntoMarket.Ventas.Application.Products;

public interface IConPrecioVigente
{
    Task<decimal?> ObtenerPrecioVigenteAsync(
        int idProducto,
        CancellationToken cancellationToken = default);

    Task<DescripcionDeProducto?> ObtenerDescripcionAsync(
        int idProducto,
        CancellationToken cancellationToken = default);
}
