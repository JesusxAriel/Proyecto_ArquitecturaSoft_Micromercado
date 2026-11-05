using System.ComponentModel.DataAnnotations;

namespace PuntoMarket.Ventas.Api.Contracts;

public sealed class RegistrarVentaSolicitud : IValidatableObject
{
    [Range(1, int.MaxValue)] public int IdCliente { get; init; }
    [Required, MinLength(1)] public List<LineaVentaSolicitud?>? Lineas { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Lineas?.Any(linea => linea is null) == true)
            yield return new ValidationResult("Todas las líneas deben contener un producto y cantidad.", [nameof(Lineas)]);
    }
}

public sealed class LineaVentaSolicitud
{
    [Range(1, int.MaxValue)] public int IdProducto { get; init; }
    [Range(1, int.MaxValue)] public int Cantidad { get; init; }
    public decimal? PrecioEsperado { get; init; }
}

public sealed class AnularVentaSolicitud
{
    [Required, StringLength(255)] public string? Motivo { get; init; }
}
