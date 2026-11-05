using PuntoMarket.Reportes.Application.Common;
using PuntoMarket.Reportes.Application.Ports.Comunes;
using PuntoMarket.Reportes.Application.Ports.Consultas;
using PuntoMarket.Reportes.Application.Ports.Exportacion;
using PuntoMarket.Reportes.Domain.ListadoVentas;
using PuntoMarket.Reportes.Domain.ValueObjects;

namespace PuntoMarket.Reportes.Application.UseCases.ListadoVentas.GenerarListadoVentas;

public class GenerarListadoVentasCasoDeUso
{
    public const int TopeMaximo = 1000;
    public const int LongitudMinimaDelCliente = 2;
    public const int LongitudMaximaDelCliente = 150;

    private readonly IConsultaListadoVentas _consulta;
    private readonly CreadorDeExportador<ReporteListadoVentas> _creador;
    private readonly IReloj _reloj;
    private readonly IUsuarioActual _usuarioActual;

    public GenerarListadoVentasCasoDeUso(
        IConsultaListadoVentas consulta,
        CreadorDeExportador<ReporteListadoVentas> creador,
        IReloj reloj,
        IUsuarioActual usuarioActual)
    {
        _consulta = consulta;
        _creador = creador;
        _reloj = reloj;
        _usuarioActual = usuarioActual;
    }

    public async Task<GenerarListadoVentasSalida> EjecutarAsync(
        GenerarListadoVentasEntrada entrada,
        CancellationToken cancelacion)
    {
        var filtros = ValidarFiltros(entrada);
        var resultado = await _consulta.ConsultarAsync(
            filtros.Rango,
            filtros.Estado,
            filtros.Cliente,
            filtros.Tope,
            cancelacion);
        var reporte = new ReporteListadoVentas(resultado.Filas, resultado.Totales, filtros, _reloj.Ahora(), _usuarioActual.NombreDeUsuario);

        return new GenerarListadoVentasSalida(reporte, GenerarArchivo(reporte, entrada.Formato));
    }

    private ArchivoGenerado? GenerarArchivo(ReporteListadoVentas reporte, FormatoDeExportacion formato) =>
        formato == FormatoDeExportacion.Json ? null : _creador.Exportar(reporte, formato);

    private static FiltrosDeListadoVentas ValidarFiltros(GenerarListadoVentasEntrada entrada)
    {
        var cliente = Normalizar(entrada.Cliente);

        ValidarEstado(entrada.Estado);
        ValidarCliente(cliente);
        ValidarTope(entrada.Tope);

        return new FiltrosDeListadoVentas(
            new RangoDeFechas(entrada.Desde, entrada.Hasta),
            entrada.Estado,
            cliente,
            entrada.Tope ?? TopeMaximo);
    }

    private static string? Normalizar(string? cliente) =>
        string.IsNullOrWhiteSpace(cliente) ? null : cliente.Trim();

    private static void ValidarEstado(EstadoDeVenta? estado)
    {
        if (estado.HasValue && !Enum.IsDefined(estado.Value))
            throw new FiltroDeReporteInvalidoException("El estado de la venta debe ser «COMPLETADA» o «ANULADA».");
    }

    private static void ValidarCliente(string? cliente)
    {
        if (cliente is null)
            return;

        if (cliente.Length is < LongitudMinimaDelCliente or > LongitudMaximaDelCliente)
            throw new FiltroDeReporteInvalidoException(
                $"El cliente debe tener entre {LongitudMinimaDelCliente} y {LongitudMaximaDelCliente} caracteres.");
    }

    private static void ValidarTope(int? tope)
    {
        if (tope is <= 0 or > TopeMaximo)
            throw new FiltroDeReporteInvalidoException($"El tope de filas debe estar entre 1 y {TopeMaximo}.");
    }
}
