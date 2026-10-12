namespace Proyecto_Arquitectura_Micromercado.Application.Sales;

public interface IVentaFacade
{
    Task<ResultadoVentaDto> RegistrarVentaAsync(NuevaVentaDto dto, CancellationToken cancellationToken = default);
    Task<bool> AnularVentaAsync(int idVenta, CancellationToken cancellationToken = default);
}