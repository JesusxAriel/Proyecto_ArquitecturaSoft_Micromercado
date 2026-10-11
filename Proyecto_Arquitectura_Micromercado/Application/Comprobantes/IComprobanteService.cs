namespace Proyecto_Arquitectura_Micromercado.Application.Comprobantes;

// Puerto de entrada del caso de uso "ver el comprobante de una venta".
public interface IComprobanteService
{
    // Devuelve el comprobante con la fecha y hora de este momento y el usuario que lo
    // esta generando, o null si la venta no existe.
    Task<ComprobanteDto?> ObtenerAsync(
        int idVenta,
        CancellationToken cancellationToken = default);
}
