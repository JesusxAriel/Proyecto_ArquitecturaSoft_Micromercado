using PuntoMarket.Reportes.Domain.ValueObjects;

namespace PuntoMarket.Reportes.Domain.VentasPorCategoria;

public sealed record FiltrosDeVentasPorCategoria(RangoDeFechas Rango, int? IdCategoria, int? Top);
