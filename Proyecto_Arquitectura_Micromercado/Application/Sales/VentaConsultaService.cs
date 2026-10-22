
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Proyecto_Arquitectura_Micromercado.Application.Common;

namespace Proyecto_Arquitectura_Micromercado.Application.Sales;

public sealed class VentaConsultaService(
    IVentaRepository ventaRepository,
    IVentaConsultaRepository consultaRepository,
    IDistributedCache cache,
    ILogger<VentaConsultaService> logger,
    IIdentificadorReplica replica) : IVentaConsultaService
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
        var clave = ClaveHistorial(incluirAnuladas);

        var guardadas = await LeerDeCacheAsync(clave, cancellationToken);
        if (guardadas is not null)
        {
            RegistrarAcceso("HIT", clave, "Redis");
            return guardadas;
        }

        var ventas = await consultaRepository.ListarAsync(
            incluirAnuladas,
            LimiteHistorial,
            cancellationToken);
        RegistrarAcceso("MISS", clave, "MySQL");

        await GuardarEnCacheAsync(clave, ventas, cancellationToken);
        return ventas;
    }

    public async Task InvalidarHistorialAsync(
        CancellationToken cancellationToken = default)
    {
        foreach (var clave in new[] { ClaveHistorial(false), ClaveHistorial(true) })
        {
            await InvalidarClaveAsync(clave, cancellationToken);
        }
    }

    private static string ClaveHistorial(bool incluirAnuladas) =>
        $"ventas:historial:incluir-anuladas:{incluirAnuladas}";

    private async Task<IReadOnlyList<VentaResumenDto>?> LeerDeCacheAsync(
        string clave,
        CancellationToken cancellationToken)
    {
        try
        {
            var json = await cache.GetStringAsync(clave, cancellationToken);
            return string.IsNullOrWhiteSpace(json)
                ? null
                : JsonSerializer.Deserialize<List<VentaResumenDto>>(json);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(
                ex,
                "No se pudo leer el historial de ventas desde Redis.");
            return null;
        }
    }

    private async Task GuardarEnCacheAsync(
        string clave,
        IReadOnlyList<VentaResumenDto> ventas,
        CancellationToken cancellationToken)
    {
        try
        {
            await cache.SetStringAsync(
                clave,
                JsonSerializer.Serialize(ventas),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = DuracionCache
                },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(
                ex,
                "No se pudo guardar el historial de ventas en Redis.");
        }
    }

    private async Task InvalidarClaveAsync(
        string clave,
        CancellationToken cancellationToken)
    {
        try
        {
            await cache.RemoveAsync(clave, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(
                ex,
                "No se pudo invalidar la clave {Clave} en Redis.",
                clave);
        }
    }

    private void RegistrarAcceso(string resultado, string clave, string origen) =>
        logger.LogInformation(
            "Caché del historial: {Resultado} clave={Clave} origen={Origen} réplica={Replica}",
            resultado,
            clave,
            origen,
            replica.Valor);

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
