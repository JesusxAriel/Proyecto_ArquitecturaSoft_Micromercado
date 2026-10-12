using Proyecto_Arquitectura_Micromercado.Application.Common;

namespace Proyecto_Arquitectura_Micromercado.Application.Sales;

public class VentaFacade(
    IUnidadDeTrabajo unidadDeTrabajo,
    IVentaRepository ventaRepository) : IVentaFacade
{
    public async Task<ResultadoVentaDto> RegistrarVentaAsync(NuevaVentaDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.Lineas == null || dto.Lineas.Count == 0)
        {
            throw new InvalidOperationException("Debe seleccionar al menos un producto para registrar la venta.");
        }

        await unidadDeTrabajo.IniciarAsync(cancellationToken);

        try
        {
            var (idVenta, nroComprobante, total) = await ventaRepository.RegistrarVentaConSpAsync(dto, cancellationToken);

            await unidadDeTrabajo.ConfirmarAsync(cancellationToken);

            return new ResultadoVentaDto(idVenta, total);
        }
        catch
        {
            await unidadDeTrabajo.RevertirAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> AnularVentaAsync(int idVenta, CancellationToken cancellationToken = default)
    {
        await unidadDeTrabajo.IniciarAsync(cancellationToken);

        try
        {
            var exito = await ventaRepository.AnularVentaConSpAsync(idVenta, "Anulación solicitada desde la interfaz", cancellationToken);

            await unidadDeTrabajo.ConfirmarAsync(cancellationToken);
            return exito;
        }
        catch
        {
            await unidadDeTrabajo.RevertirAsync(cancellationToken);
            throw;
        }
    }
}