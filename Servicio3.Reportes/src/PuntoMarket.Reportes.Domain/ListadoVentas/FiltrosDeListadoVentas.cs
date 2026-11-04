using PuntoMarket.Reportes.Domain.ValueObjects;

namespace PuntoMarket.Reportes.Domain.ListadoVentas;

public sealed record FiltrosDeListadoVentas(RangoDeFechas Rango, EstadoDeVenta? Estado, string? Cliente, int? Tope);
