namespace Proyecto_Arquitectura_Micromercado.Application.Sales;

public interface IVentaConsultaService
{
    Task<decimal> ObtenerVentasDelDiaAsync(CancellationToken cancellationToken = default);
}
