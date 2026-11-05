using PuntoMarket.Reportes.Domain.ValueObjects;

namespace PuntoMarket.Reportes.Application.Ports.Consultas;

public interface IConsultaTendenciaDeVentas
{
    Task<IReadOnlyList<PuntoDeTendencia>> ConsultarAsync(
        RangoDeFechas rango,
        IReadOnlyList<int> idsDeCategorias,
        CancellationToken cancelacion);
}
