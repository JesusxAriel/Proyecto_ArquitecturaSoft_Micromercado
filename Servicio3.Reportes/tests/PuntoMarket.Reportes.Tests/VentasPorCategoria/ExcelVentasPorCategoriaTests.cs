using System.Text;
using ClosedXML.Excel;
using PuntoMarket.Reportes.Domain.Tendencias;
using PuntoMarket.Reportes.Domain.ValueObjects;
using PuntoMarket.Reportes.Domain.VentasPorCategoria;
using PuntoMarket.Reportes.Infrastructure.Exportacion.VentasPorCategoria;

namespace PuntoMarket.Reportes.Tests.VentasPorCategoria;

public class ExcelVentasPorCategoriaTests
{
    private static readonly GeneradorDeGrafico Grafico = new();

    private static ReporteVentasPorCategoria CrearReporte(int categorias, bool conDetalle)
    {
        var filas = Enumerable.Range(1, categorias)
            .Select(i => new FilaDeCategoria(i, $"Categoría con nombre largo {i}", i * 2, 100m * (categorias - i + 1)))
            .ToList();
        var filtros = new FiltrosDeVentasPorCategoria(RangoDeFechas.SinLimites(), null, null);
        var reporte = ReporteVentasPorCategoria.Crear(filas, filtros, new DateTime(2026, 5, 20, 14, 30, 5), "ariana");

        return conDetalle && categorias > 0 ? reporte.ConDetalle(Tendencia(), Productos()) : reporte;
    }

    private static TendenciaDeVentas Tendencia() =>
        TendenciaDeVentas.Desde(
        [
            new PuntoDeTendencia(new DateOnly(2026, 5, 1), 50m),
            new PuntoDeTendencia(new DateOnly(2026, 5, 3), 150m)
        ]);

    private static IReadOnlyList<FilaDeProducto> Productos() =>
    [
        new FilaDeProducto(1, "Leche entera 1 L", 10, 65m),
        new FilaDeProducto(2, "Yogur frutado 1 kg", 4, 48m)
    ];

    private static XLWorkbook Abrir(ReporteVentasPorCategoria reporte)
    {
        var archivo = new VentasPorCategoriaExcel(Grafico).Exportar(reporte);

        Assert.Equal("PK", Encoding.ASCII.GetString(archivo.Contenido, 0, 2));
        return new XLWorkbook(new MemoryStream(archivo.Contenido));
    }

    [Fact]
    public void Con_detalle_el_libro_tiene_las_hojas_resumen_productos_y_tendencia()
    {
        using var libro = Abrir(CrearReporte(3, conDetalle: true));

        Assert.Equal(["Resumen", "Productos", "Tendencia"], libro.Worksheets.Select(h => h.Name));
    }

    [Fact]
    public void Sin_detalle_el_libro_solo_tiene_la_hoja_resumen()
    {
        using var libro = Abrir(CrearReporte(3, conDetalle: false));

        Assert.Equal(["Resumen"], libro.Worksheets.Select(h => h.Name));
    }

    [Fact]
    public void Las_formulas_de_totales_y_participacion_calculan_los_valores_correctos()
    {
        using var libro = Abrir(CrearReporte(3, conDetalle: true));
        var resumen = libro.Worksheet("Resumen");

        Assert.Equal(600d, resumen.Cell(15, 4).Value.GetNumber());
        Assert.Equal(12d, resumen.Cell(15, 3).Value.GetNumber());
        Assert.Equal(0.5d, resumen.Cell(12, 5).Value.GetNumber(), 6);
        Assert.Equal(1d, resumen.Cell(15, 5).Value.GetNumber(), 6);
    }

    [Fact]
    public void La_hoja_de_productos_calcula_la_participacion_sobre_el_total_del_resumen()
    {
        using var libro = Abrir(CrearReporte(3, conDetalle: true));
        var productos = libro.Worksheet("Productos");

        Assert.Equal("Leche entera 1 L", productos.Cell(2, 2).GetString());
        Assert.Equal(65d / 600d, productos.Cell(2, 5).Value.GetNumber(), 6);
        Assert.True(productos.AutoFilter.IsEnabled);
        Assert.Equal(1, productos.SheetView.SplitRow);
    }

    [Fact]
    public void La_hoja_de_tendencia_suma_los_montos_con_una_formula()
    {
        using var libro = Abrir(CrearReporte(3, conDetalle: true));
        var tendencia = libro.Worksheet("Tendencia");

        Assert.Equal("Fecha", tendencia.Cell(1, 1).GetString());
        Assert.Equal(200d, tendencia.Cell(5, 2).Value.GetNumber());
        Assert.StartsWith("SUM(", tendencia.Cell(5, 2).FormulaA1);
    }

    [Fact]
    public void El_resumen_incluye_el_logo_los_indicadores_y_los_graficos_como_imagenes()
    {
        using var libro = Abrir(CrearReporte(3, conDetalle: true));
        var resumen = libro.Worksheet("Resumen");

        Assert.Equal(5, resumen.Pictures.Count);
        Assert.Equal("MONTO TOTAL", resumen.Cell(7, 1).GetString());
        Assert.Equal("Bs 600,00", resumen.Cell(8, 1).GetString());
        Assert.Equal("CATEGORÍA LÍDER", resumen.Cell(7, 5).GetString());
    }

    [Fact]
    public void Con_una_sola_categoria_el_excel_se_genera_con_su_total()
    {
        using var libro = Abrir(CrearReporte(1, conDetalle: false));

        Assert.Equal(100d, libro.Worksheet("Resumen").Cell(13, 4).Value.GetNumber());
    }

    [Fact]
    public void Con_muchas_categorias_el_excel_se_genera_y_filtra_toda_la_tabla()
    {
        using var libro = Abrir(CrearReporte(60, conDetalle: true));
        var resumen = libro.Worksheet("Resumen");

        Assert.Equal("Totales", resumen.Cell(72, 2).GetString());
        Assert.True(resumen.AutoFilter.IsEnabled);
        Assert.Equal("SUM(D12:D71)", resumen.Cell(72, 4).FormulaA1);
    }

    [Fact]
    public void Sin_datos_el_excel_muestra_el_mensaje_con_el_encabezado_y_sin_indicadores()
    {
        using var libro = Abrir(CrearReporte(0, conDetalle: false));
        var resumen = libro.Worksheet("Resumen");

        Assert.Equal("Ventas por categoría", resumen.Cell(1, 1).GetString());
        Assert.StartsWith("Reporte generado: ariana - ", resumen.Cell(5, 1).GetString());
        Assert.Equal("Sin resultados para los filtros seleccionados", resumen.Cell(7, 1).GetString());
        Assert.Single(resumen.Pictures);
    }
}
