using System.Text.RegularExpressions;
using Proyecto_Arquitectura_Micromercado.Application.Security;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Security;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class BCryptPasswordHasherTests
{
    private readonly IPasswordHasher hasher = new BCryptPasswordHasher();

    [Fact]
    public void El_mismo_texto_produce_un_hash_distinto_cada_vez()
    {
        var primero = hasher.Hash("clave-segura-1");
        var segundo = hasher.Hash("clave-segura-1");

        Assert.NotEqual(primero, segundo);
    }

    [Fact]
    public void El_hash_no_contiene_la_contrasena_y_usa_el_formato_BCrypt_con_factor_11()
    {
        var hash = hasher.Hash("clave-segura-1");

        Assert.DoesNotContain("clave-segura-1", hash);
        Assert.Equal(60, hash.Length);
        Assert.StartsWith("$2", hash);
        Assert.Contains("$11$", hash);
    }

    [Fact]
    public void Verify_acepta_la_contrasena_correcta()
    {
        var hash = hasher.Hash("clave-segura-1");

        Assert.True(hasher.Verify("clave-segura-1", hash));
    }

    [Theory]
    [InlineData("clave-segura-2")]
    [InlineData("CLAVE-SEGURA-1")]
    [InlineData("clave-segura-1 ")]
    [InlineData("")]
    public void Verify_rechaza_una_contrasena_incorrecta(string intento)
    {
        var hash = hasher.Hash("clave-segura-1");

        Assert.False(hasher.Verify(intento, hash));
    }

    [Theory]
    [InlineData("")]
    [InlineData("no-es-un-hash")]
    [InlineData("$2a$11$corto")]
    public void Verify_devuelve_false_sin_lanzar_cuando_el_hash_es_invalido(string hashInvalido)
    {
        Assert.False(hasher.Verify("clave-segura-1", hashInvalido));
    }

    // Los hashes del script SQL se generaron aparte y se pegaron a mano. Esta prueba
    // garantiza que son verificables con el mismo adaptador que usa el login y que
    // coinciden con las credenciales documentadas en el README.
    [Theory]
    [InlineData("admin", "admin123")]
    [InlineData("cajero1", "cajero123")]
    public void Los_hashes_sembrados_en_el_script_SQL_corresponden_a_las_credenciales_de_prueba(
        string usuario,
        string contrasena)
    {
        var script = File.ReadAllText(RutaDelScript());
        var coincidencia = Regex.Match(
            script,
            $@"\(\d+,\s*'{usuario}',\s*'(?<hash>\$2[aby]\$11\$[^']+)'");

        Assert.True(coincidencia.Success, $"No se encontro el hash BCrypt de '{usuario}' en el script.");
        Assert.True(hasher.Verify(contrasena, coincidencia.Groups["hash"].Value));
    }

    private static string RutaDelScript()
    {
        var directorio = new DirectoryInfo(AppContext.BaseDirectory);

        while (directorio is not null &&
               !File.Exists(Path.Combine(directorio.FullName, "Proyecto_Arquitectura_Micromercado.slnx")))
        {
            directorio = directorio.Parent;
        }

        Assert.NotNull(directorio);

        return Path.Combine(directorio.FullName, "bdMicroMercadoArqui.sql");
    }
}
