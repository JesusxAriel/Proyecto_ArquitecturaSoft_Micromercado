using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Proyecto_Arquitectura_Micromercado.Application.Stock;
using Proyecto_Arquitectura_Micromercado.Domain.Stock;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Background;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class BajaAutomaticaDeLotesVencidosTests
{
    [Fact]
    public async Task Al_arrancar_da_de_baja_los_vencidos_una_vez_sin_esperar_el_intervalo()
    {
        var servicio = new BajaDoble();
        using var servicioEnSegundoPlano = Crear(servicio, TimeSpan.FromHours(6));

        await servicioEnSegundoPlano.StartAsync(CancellationToken.None);
        await Esperar(() => servicio.Llamadas >= 1);
        await servicioEnSegundoPlano.StopAsync(CancellationToken.None);

        Assert.Equal(1, servicio.Llamadas);
        Assert.Equal(0, servicio.LlamadasConUsuario);
    }

    [Fact]
    public async Task Despues_del_primer_ciclo_repite_la_baja_en_cada_intervalo()
    {
        var servicio = new BajaDoble();
        using var servicioEnSegundoPlano = Crear(servicio, TimeSpan.FromMilliseconds(40));

        await servicioEnSegundoPlano.StartAsync(CancellationToken.None);
        await Esperar(() => servicio.Llamadas >= 3);
        await servicioEnSegundoPlano.StopAsync(CancellationToken.None);

        Assert.True(servicio.Llamadas >= 3);
    }

    [Fact]
    public async Task Un_fallo_en_un_ciclo_se_registra_y_no_detiene_los_siguientes()
    {
        var servicio = new BajaDoble { FallarPrimeraVez = true };
        var registro = new RegistroDeBaja();
        using var servicioEnSegundoPlano = Crear(servicio, TimeSpan.FromMilliseconds(40), registro);

        await servicioEnSegundoPlano.StartAsync(CancellationToken.None);
        await Esperar(() => servicio.Llamadas >= 2);
        await servicioEnSegundoPlano.StopAsync(CancellationToken.None);

        Assert.True(servicio.Llamadas >= 2);
        Assert.Contains(LogLevel.Error, registro.Niveles);
    }

    [Fact]
    public async Task Cada_ciclo_usa_su_propio_alcance_de_servicios()
    {
        var servicio = new BajaDoble();
        var proveedor = new ServiceCollection().AddScoped<IBajaDeLotesService>(_ => servicio.NuevaInstancia()).BuildServiceProvider();
        using var servicioEnSegundoPlano = new BajaAutomaticaDeLotesVencidos(
            proveedor.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<BajaAutomaticaDeLotesVencidos>.Instance,
            TimeSpan.FromMilliseconds(40));

        await servicioEnSegundoPlano.StartAsync(CancellationToken.None);
        await Esperar(() => servicio.Instancias >= 2);
        await servicioEnSegundoPlano.StopAsync(CancellationToken.None);

        Assert.True(servicio.Instancias >= 2);
    }

    [Fact]
    public void El_intervalo_por_defecto_es_de_seis_horas()
    {
        Assert.Equal(TimeSpan.FromHours(6), BajaAutomaticaDeLotesVencidos.IntervaloPorDefecto);
    }

    private static BajaAutomaticaDeLotesVencidos Crear(BajaDoble servicio, TimeSpan intervalo, ILogger<BajaAutomaticaDeLotesVencidos>? registro = null)
    {
        var proveedor = new ServiceCollection().AddScoped<IBajaDeLotesService>(_ => servicio).BuildServiceProvider();
        return new BajaAutomaticaDeLotesVencidos(
            proveedor.GetRequiredService<IServiceScopeFactory>(),
            registro ?? NullLogger<BajaAutomaticaDeLotesVencidos>.Instance,
            intervalo);
    }

    private static async Task Esperar(Func<bool> condicion)
    {
        var limite = DateTime.UtcNow.AddSeconds(5);
        while (!condicion() && DateTime.UtcNow < limite)
        {
            await Task.Delay(10);
        }
    }

    private sealed class BajaDoble : IBajaDeLotesService
    {
        private int llamadas;
        private int llamadasConUsuario;
        private int instancias;

        public int Llamadas => Volatile.Read(ref llamadas);
        public int LlamadasConUsuario => Volatile.Read(ref llamadasConUsuario);
        public int Instancias => Volatile.Read(ref instancias);
        public bool FallarPrimeraVez { get; init; }

        public BajaDoble NuevaInstancia()
        {
            Interlocked.Increment(ref instancias);
            return this;
        }

        public Task<ResultadoBajaDeLotes> DarDeBajaVencidosAsync(CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref llamadasConUsuario);
            return Task.FromResult(new ResultadoBajaDeLotes(0, 0));
        }

        public Task<ResultadoBajaDeLotes> DarDeBajaVencidosDelSistemaAsync(CancellationToken cancellationToken = default)
        {
            var numero = Interlocked.Increment(ref llamadas);
            if (FallarPrimeraVez && numero == 1)
            {
                throw new InvalidOperationException("Fallo simulado.");
            }

            return Task.FromResult(new ResultadoBajaDeLotes(1, 4));
        }
    }

    private sealed class RegistroDeBaja : ILogger<BajaAutomaticaDeLotesVencidos>
    {
        public List<LogLevel> Niveles { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            lock (Niveles)
            {
                Niveles.Add(logLevel);
            }
        }
    }
}
