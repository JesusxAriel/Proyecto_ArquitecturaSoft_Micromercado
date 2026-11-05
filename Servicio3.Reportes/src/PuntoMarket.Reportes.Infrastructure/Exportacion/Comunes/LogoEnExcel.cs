using ClosedXML.Excel;

namespace PuntoMarket.Reportes.Infrastructure.Exportacion.Comunes;

public static class LogoEnExcel
{
    private const int Ancho = 170;
    private const int Alto = 60;

    public static void Agregar(IXLWorksheet hoja, int columna)
    {
        using var imagen = new MemoryStream(LogoDeReportes.Obtener());
        hoja.AddPicture(imagen)
            .MoveTo(hoja.Cell(1, columna))
            .WithSize(Ancho, Alto);
    }
}
