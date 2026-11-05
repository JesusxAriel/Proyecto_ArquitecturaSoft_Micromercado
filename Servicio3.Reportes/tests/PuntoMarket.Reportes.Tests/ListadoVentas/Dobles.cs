using PuntoMarket.Reportes.Application.Common;
using PuntoMarket.Reportes.Application.Ports.Comunes;
using PuntoMarket.Reportes.Application.Ports.Consultas;
using PuntoMarket.Reportes.Application.Ports.Exportacion;
using PuntoMarket.Reportes.Domain.ListadoVentas;
using PuntoMarket.Reportes.Domain.ValueObjects;

namespace PuntoMarket.Reportes.Tests.ListadoVentas;

public class ConsultaFalsa : IConsultaListadoVentas
{
    private readonly IReadOnlyList<FilaDeVenta> _filas;

    public ConsultaFalsa(params FilaDeVenta[] filas)
    {
        _filas = filas;
    }

    public int Llamadas { get; private set; }

    public RangoDeFechas? RangoRecibido { get; private set; }

    public EstadoDeVenta? EstadoRecibido { get; private set; }

    public string? ClienteRecibido { get; private set; }

    public int? TopeRecibido { get; private set; }

    public Task<ResultadoListadoVentas> ConsultarAsync(
        RangoDeFechas rango,
        EstadoDeVenta? estado,
        string? cliente,
        int? tope,
        CancellationToken cancelacion)
    {
        Llamadas++;
        RangoRecibido = rango;
        EstadoRecibido = estado;
        ClienteRecibido = cliente;
        TopeRecibido = tope;
        return Task.FromResult(new ResultadoListadoVentas(_filas.Take(tope ?? int.MaxValue).ToArray(), TotalesListadoVentas.Calcular(_filas)));
    }
}

public class RelojFalso : IReloj
{
    public static readonly DateTime Momento = new(2026, 11, 3, 9, 15, 0);

    public DateTime Ahora() => Momento;
}

public class UsuarioFalso : IUsuarioActual
{
    public int Id => 3;

    public string NombreDeUsuario => "alejandra";
}

public class ExportadorFalso : IExportadorDeReporte<ReporteListadoVentas>
{
    public ExportadorFalso(FormatoDeExportacion formato)
    {
        Formato = formato;
    }

    public FormatoDeExportacion Formato { get; }

    public ReporteListadoVentas? ReporteRecibido { get; private set; }

    public ArchivoGenerado Exportar(ReporteListadoVentas reporte)
    {
        ReporteRecibido = reporte;
        return new ArchivoGenerado([1, 2, 3], $"prueba/{Formato}", $"reporte.{Formato}");
    }
}

public class CreadorFalso : CreadorDeExportador<ReporteListadoVentas>
{
    private readonly IReadOnlyList<IExportadorDeReporte<ReporteListadoVentas>> _exportadores;

    public CreadorFalso(params IExportadorDeReporte<ReporteListadoVentas>[] exportadores)
    {
        _exportadores = exportadores;
    }

    protected override IExportadorDeReporte<ReporteListadoVentas>? CrearExportador(FormatoDeExportacion formato) =>
        _exportadores.FirstOrDefault(e => e.Formato == formato);
}
