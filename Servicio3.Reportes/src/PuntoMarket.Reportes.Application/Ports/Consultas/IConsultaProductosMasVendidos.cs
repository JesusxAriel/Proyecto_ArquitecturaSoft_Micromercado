using PuntoMarket.Reportes.Domain.ValueObjects;
using PuntoMarket.Reportes.Domain.VentasPorCategoria;

namespace PuntoMarket.Reportes.Application.Ports.Consultas;

public interface IConsultaProductosMasVendidos
{
    Task<IReadOnlyList<FilaDeProducto>> ConsultarAsync(
        RangoDeFechas rango,
        IReadOnlyList<int> idsDeCategorias,
        int limite,
        CancellationToken cancelacion);
}
