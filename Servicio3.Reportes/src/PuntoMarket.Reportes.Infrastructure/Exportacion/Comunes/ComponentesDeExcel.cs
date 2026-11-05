using ClosedXML.Excel;

namespace PuntoMarket.Reportes.Infrastructure.Exportacion.Comunes;

public static class ComponentesDeExcel
{
    private const double AlturaDeFilaEnPixeles = 20;
    private const int AnchoDeImagenEnPantalla = 640;

    public static int EscribirTitulo(IXLWorksheet hoja, string titulo, IReadOnlyList<string> lineas, int columnaDelLogo)
    {
        LogoEnExcel.Agregar(hoja, columnaDelLogo);
        hoja.Cell(1, 1).Value = titulo;
        hoja.Cell(1, 1).Style.Font.SetBold().Font.SetFontSize(18).Font.SetFontColor(Color(EstilosDeReporte.AzulOscuro));
        for (var indice = 0; indice < lineas.Count; indice++)
            hoja.Cell(indice + 2, 1).Value = lineas[indice];

        return lineas.Count + 3;
    }

    public static int EscribirTarjetas(
        IXLWorksheet hoja,
        int fila,
        IReadOnlyList<(Indicador Indicador, int ColumnaInicial, int ColumnaFinal)> tarjetas)
    {
        foreach (var (indicador, inicio, fin) in tarjetas)
            EscribirTarjeta(hoja, fila, indicador, inicio, fin);

        hoja.Row(fila + 1).Height = 30;
        return fila + 4;
    }

    public static void EstilizarEncabezadoDeTabla(IXLRange rango)
    {
        rango.Style.Font.SetBold().Font.SetFontColor(XLColor.White);
        rango.Style.Fill.SetBackgroundColor(Color(EstilosDeReporte.AzulGrisaceo));
        rango.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        rango.Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
    }

    public static void AlternarFilas(IXLWorksheet hoja, int primeraFila, int ultimaFila, int primeraColumna, int ultimaColumna)
    {
        for (var fila = primeraFila; fila <= ultimaFila; fila++)
            PintarFila(hoja, fila, primeraFila, primeraColumna, ultimaColumna);
    }

    public static void EstilizarTotales(IXLRange rango)
    {
        rango.Style.Font.SetBold().Font.SetFontColor(Color(EstilosDeReporte.AzulOscuro));
        rango.Style.Fill.SetBackgroundColor(Color(EstilosDeReporte.Ambar));
        rango.Style.Border.SetTopBorder(XLBorderStyleValues.Medium);
    }

    public static int InsertarImagen(IXLWorksheet hoja, ImagenDeGrafico grafico, int fila, int columna)
    {
        var alto = (int)Math.Round(grafico.Alto * (double)AnchoDeImagenEnPantalla / grafico.Ancho);
        using var flujo = new MemoryStream(grafico.Contenido);
        hoja.AddPicture(flujo)
            .MoveTo(hoja.Cell(fila, columna))
            .WithSize(AnchoDeImagenEnPantalla, alto);

        return (int)Math.Ceiling(alto / AlturaDeFilaEnPixeles) + 1;
    }

    public static XLColor Color(string html) => XLColor.FromHtml(html);

    private static void EscribirTarjeta(IXLWorksheet hoja, int fila, Indicador indicador, int inicio, int fin)
    {
        var etiqueta = hoja.Range(fila, inicio, fila, fin).Merge();
        var valor = hoja.Range(fila + 1, inicio, fila + 1, fin).Merge();
        var detalle = hoja.Range(fila + 2, inicio, fila + 2, fin).Merge();

        etiqueta.Value = indicador.Etiqueta;
        valor.Value = indicador.Valor;
        detalle.Value = indicador.Detalle ?? string.Empty;
        EstilizarTarjeta(etiqueta, valor, detalle);
    }

    private static void EstilizarTarjeta(IXLRange etiqueta, IXLRange valor, IXLRange detalle)
    {
        var tarjeta = etiqueta.Worksheet.Range(etiqueta.FirstCell().Address, detalle.LastCell().Address);

        tarjeta.Style.Fill.SetBackgroundColor(Color(EstilosDeReporte.FondoSuave));
        tarjeta.Style.Border.SetLeftBorder(XLBorderStyleValues.Thick);
        tarjeta.Style.Border.SetLeftBorderColor(Color(EstilosDeReporte.Ambar));
        tarjeta.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);
        tarjeta.Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
        tarjeta.Style.Alignment.SetIndent(1);
        etiqueta.Style.Font.SetBold().Font.SetFontSize(9).Font.SetFontColor(Color(EstilosDeReporte.TextoAtenuado));
        valor.Style.Font.SetBold().Font.SetFontSize(16).Font.SetFontColor(Color(EstilosDeReporte.AzulOscuro));
        detalle.Style.Font.SetFontSize(9).Font.SetFontColor(Color(EstilosDeReporte.AzulGrisaceo));
    }

    private static void PintarFila(IXLWorksheet hoja, int fila, int primeraFila, int primeraColumna, int ultimaColumna)
    {
        var rango = hoja.Range(fila, primeraColumna, fila, ultimaColumna);
        var alterna = (fila - primeraFila) % 2 == 1;
        rango.Style.Fill.SetBackgroundColor(alterna ? Color(EstilosDeReporte.FondoSuave) : XLColor.White);
        rango.Style.Border.SetBottomBorder(XLBorderStyleValues.Thin);
        rango.Style.Border.SetBottomBorderColor(Color(EstilosDeReporte.Borde));
    }
}
