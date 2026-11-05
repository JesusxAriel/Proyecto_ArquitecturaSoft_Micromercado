using PuntoMarket.Reportes.Application.Common;
using PuntoMarket.Reportes.Application.Ports.Comunes;
using PuntoMarket.Reportes.Application.Ports.Consultas;
using PuntoMarket.Reportes.Application.Ports.Exportacion;
using PuntoMarket.Reportes.Domain.ValueObjects;
using PuntoMarket.Reportes.Domain.VentasPorCategoria;

namespace PuntoMarket.Reportes.Tests.VentasPorCategoria;

public class ConsultaFalsa : IConsultaVentasPorCategoria
{
    private readonly IReadOnlyList<FilaDeCategoria> _filas;

    public ConsultaFalsa(params FilaDeCategoria[] filas)
    {
        _filas = filas;
    }

    public int Llamadas { get; private set; }

    public RangoDeFechas? RangoRecibido { get; private set; }

    public int? IdCategoriaRecibido { get; private set; }

    public int? TopRecibido { get; private set; }

    public Task<ResultadoVentasPorCategoria> ConsultarAsync(
        RangoDeFechas rango,
        int? idCategoria,
        int? top,
        CancellationToken cancelacion)
    {
        Llamadas++;
        RangoRecibido = rango;
        IdCategoriaRecibido = idCategoria;
        TopRecibido = top;
        return Task.FromResult(new ResultadoVentasPorCategoria(_filas.Take(top ?? int.MaxValue).ToArray(), TotalesPorCategoria.Calcular(_filas)));
    }
}

public class ConsultaDeTendenciaFalsa : IConsultaTendenciaDeVentas
{
    private readonly IReadOnlyList<PuntoDeTendencia> _puntos;

    public ConsultaDeTendenciaFalsa(params PuntoDeTendencia[] puntos)
    {
        _puntos = puntos;
    }

    public int Llamadas { get; private set; }

    public IReadOnlyList<int>? CategoriasRecibidas { get; private set; }

    public Task<IReadOnlyList<PuntoDeTendencia>> ConsultarAsync(
        RangoDeFechas rango,
        IReadOnlyList<int> idsDeCategorias,
        CancellationToken cancelacion)
    {
        Llamadas++;
        CategoriasRecibidas = idsDeCategorias;
        return Task.FromResult(_puntos);
    }
}

public class ConsultaDeProductosFalsa : IConsultaProductosMasVendidos
{
    private readonly IReadOnlyList<FilaDeProducto> _filas;

    public ConsultaDeProductosFalsa(params FilaDeProducto[] filas)
    {
        _filas = filas;
    }

    public int Llamadas { get; private set; }

    public int? LimiteRecibido { get; private set; }

    public Task<IReadOnlyList<FilaDeProducto>> ConsultarAsync(
        RangoDeFechas rango,
        IReadOnlyList<int> idsDeCategorias,
        int limite,
        CancellationToken cancelacion)
    {
        Llamadas++;
        LimiteRecibido = limite;
        return Task.FromResult(_filas);
    }
}

public class RelojFalso : IReloj
{
    public static readonly DateTime Momento = new(2026, 5, 20, 14, 30, 0);

    public DateTime Ahora() => Momento;
}

public class UsuarioFalso : IUsuarioActual
{
    public int Id => 7;

    public string NombreDeUsuario => "ariana";
}

public class ExportadorFalso : IExportadorDeReporte<ReporteVentasPorCategoria>
{
    public ExportadorFalso(FormatoDeExportacion formato)
    {
        Formato = formato;
    }

    public FormatoDeExportacion Formato { get; }

    public ReporteVentasPorCategoria? ReporteRecibido { get; private set; }

    public ArchivoGenerado Exportar(ReporteVentasPorCategoria reporte)
    {
        ReporteRecibido = reporte;
        return new ArchivoGenerado([1, 2, 3], $"prueba/{Formato}", $"reporte.{Formato}");
    }
}

public class CreadorFalso : CreadorDeExportador<ReporteVentasPorCategoria>
{
    private readonly IReadOnlyList<IExportadorDeReporte<ReporteVentasPorCategoria>> _exportadores;

    public CreadorFalso(params IExportadorDeReporte<ReporteVentasPorCategoria>[] exportadores)
    {
        _exportadores = exportadores;
    }

    protected override IExportadorDeReporte<ReporteVentasPorCategoria>? CrearExportador(FormatoDeExportacion formato) =>
        _exportadores.FirstOrDefault(e => e.Formato == formato);
}
