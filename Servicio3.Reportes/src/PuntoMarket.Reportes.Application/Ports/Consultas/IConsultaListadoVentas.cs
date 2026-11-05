using PuntoMarket.Reportes.Domain.ListadoVentas;
using PuntoMarket.Reportes.Domain.ValueObjects;

namespace PuntoMarket.Reportes.Application.Ports.Consultas;

public interface IConsultaListadoVentas
{
    Task<ResultadoListadoVentas> ConsultarAsync(
        RangoDeFechas rango,
        EstadoDeVenta? estado,
        string? cliente,
        int? tope,
        CancellationToken cancelacion);
}

// Filas respeta el límite; Totales incluye todas las coincidencias de los filtros, sin límite.
public sealed record ResultadoListadoVentas(IReadOnlyList<FilaDeVenta> Filas, TotalesListadoVentas Totales);
