using PuntoMarket.Reportes.Application.Common;
using PuntoMarket.Reportes.Application.UseCases.ListadoVentas.GenerarListadoVentas;
using PuntoMarket.Reportes.Domain.ListadoVentas;

namespace PuntoMarket.Reportes.Api.Contracts.ListadoVentas;

public sealed class PeticionListadoVentas
{
    public DateOnly? Desde { get; init; }

    public DateOnly? Hasta { get; init; }

    public string? Estado { get; init; }

    public string? Cliente { get; init; }

    public int? Tope { get; init; }

    public string? Formato { get; init; }

    public GenerarListadoVentasEntrada AEntrada() =>
        new(Desde, Hasta, ConvertirEstado(Estado), Cliente, Tope, ConvertirFormato(Formato));

    private static EstadoDeVenta? ConvertirEstado(string? estado)
    {
        if (string.IsNullOrWhiteSpace(estado))
            return null;

        return Enum.TryParse<EstadoDeVenta>(estado.Trim(), ignoreCase: true, out var valor) && Enum.IsDefined(valor)
            ? valor
            : throw new FiltroDeReporteInvalidoException("El estado debe ser «COMPLETADA» o «ANULADA».");
    }

    private static FormatoDeExportacion ConvertirFormato(string? formato)
    {
        if (string.IsNullOrWhiteSpace(formato))
            return FormatoDeExportacion.Json;

        return Enum.TryParse<FormatoDeExportacion>(formato.Trim(), ignoreCase: true, out var valor) && Enum.IsDefined(valor)
            ? valor
            : throw new FiltroDeReporteInvalidoException("El formato debe ser «json», «pdf» o «xlsx».");
    }
}
