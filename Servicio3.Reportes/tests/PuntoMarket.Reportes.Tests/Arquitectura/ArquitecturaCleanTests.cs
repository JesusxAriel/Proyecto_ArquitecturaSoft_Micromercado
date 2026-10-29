using System.Reflection;
using PuntoMarket.Reportes.Application.Common;
using PuntoMarket.Reportes.Domain.ValueObjects;
using PuntoMarket.Reportes.Infrastructure.Persistence;

namespace PuntoMarket.Reportes.Tests.Arquitectura;

public class ArquitecturaCleanTests
{
    private const string Prefijo = "PuntoMarket.Reportes.";
    private const string EspacioDeCasosDeUso = "PuntoMarket.Reportes.Application.UseCases";

    private static readonly Assembly Dominio = typeof(RangoDeFechas).Assembly;
    private static readonly Assembly Aplicacion = typeof(ArchivoGenerado).Assembly;
    private static readonly Assembly Infraestructura = typeof(ConexionMySql).Assembly;
    private static readonly Assembly Api = typeof(Program).Assembly;

    [Fact]
    public void Dominio_no_depende_de_otras_capas_ni_de_tecnologia()
    {
        var referencias = NombresReferenciados(Dominio);

        Assert.DoesNotContain(referencias, EsDeServicioDistintoDeDominio);
        Assert.DoesNotContain(referencias, EsMySql);
        Assert.DoesNotContain(referencias, EsAspNetCore);
    }

    [Fact]
    public void Aplicacion_solo_depende_del_dominio_entre_los_proyectos_del_servicio()
    {
        var referencias = NombresReferenciados(Aplicacion);

        var delServicio = referencias.Where(n => n.StartsWith(Prefijo)).ToList();
        Assert.All(delServicio, n => Assert.Equal(Dominio.GetName().Name, n));
        Assert.DoesNotContain(referencias, EsMySql);
        Assert.DoesNotContain(referencias, EsAspNetCore);
    }

    [Fact]
    public void Infraestructura_no_depende_de_la_api()
    {
        var referencias = NombresReferenciados(Infraestructura);

        Assert.DoesNotContain(Api.GetName().Name!, referencias);
    }

    [Fact]
    public void Los_casos_de_uso_viven_solo_en_el_espacio_de_nombres_de_casos_de_uso()
    {
        var fuera = new[] { Dominio, Aplicacion, Infraestructura, Api }
            .SelectMany(a => a.GetTypes())
            .Where(t => t.Name.EndsWith("CasoDeUso"))
            .Where(t => !EstaEnCasosDeUso(t));

        Assert.Empty(fuera);
    }

    private static List<string> NombresReferenciados(Assembly ensamblado) =>
        ensamblado.GetReferencedAssemblies()
            .Select(r => r.Name!)
            .ToList();

    private static bool EsDeServicioDistintoDeDominio(string nombre) =>
        nombre.StartsWith(Prefijo) && nombre != Dominio.GetName().Name;

    private static bool EsMySql(string nombre) => nombre.StartsWith("MySql.Data");

    private static bool EsAspNetCore(string nombre) => nombre.StartsWith("Microsoft.AspNetCore");

    private static bool EstaEnCasosDeUso(Type tipo) =>
        tipo.Namespace is not null && tipo.Namespace.StartsWith(EspacioDeCasosDeUso);
}
