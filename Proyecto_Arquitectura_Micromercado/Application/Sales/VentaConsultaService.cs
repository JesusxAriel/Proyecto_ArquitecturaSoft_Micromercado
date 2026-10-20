
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Proyecto_Arquitectura_Micromercado.Application.Sales;

public sealed class VentaConsultaService(
    IVentaRepository ventaRepository,
    IVentaConsultaRepository consultaRepository,
    IDistributedCache cache,
    ILogger<VentaConsultaService> logger) : IVentaConsultaService
{
    private const int LimiteHistorial = 50;

    private static readonly TimeSpan DuracionCache =
        TimeSpan.FromSeconds(30);

    public Task<decimal> ObtenerVentasDelDiaAsync(
        CancellationToken cancellationToken = default) =>
        ventaRepository.GetVentasDelDiaAsync(cancellationToken);

    public async Task<IReadOnlyList<VentaResumenDto>> ListarAsync(
        bool incluirAnuladas,
        CancellationToken cancellationToken = default)
    {
        // Clave diferente para cada tipo de historial.
        var clave = $"ventas:historial:incluir-anuladas:{incluirAnuladas}";

        // 1. Intentar recuperar el historial desde Redis.
        try
        {
            var json = await cache.GetStringAsync(
                clave,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(json))
            {
                var ventasGuardadas =
                    JsonSerializer.Deserialize<List<VentaResumenDto>>(json);

                if (ventasGuardadas is not null)
                {
                    return ventasGuardadas;
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Si Redis falla, se continúa consultando MySQL.
            logger.LogWarning(
                ex,
                "No se pudo leer el historial de ventas desde Redis.");
        }

        // 2. Si no existe la caché, consultar MySQL.
        var ventas = await consultaRepository.ListarAsync(
            incluirAnuladas,
            LimiteHistorial,
            cancellationToken);

        // 3. Guardar el resultado en Redis durante 30 segundos.
        try
        {
            var json = JsonSerializer.Serialize(ventas);

            await cache.SetStringAsync(
                clave,
                json,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = DuracionCache
                },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Si Redis falla, el historial sigue funcionando con MySQL.
            logger.LogWarning(
                ex,
                "No se pudo guardar el historial de ventas en Redis.");
        }

        return ventas;
    }

    public async Task InvalidarHistorialAsync(
        CancellationToken cancellationToken = default)
    {
        // Eliminar las dos variantes del historial.
        var claves = new[]
        {
            "ventas:historial:incluir-anuladas:False",
            "ventas:historial:incluir-anuladas:True"
        };

        foreach (var clave in claves)
        {
            try
            {
                await cache.RemoveAsync(clave, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // La operación de venta ya puede estar confirmada en MySQL.
                logger.LogWarning(
                    ex,
                    "No se pudo invalidar la clave {Clave} en Redis.",
                    clave);
            }
        }
    }

    public async Task<VentaDetalleDto?> ObtenerDetalleAsync(
        int idVenta,
        CancellationToken cancellationToken = default)
    {
        if (idVenta <= 0)
        {
            return null;
        }

        var venta = await consultaRepository.ObtenerDetalleAsync(
            idVenta,
            cancellationToken);

        if (venta is null)
        {
            return null;
        }

        return new VentaDetalleDto(
            venta.Id,
            venta.NroComprobante,
            venta.FechaHora,
            venta.NitCiCliente,
            venta.RazonSocialCliente,
            venta.UsuarioCajero,
            venta.Estado,
            venta.Total,
            venta.MotivoAnulacion,
            venta.FechaAnulacion,
            venta.UsuarioAnulacion,
            AgruparPorProducto(venta.Filas));
    }

    private static IReadOnlyList<LineaDetalleVentaDto> AgruparPorProducto(
        IReadOnlyList<FilaDetalleVenta> filas) =>
        filas
            .GroupBy(fila => (fila.IdProducto, fila.PrecioUnitario))
            .Select(grupo => new LineaDetalleVentaDto(
                grupo.Sum(fila => fila.Cantidad),
                grupo.First().Descripcion,
                grupo.Key.PrecioUnitario,
                grupo.Sum(fila => fila.Subtotal),
                grupo
                    .GroupBy(fila => fila.CodigoLote)
                    .Select(lote => new LoteConsumidoDto(
                        lote.Key,
                        lote.Sum(fila => fila.Cantidad)))
                    .ToList()))
            .ToList();
}
