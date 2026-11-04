using PuntoMarket.Reportes.Application.Common;
using PuntoMarket.Reportes.Domain.ListadoVentas;

namespace PuntoMarket.Reportes.Application.UseCases.ListadoVentas.GenerarListadoVentas;

public sealed record GenerarListadoVentasEntrada(
    DateOnly? Desde,
    DateOnly? Hasta,
    EstadoDeVenta? Estado,
    string? Cliente,
    int? Tope,
    FormatoDeExportacion Formato);
