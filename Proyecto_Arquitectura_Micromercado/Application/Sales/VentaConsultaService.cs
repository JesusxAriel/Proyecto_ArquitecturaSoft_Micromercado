namespace Proyecto_Arquitectura_Micromercado.Application.Sales;

public sealed class VentaConsultaService(IVentaRepository ventaRepository) : IVentaConsultaService
{
    public Task<decimal> ObtenerVentasDelDiaAsync(CancellationToken cancellationToken = default) =>
        ventaRepository.GetVentasDelDiaAsync(cancellationToken);
}
