using System.Reflection;
using MySql.Data.MySqlClient;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class ReintentoDeLecturasTests
{
    private static readonly Func<TimeSpan, Task> SinEspera = _ => Task.CompletedTask;

    private static MySqlException CrearMySqlException(string mensaje, int numero, Exception interna) =>
        (MySqlException)Activator.CreateInstance(
            typeof(MySqlException),
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            [mensaje, numero, interna],
            null)!;

    private static MySqlException Transitoria() =>
        CrearMySqlException("Fatal error encountered during command execution", 0, new IOException("cerrada"));

    private static MySqlException Permanente() =>
        CrearMySqlException("Unknown column 'x' in 'field list'", 1054, new Exception("sql"));

    [Fact]
    public async Task Una_lectura_que_funciona_se_ejecuta_una_sola_vez()
    {
        var llamadas = 0;

        var resultado = await ReintentoDeLecturas.EjecutarAsync(() =>
        {
            llamadas++;
            return Task.FromResult(7);
        }, esperar: SinEspera);

        Assert.Equal(7, resultado);
        Assert.Equal(1, llamadas);
    }

    [Fact]
    public async Task Un_error_transitorio_se_reintenta_una_vez_y_devuelve_el_segundo_resultado()
    {
        var llamadas = 0;
        var esperas = new List<TimeSpan>();

        var resultado = await ReintentoDeLecturas.EjecutarAsync(() =>
        {
            llamadas++;
            return llamadas == 1 ? throw Transitoria() : Task.FromResult("ok");
        }, TimeSpan.FromMilliseconds(40), tiempo =>
        {
            esperas.Add(tiempo);
            return Task.CompletedTask;
        });

        Assert.Equal("ok", resultado);
        Assert.Equal(2, llamadas);
        Assert.Equal([TimeSpan.FromMilliseconds(40)], esperas);
    }

    [Fact]
    public async Task Si_el_reintento_tambien_falla_la_excepcion_se_propaga_sin_un_tercer_intento()
    {
        var llamadas = 0;

        await Assert.ThrowsAsync<MySqlException>(() => ReintentoDeLecturas.EjecutarAsync<int>(() =>
        {
            llamadas++;
            throw Transitoria();
        }, esperar: SinEspera));

        Assert.Equal(2, llamadas);
    }

    [Fact]
    public async Task Un_error_que_no_es_transitorio_no_se_reintenta()
    {
        var llamadas = 0;

        await Assert.ThrowsAsync<MySqlException>(() => ReintentoDeLecturas.EjecutarAsync<int>(() =>
        {
            llamadas++;
            throw Permanente();
        }, esperar: SinEspera));

        Assert.Equal(1, llamadas);
    }

    [Fact]
    public async Task Una_cancelacion_no_se_reintenta()
    {
        var llamadas = 0;

        await Assert.ThrowsAsync<TaskCanceledException>(() => ReintentoDeLecturas.EjecutarAsync<int>(() =>
        {
            llamadas++;
            throw new TaskCanceledException();
        }, esperar: SinEspera));

        Assert.Equal(1, llamadas);
    }

    [Theory]
    [InlineData(1213)]
    [InlineData(1205)]
    [InlineData(2006)]
    [InlineData(2013)]
    [InlineData(1042)]
    public void Los_codigos_de_conexion_perdida_y_bloqueo_se_consideran_transitorios(int numero)
    {
        Assert.True(ReintentoDeLecturas.EsTransitoria(CrearMySqlException("error", numero, new Exception("x"))));
    }

    [Fact]
    public void Un_error_de_sintaxis_no_es_transitorio()
    {
        Assert.False(ReintentoDeLecturas.EsTransitoria(Permanente()));
    }
}
