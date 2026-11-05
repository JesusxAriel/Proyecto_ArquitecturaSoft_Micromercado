using PuntoMarket.Reportes.Domain.Tendencias;
using PuntoMarket.Reportes.Domain.ValueObjects;
using PuntoMarket.Reportes.Domain.VentasPorCategoria;
using PuntoMarket.Reportes.Infrastructure.Exportacion.Comunes;
using PuntoMarket.Reportes.Infrastructure.Exportacion.Graficos;

namespace PuntoMarket.Reportes.Infrastructure.Exportacion.VentasPorCategoria;

public class GeneradorDeGrafico
{
    public const int MaximoDeCategoriasEnBarras = 10;
    public const int MaximoDePartesEnDona = 5;

    public byte[] GenerarBarras(IReadOnlyList<FilaDeCategoria> filas) => MontoPorCategoria(filas).Contenido;

    public ImagenDeGrafico MontoPorCategoria(IReadOnlyList<FilaDeCategoria> filas)
    {
        var partes = ParteDelTotal.TopConOtros(AParteDelTotal(filas), MaximoDeCategoriasEnBarras);
        var datos = partes.Select(p => (p.Nombre, p.Monto)).ToList();

        return GraficoDeBarrasHorizontales.Generar("Monto vendido por categoría", "Monto vendido (Bs)", datos);
    }

    public ImagenDeGrafico ParticipacionPorCategoria(IReadOnlyList<FilaDeCategoria> filas) =>
        GraficoDeDona.Generar(
            "Participación por categoría",
            ParteDelTotal.TopConOtros(AParteDelTotal(filas), MaximoDePartesEnDona));

    public ImagenDeGrafico TendenciaDelMonto(TendenciaDeVentas tendencia)
    {
        var titulo = tendencia.Agrupacion == AgrupacionDeTendencia.Diaria
            ? "Tendencia del monto vendido por día"
            : "Tendencia del monto vendido por semana";

        return GraficoDeTendencia.Generar(titulo, tendencia);
    }

    public ImagenDeGrafico ProductosMasVendidos(IReadOnlyList<FilaDeProducto> productos)
    {
        var datos = productos.Select(p => (p.NombreProducto, p.MontoVendido)).ToList();

        return GraficoDeBarrasHorizontales.Generar("Los 5 productos más vendidos", "Monto vendido (Bs)", datos);
    }

    public ImagenDeGrafico MontoPorDia(IReadOnlyList<PuntoDeTendencia> puntos) =>
        GraficoDeColumnas.Generar("Monto de ventas por día", puntos);

    public ImagenDeGrafico ParticipacionPorCliente(IReadOnlyList<ParteDelTotal> partes) =>
        GraficoDeDona.Generar("Participación por cliente", partes);

    private static IReadOnlyList<ParteDelTotal> AParteDelTotal(IReadOnlyList<FilaDeCategoria> filas) =>
        filas
            .OrderByDescending(f => f.MontoVendido)
            .Select(f => new ParteDelTotal(f.NombreCategoria, f.MontoVendido))
            .ToList();
}
