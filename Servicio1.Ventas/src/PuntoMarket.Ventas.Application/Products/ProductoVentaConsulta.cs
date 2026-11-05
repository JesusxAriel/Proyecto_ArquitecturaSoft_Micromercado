namespace PuntoMarket.Ventas.Application.Products;

public sealed record ProductoParaVenta(int Id, string Nombre, string Presentacion, decimal PrecioVenta, int StockDisponible);
public sealed record ResultadoBusquedaParaVenta(IReadOnlyList<ProductoParaVenta> Productos, bool HayMas)
{
    public int Total => Productos.Count;
    public string? Mensaje => null;
}

/// <summary>Lecturas sin bloqueos de escritura. Disponibilidad informativa, no reserva stock.</summary>
public interface IProductoVentaConsultaRepository
{
    Task<ResultadoBusquedaParaVenta> BuscarAsync(string termino, int limite, DateOnly hoy, CancellationToken cancellationToken = default);
    Task<ProductoParaVenta?> ObtenerAsync(int id, DateOnly hoy, CancellationToken cancellationToken = default);
}

public interface IProductoVentaConsultaService
{
    Task<ResultadoBusquedaParaVenta> BuscarAsync(string? termino, CancellationToken cancellationToken = default);
    Task<ProductoParaVenta?> ObtenerAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class ProductoVentaConsultaService(IProductoVentaConsultaRepository repositorio, TimeProvider reloj) : IProductoVentaConsultaService
{
    public Task<ResultadoBusquedaParaVenta> BuscarAsync(string? termino, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizado = termino?.Trim();
        if (normalizado is { Length: > 150 }) throw new ArgumentException("El término admite como máximo 150 caracteres.", nameof(termino));
        if (string.IsNullOrEmpty(normalizado) || normalizado.Length < 2)
            return Task.FromResult(new ResultadoBusquedaParaVenta([], false));
        return repositorio.BuscarAsync(normalizado, 10, Hoy(), cancellationToken);
    }

    public Task<ProductoParaVenta?> ObtenerAsync(int id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return id <= 0 ? Task.FromResult<ProductoParaVenta?>(null) : repositorio.ObtenerAsync(id, Hoy(), cancellationToken);
    }

    private DateOnly Hoy() => DateOnly.FromDateTime(reloj.GetLocalNow().DateTime);
}
