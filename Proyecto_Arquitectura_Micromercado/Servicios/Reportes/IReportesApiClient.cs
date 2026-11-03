using Proyecto_Arquitectura_Micromercado.Dtos.Reportes;

namespace Proyecto_Arquitectura_Micromercado.Servicios.Reportes;

public interface IReportesApiClient
{
    Task<RespuestaVentasPorCategoriaDto> ObtenerVentasPorCategoriaAsync(
        FiltrosVentasPorCategoriaDto filtros,
        CancellationToken cancellationToken);

    Task<ArchivoDescargadoDto> DescargarVentasPorCategoriaAsync(
        FiltrosVentasPorCategoriaDto filtros,
        FormatoDeDescarga formato,
        CancellationToken cancellationToken);
}
