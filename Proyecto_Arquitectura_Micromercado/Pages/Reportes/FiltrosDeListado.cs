using System.ComponentModel.DataAnnotations;
using Proyecto_Arquitectura_Micromercado.Dtos.Reportes;

namespace Proyecto_Arquitectura_Micromercado.Pages.Reportes;

public sealed class FiltrosDeListado
{
    public const int TopeMaximo = 1000;
    public const int LongitudMinimaDelCliente = 2;
    public const int LongitudMaximaDelCliente = 150;

    [Display(Name = "Desde")]
    public DateOnly? Desde { get; set; }

    [Display(Name = "Hasta")]
    public DateOnly? Hasta { get; set; }

    [Display(Name = "Estado")]
    public string? Estado { get; set; }

    [Display(Name = "Cliente (NIT/CI o razón social)")]
    [StringLength(
        LongitudMaximaDelCliente,
        MinimumLength = LongitudMinimaDelCliente,
        ErrorMessage = "El cliente debe tener entre 2 y 150 caracteres.")]
    public string? Cliente { get; set; }

    [Display(Name = "Tope de filas (opcional)")]
    [Range(1, TopeMaximo, ErrorMessage = "El tope de filas debe ser un número entero entre 1 y 1000.")]
    public int? Tope { get; set; }

    public FiltrosListadoVentasDto ADto() => new(Desde, Hasta, Normalizar(Estado), Normalizar(Cliente), Tope);

    private static string? Normalizar(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}
