using System.ComponentModel.DataAnnotations;

namespace PuntoMarket.Ventas.Frontend.Clients;

// Contratos HTTP propios del frontend: ninguna referencia a ensamblados de Reportes.
public sealed record FilaVentaReporte(int IdVenta, string Comprobante, DateTime Fecha, string NitCi, string RazonSocial, string Cajero, string Estado, decimal Monto);
public sealed record TotalesVentasReporte(int VentasCompletadas, int VentasAnuladas, decimal MontoTotalDeCompletadas, decimal PromedioPorVenta, decimal VentaMasAlta = 0m);
public sealed record FiltrosVentasReporte(DateOnly? Desde, DateOnly? Hasta, string? Estado, string? Cliente, int? Tope);
public sealed record ListadoVentasReporte(IReadOnlyList<FilaVentaReporte> Filas, TotalesVentasReporte Totales, FiltrosVentasReporte Filtros, DateTime GeneradoEn, string GeneradoPor);
public sealed record FilaCategoriaReporte(int IdCategoria, string Categoria, long UnidadesVendidas, decimal MontoVendido);
public sealed record TotalesCategoriasReporte(long TotalUnidades, decimal TotalMonto, string? CategoriaLider);
public sealed record FiltrosCategoriasReporte(DateOnly? Desde, DateOnly? Hasta, int? IdCategoria, int? Top);
public sealed record CategoriasReporte(IReadOnlyList<FilaCategoriaReporte> Filas, TotalesCategoriasReporte Totales, FiltrosCategoriasReporte Filtros, DateTime GeneradoEn, string GeneradoPor);
public sealed record ArchivoReporte(byte[] Contenido, string TipoContenido, string Nombre);

public class FiltroFechasReporte : IValidatableObject
{
    [DataType(DataType.Date)] public DateOnly? Desde { get; set; }
    [DataType(DataType.Date)] public DateOnly? Hasta { get; set; }
    public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Desde > Hasta) yield return new ValidationResult("La fecha inicial no puede superar la final.", [nameof(Hasta)]);
        if (Hasta == DateOnly.MaxValue) yield return new ValidationResult("La fecha final está fuera del rango permitido.", [nameof(Hasta)]);
    }
}
public sealed class FiltroListadoReporte : FiltroFechasReporte
{
    [RegularExpression("^(COMPLETADA|ANULADA)$", ErrorMessage = "Seleccione un estado válido.")]
    public string? Estado { get; set; }
    [StringLength(150, MinimumLength = 2)] public string? Cliente { get; set; }
    [Range(1, 1000)] public int? Tope { get; set; } = 1000;
}
public sealed class FiltroCategoriasReporte : FiltroFechasReporte
{
    [Range(1, int.MaxValue)] public int? IdCategoria { get; set; }
    [Range(1, 100)] public int? Top { get; set; }
}
