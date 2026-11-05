using PuntoMarket.Ventas.Application.Common;
using PuntoMarket.Ventas.Application.Facades;
using PuntoMarket.Ventas.Application.Sales;
using PuntoMarket.Ventas.Domain.Sales;
using PuntoMarket.Ventas.Domain.Stock;

namespace PuntoMarket.Ventas.Tests;

public class RegresionCasosDeUsoTests
{
    // Espía de protocolo exclusivamente de pruebas. No implementa atomicidad ni una BD.
    private sealed class PuertosObservados : IUnidadDeTrabajo, IVentaRepository
    {
        public VentaRepositoryEnMemoria Ventas { get; } = new();
        public List<string> Operaciones { get; } = [];
        public string? FallarEn { get; set; }
        public Exception Fallo { get; } = new InvalidOperationException("Fallo inyectado en puerto");
        public CancellationToken TokenRollback { get; private set; }
        public Action? AntesDeFallar { get; set; }

        private void Registrar(string operacion)
        {
            Operaciones.Add(operacion);
            if (FallarEn == operacion)
            {
                AntesDeFallar?.Invoke();
                throw Fallo;
            }
        }

        public Task IniciarAsync(CancellationToken cancellationToken = default)
        {
            Registrar("iniciar");
            return Task.CompletedTask;
        }

        public Task ConfirmarAsync(CancellationToken cancellationToken = default)
        {
            Registrar("confirmar");
            return Task.CompletedTask;
        }

        public Task RevertirAsync(CancellationToken cancellationToken = default)
        {
            TokenRollback = cancellationToken;
            Registrar("revertir");
            return Task.CompletedTask;
        }

        public Task<int> InsertarCabeceraAsync(int idCliente, int idUsuario, CancellationToken cancellationToken = default)
        {
            Registrar("cabecera");
            return Ventas.InsertarCabeceraAsync(idCliente, idUsuario, cancellationToken);
        }

        public Task InsertarDetalleAsync(int idVenta, int idProducto, int idLote, int cantidad, decimal precioUnitario, CancellationToken cancellationToken = default)
        {
            Registrar("detalle");
            return Ventas.InsertarDetalleAsync(idVenta, idProducto, idLote, cantidad, precioUnitario, cancellationToken);
        }

        public Task CerrarVentaAsync(int idVenta, string nroComprobante, decimal montoTotal, CancellationToken cancellationToken = default)
        {
            Registrar("cerrar");
            return Ventas.CerrarVentaAsync(idVenta, nroComprobante, montoTotal, cancellationToken);
        }

        public Task<Venta?> ObtenerParaAnularAsync(int idVenta, CancellationToken cancellationToken = default)
        {
            Registrar("leer-anulacion");
            return Ventas.ObtenerParaAnularAsync(idVenta, cancellationToken);
        }

        public Task MarcarAnuladaAsync(int idVenta, string motivo, int idUsuarioAnulacion, CancellationToken cancellationToken = default)
        {
            Registrar("marcar-anulada");
            return Ventas.MarcarAnuladaAsync(idVenta, motivo, idUsuarioAnulacion, cancellationToken);
        }

        public Task<decimal> GetVentasDelDiaAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<Venta?> GetByIdAsync(int idVenta, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<Venta>> GetAllAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class Escenario
    {
        public PuertosObservados Puertos { get; } = new();
        public InMemoryLoteRepository Lotes { get; } = new();
        public FachadaVenta Registro { get; }
        public FachadaAnulacion Anulacion { get; }

        public Escenario()
        {
            var reloj = new RelojFijo(new DateTimeOffset(2026, 6, 15, 10, 0, 0, TimeSpan.Zero));
            var stock = new FachadaActualizacionStockProducto(Lotes, reloj, Puertos, new UsuarioActualEnMemoria(id: 12));
            Registro = new FachadaVenta(Puertos, Puertos,
                new PrecioVigenteDoble(new Dictionary<int, decimal> { [1] = 10m }), stock,
                new UsuarioActualEnMemoria(id: 7), reloj, new DetectorDeInterbloqueoDoble(), new ClientesDoble(3));
            Anulacion = new FachadaAnulacion(Puertos, Puertos, stock, new UsuarioActualEnMemoria(id: 9));
            Lotes.Agregar(1, new LoteDisponible(10, 3, new DateOnly(2026, 7, 1)), cantidadInicial: 3);
            Lotes.Agregar(1, new LoteDisponible(11, 10, new DateOnly(2026, 8, 1)), cantidadInicial: 10);
        }

        public Task<ResultadoVentaDto> Registrar() =>
            Registro.RegistrarVentaAsync(new NuevaVentaDto(3, [new LineaVentaDto(1, 5, 10m)]));
    }

    [Fact]
    public async Task Registro_y_anulacion_respetan_protocolo_FEFO_y_usuarios_de_auditoria()
    {
        var e = new Escenario();

        var resultado = await e.Registrar();

        Assert.Equal(["iniciar", "cabecera", "detalle", "detalle", "cerrar", "confirmar"], e.Puertos.Operaciones);
        var cabecera = Assert.Single(e.Puertos.Ventas.Cabeceras);
        Assert.Equal(7, cabecera.IdUsuario);
        Assert.Equal(50m, resultado.Total);
        Assert.Equal([(10, 3), (11, 2)], cabecera.Detalles.Select(d => (d.IdLote, d.Cantidad)));

        e.Puertos.Operaciones.Clear();
        await e.Anulacion.AnularVentaAsync(resultado.IdVenta, "  Corrección  ");

        Assert.Equal(["iniciar", "leer-anulacion", "marcar-anulada", "confirmar"], e.Puertos.Operaciones);
        Assert.Equal(9, cabecera.IdUsuarioAnulacion);
        Assert.Equal(EstadoVenta.Anulada, cabecera.Estado);
        Assert.Equal("Corrección", cabecera.Motivo);
        var lotes = await e.Lotes.ObtenerVigentesParaDescuentoAsync(1);
        Assert.Equal([(10, 3), (11, 10)], lotes.Select(l => (l.IdLote, l.CantidadDisponible)));
    }

    [Theory]
    [InlineData("detalle")]
    [InlineData("cerrar")]
    [InlineData("confirmar")]
    public async Task Un_fallo_despues_de_consumir_stock_pide_rollback_y_propaga_el_error(string operacion)
    {
        var e = new Escenario();
        e.Puertos.FallarEn = operacion;

        var error = await Assert.ThrowsAsync<InvalidOperationException>(e.Registrar);

        Assert.Same(e.Puertos.Fallo, error);
        Assert.Equal("revertir", e.Puertos.Operaciones.Last());
        Assert.Equal(1, e.Puertos.Operaciones.Count(o => o == "iniciar"));
        Assert.Equal(1, e.Puertos.Operaciones.Count(o => o == "revertir"));
        if (operacion != "confirmar") Assert.DoesNotContain("confirmar", e.Puertos.Operaciones);
        Assert.Equal(CancellationToken.None, e.Puertos.TokenRollback);
    }

    [Fact]
    public async Task Un_fallo_al_marcar_anulada_pide_rollback_sin_confirmar()
    {
        var e = new Escenario();
        var venta = await e.Registrar();
        e.Puertos.Operaciones.Clear();
        e.Puertos.FallarEn = "marcar-anulada";

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => e.Anulacion.AnularVentaAsync(venta.IdVenta, "Error"));

        Assert.Same(e.Puertos.Fallo, error);
        Assert.Equal(["iniciar", "leer-anulacion", "marcar-anulada", "revertir"], e.Puertos.Operaciones);
    }

    [Fact]
    public async Task El_rollback_se_solicita_con_token_no_cancelado_aunque_se_cancele_la_peticion()
    {
        using var cancelacion = new CancellationTokenSource();
        var e = new Escenario();
        e.Puertos.FallarEn = "detalle";
        e.Puertos.AntesDeFallar = cancelacion.Cancel;

        await Assert.ThrowsAsync<InvalidOperationException>(() => e.Registro.RegistrarVentaAsync(
            new NuevaVentaDto(3, [new LineaVentaDto(1, 1)]), cancelacion.Token));

        Assert.True(cancelacion.IsCancellationRequested);
        Assert.False(e.Puertos.TokenRollback.CanBeCanceled);
        Assert.Equal("revertir", e.Puertos.Operaciones.Last());
    }

    [Fact]
    public async Task Una_segunda_anulacion_no_restaura_dos_veces()
    {
        var e = new Escenario();
        var venta = await e.Registrar();
        await e.Anulacion.AnularVentaAsync(venta.IdVenta, "Error");
        e.Puertos.Operaciones.Clear();

        await Assert.ThrowsAsync<InvalidOperationException>(() => e.Anulacion.AnularVentaAsync(venta.IdVenta, "Otra vez"));

        Assert.Equal(["iniciar", "leer-anulacion", "revertir"], e.Puertos.Operaciones);
        var lotes = await e.Lotes.ObtenerVigentesParaDescuentoAsync(1);
        Assert.Equal(13, lotes.Sum(l => l.CantidadDisponible));
    }
}
