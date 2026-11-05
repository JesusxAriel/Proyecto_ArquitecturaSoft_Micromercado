using PuntoMarket.Reportes.Application.Common;
using PuntoMarket.Reportes.Domain.VentasPorCategoria;

namespace PuntoMarket.Reportes.Application.UseCases.VentasPorCategoria.GenerarVentasPorCategoria;

public sealed record GenerarVentasPorCategoriaSalida(ReporteVentasPorCategoria Reporte, ArchivoGenerado? Archivo);
