using PuntoMarket.Reportes.Domain.ValueObjects;
using PuntoMarket.Reportes.Domain.VentasPorCategoria;

namespace PuntoMarket.Reportes.Application.Ports.Consultas;

public interface IConsultaVentasPorCategoria
{
    Task<ResultadoVentasPorCategoria> ConsultarAsync(
        RangoDeFechas rango,
        int? idCategoria,
        int? top,
        CancellationToken cancelacion);
}

// Filas respeta el límite; Totales incluye todas las coincidencias de los filtros, sin límite.
public sealed record ResultadoVentasPorCategoria(IReadOnlyList<FilaDeCategoria> Filas, TotalesPorCategoria Totales);
