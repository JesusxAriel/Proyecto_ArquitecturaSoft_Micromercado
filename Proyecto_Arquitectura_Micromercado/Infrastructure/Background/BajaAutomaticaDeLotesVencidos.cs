using Proyecto_Arquitectura_Micromercado.Application.Stock;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Background;

public sealed class BajaAutomaticaDeLotesVencidos(
    IServiceScopeFactory alcances,
    ILogger<BajaAutomaticaDeLotesVencidos> registro,
    TimeSpan? intervalo = null) : BackgroundService
{
    public static readonly TimeSpan IntervaloPorDefecto = TimeSpan.FromHours(6);

    protected override async Task ExecuteAsync(CancellationToken detener)
    {
        using var temporizador = new PeriodicTimer(intervalo ?? IntervaloPorDefecto);

        do
        {
            await EjecutarUnaVezAsync(detener);
        }
        while (await EsperarSiguienteAsync(temporizador, detener));
    }

    public async Task EjecutarUnaVezAsync(CancellationToken detener)
    {
        try
        {
            await using var alcance = alcances.CreateAsyncScope();
            var servicio = alcance.ServiceProvider.GetRequiredService<IBajaDeLotesService>();
            var resultado = await servicio.DarDeBajaVencidosDelSistemaAsync(detener);

            if (resultado.Lotes > 0)
            {
                registro.LogInformation(
                    "Baja automática de lotes vencidos: {Lotes} lotes y {Unidades} unidades.",
                    resultado.Lotes, resultado.Unidades);
            }
        }
        catch (OperationCanceledException) when (detener.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            registro.LogError(ex, "No se pudo ejecutar la baja automática de lotes vencidos.");
        }
    }

    private static async Task<bool> EsperarSiguienteAsync(PeriodicTimer temporizador, CancellationToken detener)
    {
        try
        {
            return await temporizador.WaitForNextTickAsync(detener);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
