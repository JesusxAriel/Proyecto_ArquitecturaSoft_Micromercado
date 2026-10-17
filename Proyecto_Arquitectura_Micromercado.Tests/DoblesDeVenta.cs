using Proyecto_Arquitectura_Micromercado.Application.Common;
using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Application.Sales;
using Proyecto_Arquitectura_Micromercado.Domain.Sales;

namespace Proyecto_Arquitectura_Micromercado.Tests;

internal sealed class UnidadDeTrabajoEspia : IUnidadDeTrabajo
{
    public int Iniciadas { get; private set; }
    public int Confirmadas { get; private set; }
    public int Revertidas { get; private set; }

    public Task IniciarAsync(CancellationToken cancellationToken = default)
    {
        Iniciadas++;
        return Task.CompletedTask;
    }

    public Task ConfirmarAsync(CancellationToken cancellationToken = default)
    {
        Confirmadas++;
        return Task.CompletedTask;
    }

    public Task RevertirAsync(CancellationToken cancellationToken = default)
    {
        Revertidas++;
        return Task.CompletedTask;
    }
}

internal sealed class PrecioVigenteDoble(IDictionary<int, decimal> precios) : IConPrecioVigente
{
    public Task<decimal?> ObtenerPrecioVigenteAsync(int idProducto, CancellationToken cancellationToken = default) =>
        Task.FromResult<decimal?>(precios.TryGetValue(idProducto, out var precio) ? precio : null);

    public Task<DescripcionDeProducto?> ObtenerDescripcionAsync(int idProducto, CancellationToken cancellationToken = default) =>
        Task.FromResult<DescripcionDeProducto?>(idProducto switch
        {
            1 => new DescripcionDeProducto("Leche Entera PIL", "Bolsa 1L"),
            2 => new DescripcionDeProducto("Yogurt Frutado PIL", "Frasco 1kg"),
            _ => null
        });
}

internal sealed class CabeceraRegistrada
{
    public int Id { get; init; }
    public int IdCliente { get; init; }
    public int IdUsuario { get; init; }
    public string Estado { get; set; } = EstadoVenta.Completada;
    public string? Motivo { get; set; }
    public int? IdUsuarioAnulacion { get; set; }
    public string? NroComprobante { get; set; }
    public decimal MontoTotal { get; set; }
    public decimal EfectivoPagado { get; set; }
    public List<DetalleVenta> Detalles { get; } = [];
}

internal sealed class VentaRepositoryEnMemoria : IVentaRepository
{
    private int ultimoId;

    public List<CabeceraRegistrada> Cabeceras { get; } = [];

    public Queue<Exception> FallosPendientes { get; } = new();

    public Task<int> InsertarCabeceraAsync(int idCliente, int idUsuario, CancellationToken cancellationToken = default)
    {
        if (FallosPendientes.TryDequeue(out var fallo)) throw fallo;

        var cabecera = new CabeceraRegistrada { Id = ++ultimoId, IdCliente = idCliente, IdUsuario = idUsuario };
        Cabeceras.Add(cabecera);
        return Task.FromResult(cabecera.Id);
    }

    public Task InsertarDetalleAsync(int idVenta, int idProducto, int idLote, int cantidad, decimal precioUnitario, CancellationToken cancellationToken = default)
    {
        Cabeceras.Single(c => c.Id == idVenta).Detalles.Add(new DetalleVenta
        {
            IdVenta = idVenta,
            IdProducto = idProducto,
            IdLote = idLote,
            Cantidad = cantidad,
            PrecioUnitario = precioUnitario
        });
        return Task.CompletedTask;
    }

    public Task CerrarVentaAsync(int idVenta, string nroComprobante, decimal montoTotal, decimal efectivoPagado, CancellationToken cancellationToken = default)
    {
        var cabecera = Cabeceras.Single(c => c.Id == idVenta);
        cabecera.NroComprobante = nroComprobante;
        cabecera.MontoTotal = montoTotal;
        cabecera.EfectivoPagado = efectivoPagado;
        return Task.CompletedTask;
    }

    public Task<Venta?> ObtenerParaAnularAsync(int idVenta, CancellationToken cancellationToken = default)
    {
        var cabecera = Cabeceras.SingleOrDefault(c => c.Id == idVenta);
        if (cabecera is null) return Task.FromResult<Venta?>(null);

        var venta = new Venta
        {
            Id = cabecera.Id,
            Estado = cabecera.Estado,
            IdCliente = cabecera.IdCliente,
            IdUsuario = cabecera.IdUsuario,
            Detalles = [.. cabecera.Detalles]
        };
        return Task.FromResult<Venta?>(venta);
    }

    public Task MarcarAnuladaAsync(int idVenta, string motivo, int idUsuarioAnulacion, CancellationToken cancellationToken = default)
    {
        var cabecera = Cabeceras.Single(c => c.Id == idVenta);
        cabecera.Estado = EstadoVenta.Anulada;
        cabecera.Motivo = motivo;
        cabecera.IdUsuarioAnulacion = idUsuarioAnulacion;
        return Task.CompletedTask;
    }

    public Task<Venta?> GetByIdAsync(int idVenta, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<IReadOnlyList<Venta>> GetAllAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<decimal> GetVentasDelDiaAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}

internal sealed class InterbloqueoSimulado : Exception;

internal sealed class DetectorDeInterbloqueoDoble : IDetectorDeInterbloqueo
{
    private readonly List<Type> reconocidos = [];

    public void Reconocer<TExcepcion>() where TExcepcion : Exception => reconocidos.Add(typeof(TExcepcion));

    public bool EsInterbloqueo(Exception excepcion) => reconocidos.Contains(excepcion.GetType());
}
