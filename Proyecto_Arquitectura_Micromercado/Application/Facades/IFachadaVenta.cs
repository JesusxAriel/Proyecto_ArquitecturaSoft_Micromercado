using Proyecto_Arquitectura_Micromercado.Application.Sales;

namespace Proyecto_Arquitectura_Micromercado.Application.Facades;

public interface IFachadaVenta
{
    Task<ResultadoVentaDto> RegistrarVentaAsync(
        NuevaVentaDto venta,
        CancellationToken cancellationToken = default);
}
