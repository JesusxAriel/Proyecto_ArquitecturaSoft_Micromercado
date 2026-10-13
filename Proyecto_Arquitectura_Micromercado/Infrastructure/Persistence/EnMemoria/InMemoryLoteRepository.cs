using Proyecto_Arquitectura_Micromercado.Application.Stock;
using Proyecto_Arquitectura_Micromercado.Domain.Stock;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.EnMemoria;

public sealed class InMemoryLoteRepository : ILoteRepository
{
    private sealed class LoteAlmacenado(int idProducto, LoteDisponible lote)
    {
        public int IdProducto { get; } = idProducto;
        public int IdLote { get; } = lote.IdLote;
        public int CantidadDisponible { get; set; } = lote.CantidadDisponible;
        public DateOnly? FechaVencimiento { get; } = lote.FechaVencimiento;
    }

    private readonly List<LoteAlmacenado> lotes = [];

    public void Agregar(int idProducto, LoteDisponible lote)
    {
        ArgumentNullException.ThrowIfNull(lote);
        lotes.Add(new LoteAlmacenado(idProducto, lote));
    }

    public Task<IReadOnlyList<LoteDisponible>> ObtenerVigentesParaDescuentoAsync(
        int idProducto,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<LoteDisponible> resultado = lotes
            .Where(l => l.IdProducto == idProducto && l.CantidadDisponible > 0)
            .Select(l => new LoteDisponible(l.IdLote, l.CantidadDisponible, l.FechaVencimiento))
            .ToList();

        return Task.FromResult(resultado);
    }

    public Task DescontarAsync(int idLote, int cantidad, CancellationToken cancellationToken = default)
    {
        var lote = lotes.FirstOrDefault(l => l.IdLote == idLote);

        if (lote is null || lote.CantidadDisponible < cantidad)
        {
            throw new StockInsuficienteException();
        }

        lote.CantidadDisponible -= cantidad;

        return Task.CompletedTask;
    }

    public Task RestaurarAsync(int idLote, int cantidad, CancellationToken cancellationToken = default)
    {
        var lote = lotes.FirstOrDefault(l => l.IdLote == idLote);

        if (lote is not null)
        {
            lote.CantidadDisponible += cantidad;
        }

        return Task.CompletedTask;
    }
}
