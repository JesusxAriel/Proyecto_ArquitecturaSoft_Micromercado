using Proyecto_Arquitectura_Micromercado.Application.Stock;
using Proyecto_Arquitectura_Micromercado.Domain.Stock;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.EnMemoria;

public sealed class InMemoryLoteRepository : ILoteRepository
{
    private sealed class LoteAlmacenado(int idProducto, LoteDisponible lote, int? cantidadInicial)
    {
        public int IdProducto { get; } = idProducto;
        public int IdLote { get; } = lote.IdLote;
        public int CantidadDisponible { get; set; } = lote.CantidadDisponible;
        public int? CantidadInicial { get; } = cantidadInicial;
        public bool Activo { get; set; } = true;
        public int? IdUsuarioActualizacion { get; set; }
        public DateOnly? FechaVencimiento { get; } = lote.FechaVencimiento;
    }

    private readonly List<LoteAlmacenado> lotes = [];

    public void Agregar(int idProducto, LoteDisponible lote, int? cantidadInicial = null)
    {
        ArgumentNullException.ThrowIfNull(lote);
        lotes.Add(new LoteAlmacenado(idProducto, lote, cantidadInicial));
    }

    public Task<IReadOnlyList<LoteDisponible>> ObtenerVigentesParaDescuentoAsync(
        int idProducto,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<LoteDisponible> resultado = lotes
            .Where(l => l.IdProducto == idProducto && l.Activo && l.CantidadDisponible > 0)
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

        if (lote is null)
        {
            return Task.CompletedTask;
        }

        if (lote.CantidadInicial is int inicial && lote.CantidadDisponible + cantidad > inicial)
        {
            throw new RestauracionExcedeCantidadInicialException();
        }

        lote.CantidadDisponible += cantidad;

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<LoteDisponible>> ObtenerVencidosConStockAsync(
        DateOnly hoy,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<LoteDisponible> resultado = lotes
            .Where(l => l.Activo && l.CantidadDisponible > 0 && l.FechaVencimiento is DateOnly vence && vence < hoy)
            .OrderBy(l => l.IdLote)
            .Select(l => new LoteDisponible(l.IdLote, l.CantidadDisponible, l.FechaVencimiento))
            .ToList();

        return Task.FromResult(resultado);
    }

    public Task DesactivarAsync(int idLote, int idUsuario, CancellationToken cancellationToken = default)
    {
        var lote = lotes.FirstOrDefault(l => l.IdLote == idLote && l.Activo);

        if (lote is not null)
        {
            lote.Activo = false;
            lote.IdUsuarioActualizacion = idUsuario;
        }

        return Task.CompletedTask;
    }

    public bool EstaActivo(int idLote) => lotes.Single(l => l.IdLote == idLote).Activo;

    public int? IdUsuarioActualizacionDe(int idLote) => lotes.Single(l => l.IdLote == idLote).IdUsuarioActualizacion;
}
