using PuntoMarket.Reportes.Application.Common;

namespace PuntoMarket.Reportes.Application.UseCases.VentasPorCategoria.GenerarVentasPorCategoria;

public sealed record GenerarVentasPorCategoriaEntrada(
    DateOnly? Desde,
    DateOnly? Hasta,
    int? IdCategoria,
    int? Top,
    FormatoDeExportacion Formato);
