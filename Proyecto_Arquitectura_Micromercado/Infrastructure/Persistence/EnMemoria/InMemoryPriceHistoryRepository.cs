using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Domain.Products;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.EnMemoria;

public sealed class InMemoryPriceHistoryRepository : IPriceHistoryRepository
{
    private readonly List<ProductPriceHistory> historial = [];
    private int ultimoId;

    public Task<IReadOnlyList<ProductPriceHistory>> GetPriceHistoryAsync(
        CancellationToken cancellationToken = default)
    {
        return ObtenerOrdenados(historial);
    }

    public Task<IReadOnlyList<ProductPriceHistory>> GetPriceHistoryByProductIdAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(productId);

        return ObtenerOrdenados(historial.Where(h => h.IdProducto == productId));
    }

    public Task<ProductPriceHistory?> GetPriceHistoryByIdAsync(
        int historyId,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(historyId);

        var encontrado = historial.FirstOrDefault(h => h.Id == historyId);
        return Task.FromResult(encontrado is null ? null : Clonar(encontrado));
    }

    private static Task<IReadOnlyList<ProductPriceHistory>> ObtenerOrdenados(
        IEnumerable<ProductPriceHistory> registros)
    {
        IReadOnlyList<ProductPriceHistory> resultado = registros
            .OrderByDescending(h => h.FechaCambio)
            .ThenByDescending(h => h.Id)
            .Select(Clonar)
            .ToList();

        return Task.FromResult(resultado);
    }

    public Task AddPriceHistoryAsync(
        ProductPriceHistory history,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(history);

        var nuevo = Clonar(history);
        nuevo.Id = ++ultimoId;

        if (nuevo.FechaCambio == default)
        {
            nuevo.FechaCambio = DateTime.Now;
        }

        historial.Add(nuevo);

        return Task.CompletedTask;
    }

    private static ProductPriceHistory Clonar(ProductPriceHistory origen) => new()
    {
        Id = origen.Id,
        IdProducto = origen.IdProducto,
        NombreProducto = origen.NombreProducto,
        PrecioVentaAnterior = origen.PrecioVentaAnterior,
        PrecioVentaNuevo = origen.PrecioVentaNuevo,
        PrecioCostoAnterior = origen.PrecioCostoAnterior,
        PrecioCostoNuevo = origen.PrecioCostoNuevo,
        MotivoCambio = origen.MotivoCambio,
        IdUsuario = origen.IdUsuario,
        FechaCambio = origen.FechaCambio
    };
}
