namespace Proyecto_Arquitectura_Micromercado.Application.Products;

public sealed record ProductoParaVenta(
    int Id,
    string Nombre,
    string Presentacion,
    decimal PrecioVenta,
    int StockDisponible);

public sealed record ResultadoBusquedaParaVenta(
    IReadOnlyList<ProductoParaVenta> Productos,
    bool HayMas)
{
    public static ResultadoBusquedaParaVenta Vacio { get; } = new([], false);
}
