using PuntoMarket.Reportes.Application.Common;
using PuntoMarket.Reportes.Domain.ListadoVentas;

namespace PuntoMarket.Reportes.Application.UseCases.ListadoVentas.GenerarListadoVentas;

public sealed record GenerarListadoVentasSalida(ReporteListadoVentas Reporte, ArchivoGenerado? Archivo);
