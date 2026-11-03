using PuntoMarket.Reportes.Domain.ValueObjects;
using PuntoMarket.Reportes.Domain.VentasPorCategoria;

namespace PuntoMarket.Reportes.Application.Ports.Consultas;

public interface IConsultaVentasPorCategoria
{
    Task<IReadOnlyList<FilaDeCategoria>> ConsultarAsync(
        RangoDeFechas rango,
        int? idCategoria,
        int? top,
        CancellationToken cancelacion);
}
