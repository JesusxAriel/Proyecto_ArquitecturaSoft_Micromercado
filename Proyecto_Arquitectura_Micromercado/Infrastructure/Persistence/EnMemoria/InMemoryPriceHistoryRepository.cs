using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Domain.Products;
using System.Data;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.EnMemoria;

// Producto Concreto alternativo de IPriceHistoryRepository.
// Igual que en MySQL, el historial se devuelve del cambio más reciente al más antiguo.
public sealed class InMemoryPriceHistoryRepository : IPriceHistoryRepository
{
    private readonly List<ProductPriceHistory> historial = [];
    private int ultimoId;

    public Task<IReadOnlyList<ProductPriceHistory>> GetPriceHistoryAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ProductPriceHistory> resultado = historial
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

    // La sobrecarga transaccional existe para que el repositorio de producto pueda
    // delegar dentro de una transacción. En memoria no hay transacción, así que se
    // ignoran la conexión y la transacción. Aceptar cualquier conexión (incluida null)
    // es una precondición más débil que la de MySQL, por lo que no se rompe el LSP.
    public Task AddPriceHistoryAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        ProductPriceHistory history,
        CancellationToken cancellationToken = default) =>
        AddPriceHistoryAsync(history, cancellationToken);

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
