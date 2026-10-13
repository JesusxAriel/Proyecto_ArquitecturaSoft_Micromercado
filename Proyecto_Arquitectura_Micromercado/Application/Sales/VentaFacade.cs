using Proyecto_Arquitectura_Micromercado.Application.Security;

namespace Proyecto_Arquitectura_Micromercado.Application.Sales;

public class VentaFacade(
    IUsuarioActual usuarioActual,
    IVentaRepository ventaRepository) : IVentaFacade
{
    public Task<decimal> ObtenerVentasDelDiaAsync(CancellationToken cancellationToken)
        => ventaRepository.GetVentasDelDiaAsync(cancellationToken);

    public async Task<ResultadoVentaDto> RegistrarVentaAsync(NuevaVentaDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.IdCliente <= 0)
        {
            throw new InvalidOperationException("Debe seleccionar un cliente para registrar la venta.");
        }

        if (dto.Lineas == null || dto.Lineas.Count == 0)
        {
            throw new InvalidOperationException("Debe seleccionar al menos un producto para registrar la venta.");
        }

        if (dto.Lineas.Any(linea => linea is null || linea.IdProducto <= 0 || linea.Cantidad <= 0))
        {
            throw new InvalidOperationException("Cada línea debe tener un producto válido y una cantidad mayor a cero.");
        }

        if (dto.EfectivoPagado < 0)
        {
            throw new InvalidOperationException("El efectivo pagado no puede ser negativo.");
        }

        var (idVenta, _, total) = await ventaRepository.RegistrarVentaConSpAsync(
            dto, usuarioActual.Id, cancellationToken);

        return new ResultadoVentaDto(idVenta, total);
    }

    public Task<bool> AnularVentaAsync(int idVenta, CancellationToken cancellationToken = default)
        => ventaRepository.AnularVentaConSpAsync(
            idVenta, "Anulación solicitada desde la interfaz", usuarioActual.Id, cancellationToken);
}
