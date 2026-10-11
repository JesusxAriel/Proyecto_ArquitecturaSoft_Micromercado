namespace Proyecto_Arquitectura_Micromercado.Application.Comprobantes;

// Puerto de salida de solo lectura para armar el comprobante de una venta.
//
// Es un puerto aparte y no un metodo mas de un repositorio existente: el comprobante solo
// consulta, y asi no obliga a cambiar las firmas de otros repositorios.
public interface IComprobanteRepository
{
    // Devuelve la venta con su detalle SIN agrupar (una fila por lote), o null si no
    // existe una venta con ese identificador. Incluye las ventas anuladas.
    Task<VentaParaComprobante?> ObtenerPorIdVentaAsync(
        int idVenta,
        CancellationToken cancellationToken = default);
}
