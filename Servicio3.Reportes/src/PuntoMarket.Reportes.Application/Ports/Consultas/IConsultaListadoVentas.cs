using PuntoMarket.Reportes.Domain.ListadoVentas;
using PuntoMarket.Reportes.Domain.ValueObjects;

namespace PuntoMarket.Reportes.Application.Ports.Consultas;

public interface IConsultaListadoVentas
{
    Task<IReadOnlyList<FilaDeVenta>> ConsultarAsync(
        RangoDeFechas rango,
        EstadoDeVenta? estado,
        string? cliente,
        int? tope,
        CancellationToken cancelacion);
}
