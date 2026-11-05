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

public sealed record ResultadoVentasPorCategoria(IReadOnlyList<FilaDeCategoria> Filas, TotalesPorCategoria Totales);
