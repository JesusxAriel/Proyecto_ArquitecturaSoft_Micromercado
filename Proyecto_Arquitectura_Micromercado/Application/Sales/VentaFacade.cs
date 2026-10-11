using Proyecto_Arquitectura_Micromercado.Application.Common;
using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Domain.Sales;

namespace Proyecto_Arquitectura_Micromercado.Application.Sales;

public class VentaFacade(
    IUnidadDeTrabajo unidadDeTrabajo,
    IVentaRepository ventaRepository,
    IProductRepository productRepository) : IVentaFacade
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
            var venta = new Venta
            {
                NroComprobante = $"V-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}",
                FechaHora = DateTime.Now,
                IdCliente = dto.IdCliente > 0 ? dto.IdCliente : 1,
                IdUsuario = 1,
                Estado = "EN_PROCESO"
            };

            var productos = await productRepository.GetAllAsync(cancellationToken);

            foreach (var linea in dto.Lineas)
            {
                var producto = productos.FirstOrDefault(p => p.Id == linea.IdProducto)
                    ?? throw new KeyNotFoundException($"El producto seleccionado (ID {linea.IdProducto}) no existe.");

                if (producto.StockCalculado < linea.Cantidad)
                {
                    throw new InvalidOperationException($"Stock insuficiente para '{producto.Nombre}'. Disponible: {producto.StockCalculado}.");
                }

                venta.AgregarDetalle(producto.Id, linea.Cantidad, producto.PrecioVenta);
            }

            venta.CompletarVenta();

            var idVenta = await ventaRepository.CrearVentaAsync(venta, cancellationToken);

            foreach (var detalle in venta.Detalles)
            {
                detalle.IdVenta = idVenta;
                await ventaRepository.CrearDetalleVentaAsync(detalle, cancellationToken);
            }

            await unidadDeTrabajo.ConfirmarAsync(cancellationToken);

            return new ResultadoVentaDto(idVenta, venta.Total);
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
            var venta = await ventaRepository.GetByIdAsync(idVenta, cancellationToken);
            if (venta == null || (venta.Estado != null && venta.Estado.StartsWith("ANULAD", StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            var exito = await ventaRepository.CambiarEstadoAnuladoAsync(idVenta, "Anulación solicitada desde la interfaz", cancellationToken);

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