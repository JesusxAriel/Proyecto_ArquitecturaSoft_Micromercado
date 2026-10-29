using PuntoMarket.Reportes.Application.Common;
using PuntoMarket.Reportes.Tests.Dobles;

namespace PuntoMarket.Reportes.Tests.Application;

public class CreadorDeExportadorTests
{
    private readonly CreadorDePrueba _creador = new(
        new ExportadorDePrueba(FormatoDeExportacion.Json),
        new ExportadorDePrueba(FormatoDeExportacion.Pdf));

    [Theory]
    [InlineData(FormatoDeExportacion.Json)]
    [InlineData(FormatoDeExportacion.Pdf)]
    public void Devuelve_el_archivo_del_exportador_que_corresponde_al_formato(FormatoDeExportacion formato)
    {
        var archivo = _creador.Exportar("reporte", formato);

        Assert.Equal($"prueba/{formato}", archivo.TipoDeContenido);
        Assert.Equal($"reporte.{formato}", archivo.NombreDeArchivo);
    }

    [Fact]
    public void Un_formato_no_soportado_lanza_excepcion()
    {
        Assert.Throws<FormatoDeExportacionNoSoportadoException>(
            () => _creador.Exportar("reporte", FormatoDeExportacion.Xlsx));
    }
}
