using PuntoMarket.Reportes.Application.Common;

namespace PuntoMarket.Reportes.Api.Contracts.VentasPorCategoria;

public sealed class PeticionVentasPorCategoria
{
    public DateOnly? Desde { get; init; }

    public DateOnly? Hasta { get; init; }

    public int? IdCategoria { get; init; }

    public int? Top { get; init; }

    public FormatoDeExportacion Formato { get; init; } = FormatoDeExportacion.Json;
}
