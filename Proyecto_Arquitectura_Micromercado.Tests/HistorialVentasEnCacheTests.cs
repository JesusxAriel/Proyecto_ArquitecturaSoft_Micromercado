using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Proyecto_Arquitectura_Micromercado.Application.Sales;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class HistorialVentasEnCacheTests
{
    private const string ClaveSinAnuladas = "ventas:historial:incluir-anuladas:False";
    private const string ClaveConAnuladas = "ventas:historial:incluir-anuladas:True";

    private sealed class CacheEspia : IDistributedCache
    {
        public Dictionary<string, byte[]> Entradas { get; } = [];
        public Dictionary<string, DistributedCacheEntryOptions> Opciones { get; } = [];
        public List<string> Eliminadas { get; } = [];
        public bool Falla { get; set; }

        public byte[]? Get(string key) => GetAsync(key).GetAwaiter().GetResult();

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        {
            ComprobarDisponible();
            return Task.FromResult(Entradas.GetValueOrDefault(key));
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) =>
            SetAsync(key, value, options).GetAwaiter().GetResult();

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            ComprobarDisponible();
            Entradas[key] = value;
            Opciones[key] = options;
            return Task.CompletedTask;
        }

        public void Refresh(string key)
        {
        }

        public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;

        public void Remove(string key) => RemoveAsync(key).GetAwaiter().GetResult();

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            ComprobarDisponible();
            Entradas.Remove(key);
            Eliminadas.Add(key);
            return Task.CompletedTask;
        }

        private void ComprobarDisponible()
        {
            if (Falla) throw new InvalidOperationException("Redis no disponible.");
        }
    }

    private sealed class RegistroDeCache : ILogger<VentaConsultaService>
    {
        public List<string> Mensajes { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Mensajes.Add(formatter(state, exception));
    }

    private sealed class ConsultaConConteo : IVentaConsultaRepository
    {
        public int Consultas { get; private set; }

        public Task<IReadOnlyList<VentaResumenDto>> ListarAsync(
            bool incluirAnuladas, int limite, CancellationToken cancellationToken = default)
        {
            Consultas++;
            IReadOnlyList<VentaResumenDto> ventas =
                [new(1, "V-20260615-0001", new DateTime(2026, 6, 15, 10, 0, 0), "Cliente Uno", "COMPLETADA", 25m)];
            return Task.FromResult(ventas);
        }

        public Task<VentaDetalleConFilasDto?> ObtenerDetalleAsync(
            int idVenta, CancellationToken cancellationToken = default) =>
            Task.FromResult<VentaDetalleConFilasDto?>(null);
    }

    private static VentaConsultaService Crear(
        ConsultaConConteo consulta, CacheEspia cache, RegistroDeCache registro, string replica = "app1") =>
        new(new VentaRepositoryEnMemoria(), consulta, cache, registro, new ReplicaFija(replica));

    [Fact]
    public async Task El_primer_listado_es_MISS_consulta_la_base_y_guarda_con_expiracion()
    {
        var consulta = new ConsultaConConteo();
        var cache = new CacheEspia();
        var servicio = Crear(consulta, cache, new RegistroDeCache());

        var ventas = await servicio.ListarAsync(false);

        Assert.Single(ventas);
        Assert.Equal(1, consulta.Consultas);
        Assert.Contains(ClaveSinAnuladas, cache.Entradas.Keys);
        Assert.Equal(TimeSpan.FromSeconds(30), cache.Opciones[ClaveSinAnuladas].AbsoluteExpirationRelativeToNow);
    }

    [Fact]
    public async Task El_segundo_listado_es_HIT_y_no_consulta_la_base()
    {
        var consulta = new ConsultaConConteo();
        var servicio = Crear(consulta, new CacheEspia(), new RegistroDeCache());

        await servicio.ListarAsync(false);
        var ventas = await servicio.ListarAsync(false);

        Assert.Equal(1, consulta.Consultas);
        Assert.Equal("V-20260615-0001", Assert.Single(ventas).NroComprobante);
    }

    [Fact]
    public async Task Una_replica_aprovecha_la_entrada_escrita_por_otra()
    {
        var consulta = new ConsultaConConteo();
        var cache = new CacheEspia();
        var primera = Crear(consulta, cache, new RegistroDeCache(), "app1");
        var registroSegunda = new RegistroDeCache();
        var segunda = Crear(consulta, cache, registroSegunda, "app2");

        await primera.ListarAsync(false);
        await segunda.ListarAsync(false);

        Assert.Equal(1, consulta.Consultas);
        var mensaje = Assert.Single(registroSegunda.Mensajes);
        Assert.Contains("HIT", mensaje);
        Assert.Contains("réplica=app2", mensaje);
    }

    [Fact]
    public async Task El_registro_indica_resultado_clave_origen_y_replica()
    {
        var registro = new RegistroDeCache();
        var servicio = Crear(new ConsultaConConteo(), new CacheEspia(), registro, "app3");

        await servicio.ListarAsync(true);
        await servicio.ListarAsync(true);

        Assert.Equal(
            [
                $"Caché del historial: MISS clave={ClaveConAnuladas} origen=MySQL réplica=app3",
                $"Caché del historial: HIT clave={ClaveConAnuladas} origen=Redis réplica=app3"
            ],
            registro.Mensajes);
    }

    [Fact]
    public async Task Cada_variante_del_historial_usa_su_propia_clave()
    {
        var consulta = new ConsultaConConteo();
        var cache = new CacheEspia();
        var servicio = Crear(consulta, cache, new RegistroDeCache());

        await servicio.ListarAsync(false);
        await servicio.ListarAsync(true);

        Assert.Equal(2, consulta.Consultas);
        Assert.Equal([ClaveSinAnuladas, ClaveConAnuladas], cache.Entradas.Keys.Order().ToArray());
    }

    [Fact]
    public async Task Invalidar_borra_las_dos_claves_y_el_siguiente_listado_vuelve_a_la_base()
    {
        var consulta = new ConsultaConConteo();
        var cache = new CacheEspia();
        var servicio = Crear(consulta, cache, new RegistroDeCache());
        await servicio.ListarAsync(false);
        await servicio.ListarAsync(true);

        await servicio.InvalidarHistorialAsync();
        await servicio.ListarAsync(false);

        Assert.Equal([ClaveSinAnuladas, ClaveConAnuladas], cache.Eliminadas);
        Assert.Equal(3, consulta.Consultas);
    }

    [Fact]
    public async Task Si_Redis_falla_el_historial_responde_desde_la_base()
    {
        var consulta = new ConsultaConConteo();
        var cache = new CacheEspia { Falla = true };
        var servicio = Crear(consulta, cache, new RegistroDeCache());

        var ventas = await servicio.ListarAsync(false);
        await servicio.InvalidarHistorialAsync();

        Assert.Single(ventas);
        Assert.Equal(1, consulta.Consultas);
    }
}
