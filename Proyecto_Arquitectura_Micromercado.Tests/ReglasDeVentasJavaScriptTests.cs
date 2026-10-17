using System.Diagnostics;

namespace Proyecto_Arquitectura_Micromercado.Tests;

[Trait("Categoria", "JavaScript")]
public class ReglasDeVentasJavaScriptTests
{
    [Fact]
    public void Las_reglas_puras_de_ventas_pasan_sus_pruebas_de_node()
    {
        var archivo = Path.Combine(RaizDelRepositorio(), "Proyecto_Arquitectura_Micromercado.Tests", "js", "ventas-reglas.test.cjs");
        Assert.True(File.Exists(archivo), $"No se encontro {archivo}.");

        var informacion = new ProcessStartInfo("node", $"--test \"{archivo}\"")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        Process? proceso;
        try
        {
            proceso = Process.Start(informacion);
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return;
        }

        using (proceso!)
        {
            var salida = proceso.StandardOutput.ReadToEnd() + proceso.StandardError.ReadToEnd();
            proceso.WaitForExit();

            Assert.True(proceso.ExitCode == 0, salida);
        }
    }

    private static string RaizDelRepositorio()
    {
        var directorio = new DirectoryInfo(AppContext.BaseDirectory);

        while (directorio is not null && !File.Exists(Path.Combine(directorio.FullName, "Proyecto_Arquitectura_Micromercado.slnx")))
        {
            directorio = directorio.Parent;
        }

        return directorio?.FullName ?? throw new DirectoryNotFoundException("No se encontro la raiz del repositorio.");
    }
}
