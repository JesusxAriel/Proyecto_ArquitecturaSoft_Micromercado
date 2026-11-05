using PuntoMarket.Reportes.Domain.Tendencias;
using PuntoMarket.Reportes.Domain.ValueObjects;

namespace PuntoMarket.Reportes.Tests.Dominio;

public class TendenciaDeVentasTests
{
    private static PuntoDeTendencia Punto(int dia, decimal monto, int mes = 5) =>
        new(new DateOnly(2026, mes, dia), monto);

    [Fact]
    public void Sin_datos_la_tendencia_esta_vacia_y_no_se_grafica()
    {
        var tendencia = TendenciaDeVentas.Desde([]);

        Assert.Empty(tendencia.Puntos);
        Assert.False(tendencia.PuedeGraficarse);
    }

    [Fact]
    public void Un_solo_dia_no_alcanza_para_graficar()
    {
        var tendencia = TendenciaDeVentas.Desde([Punto(3, 50m)]);

        Assert.Single(tendencia.Puntos);
        Assert.False(tendencia.PuedeGraficarse);
    }

    [Fact]
    public void En_rangos_cortos_agrupa_por_dia_y_completa_con_cero_los_dias_sin_ventas()
    {
        var tendencia = TendenciaDeVentas.Desde([Punto(5, 30m), Punto(1, 10m), Punto(3, 20m)]);

        Assert.Equal(AgrupacionDeTendencia.Diaria, tendencia.Agrupacion);
        Assert.Equal(5, tendencia.Puntos.Count);
        Assert.Equal([10m, 0m, 20m, 0m, 30m], tendencia.Puntos.Select(p => p.Monto));
        Assert.True(tendencia.PuedeGraficarse);
    }

    [Fact]
    public void Con_exactamente_45_dias_sigue_siendo_diaria()
    {
        var tendencia = TendenciaDeVentas.Desde(
            [new PuntoDeTendencia(new DateOnly(2026, 1, 1), 1m), new PuntoDeTendencia(new DateOnly(2026, 2, 14), 1m)]);

        Assert.Equal(AgrupacionDeTendencia.Diaria, tendencia.Agrupacion);
        Assert.Equal(45, tendencia.Puntos.Count);
    }

    [Fact]
    public void Con_mas_de_45_dias_agrupa_por_semana_iniciando_en_lunes()
    {
        var tendencia = TendenciaDeVentas.Desde(
        [
            new PuntoDeTendencia(new DateOnly(2026, 1, 1), 10m),
            new PuntoDeTendencia(new DateOnly(2026, 1, 2), 5m),
            new PuntoDeTendencia(new DateOnly(2026, 1, 14), 7m),
            new PuntoDeTendencia(new DateOnly(2026, 3, 1), 3m)
        ]);

        Assert.Equal(AgrupacionDeTendencia.Semanal, tendencia.Agrupacion);
        Assert.All(tendencia.Puntos, p => Assert.Equal(DayOfWeek.Monday, p.Fecha.DayOfWeek));
        Assert.Equal(new DateOnly(2025, 12, 29), tendencia.Puntos[0].Fecha);
        Assert.Equal(15m, tendencia.Puntos[0].Monto);
        Assert.Equal(0m, tendencia.Puntos[1].Monto);
        Assert.Equal(7m, tendencia.Puntos[2].Monto);
        Assert.Equal(3m, tendencia.Puntos[^1].Monto);
        Assert.Equal(25m, tendencia.Puntos.Sum(p => p.Monto));
    }
}
