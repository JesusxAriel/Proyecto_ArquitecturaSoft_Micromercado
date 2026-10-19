using System.Globalization;
using Proyecto_Arquitectura_Micromercado.Application.Common;
using Proyecto_Arquitectura_Micromercado.Application.Customers;
using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Application.Sales;
using Proyecto_Arquitectura_Micromercado.Application.Security;
using Proyecto_Arquitectura_Micromercado.Domain.Sales;
using Proyecto_Arquitectura_Micromercado.Domain.Stock;

namespace Proyecto_Arquitectura_Micromercado.Application.Facades;

public sealed class FachadaVenta(
    IUnidadDeTrabajo unidad,
    IVentaRepository ventaRepository,
    IConPrecioVigente precios,
    IFachadaActualizacionStockProducto stock,
    IUsuarioActual usuarioActual,
    TimeProvider reloj,
    IDetectorDeInterbloqueo interbloqueos,
    IClienteConsultaRepository clientes) : IFachadaVenta
{
    private const int IntentosMaximos = 2;

    public async Task<ResultadoVentaDto> RegistrarVentaAsync(
        NuevaVentaDto dto,
        CancellationToken cancellationToken = default)
    {
        Validar(dto);

        if (!await clientes.ExisteActivoAsync(dto.IdCliente, cancellationToken))
        {
            throw new InvalidOperationException("Cliente inexistente o inactivo.");
        }

        var ordenado = dto with { Lineas = [.. dto.Lineas.OrderBy(linea => linea.IdProducto)] };

        for (var intento = 1; ; intento++)
        {
            try
            {
                return await RegistrarEnUnaTransaccionAsync(ordenado, cancellationToken);
            }
            catch (Exception ex) when (intento < IntentosMaximos && interbloqueos.EsInterbloqueo(ex))
            {
            }
        }
    }

    private async Task<ResultadoVentaDto> RegistrarEnUnaTransaccionAsync(
        NuevaVentaDto dto,
        CancellationToken cancellationToken)
    {
        await unidad.IniciarAsync(cancellationToken);
        try
        {
            var idVenta = await ventaRepository.InsertarCabeceraAsync(
                dto.IdCliente, usuarioActual.Id, cancellationToken);

            var total = 0m;
            foreach (var linea in dto.Lineas)
            {
                var precio = await precios.ObtenerPrecioVigenteAsync(linea.IdProducto, cancellationToken)
                    ?? throw new InvalidOperationException("Producto inexistente o inactivo.");

                if (linea.PrecioEsperado is { } esperado && esperado != precio)
                {
                    throw await PrecioCambiadoAsync(linea.IdProducto, precio, cancellationToken);
                }

                var consumos = await DescontarAsync(linea, cancellationToken);
                foreach (var consumo in consumos)
                {
                    await ventaRepository.InsertarDetalleAsync(
                        idVenta, linea.IdProducto, consumo.IdLote, consumo.Cantidad, precio, cancellationToken);
                    total += consumo.Cantidad * precio;
                }
            }

            var nroComprobante = Venta.GenerarNroComprobante(reloj.GetLocalNow().DateTime, idVenta);
            await ventaRepository.CerrarVentaAsync(idVenta, nroComprobante, total, cancellationToken);

            await unidad.ConfirmarAsync(cancellationToken);
            return new ResultadoVentaDto(idVenta, total);
        }
        catch
        {
            await unidad.RevertirAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task<PrecioCambiadoException> PrecioCambiadoAsync(
        int idProducto,
        decimal precioVigente,
        CancellationToken cancellationToken)
    {
        var descripcion = await precios.ObtenerDescripcionAsync(idProducto, cancellationToken);
        var producto = descripcion?.ToString() ?? $"el producto {idProducto}";
        var importe = precioVigente.ToString("0.00", CultureInfo.GetCultureInfo("es-BO"));

        return new PrecioCambiadoException(
            idProducto,
            precioVigente,
            $"El precio de {producto} cambió a Bs {importe} desde que se agregó al detalle. Vuelva a agregarlo para continuar con el precio vigente.");
    }

    private async Task<IReadOnlyList<ConsumoDeLote>> DescontarAsync(
        LineaVentaDto linea,
        CancellationToken cancellationToken)
    {
        try
        {
            return await stock.DescontarAsync(linea.IdProducto, linea.Cantidad, cancellationToken);
        }
        catch (StockInsuficienteException original)
        {
            var descripcion = await precios.ObtenerDescripcionAsync(linea.IdProducto, cancellationToken);
            if (descripcion is null) throw;
            throw new StockInsuficienteException($"Stock insuficiente para {descripcion}.", original);
        }
    }

    private static void Validar(NuevaVentaDto dto)
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
    }
}
