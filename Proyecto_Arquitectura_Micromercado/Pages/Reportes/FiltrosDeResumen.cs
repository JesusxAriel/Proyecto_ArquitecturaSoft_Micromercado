using System.ComponentModel.DataAnnotations;
using Proyecto_Arquitectura_Micromercado.Dtos.Reportes;

namespace Proyecto_Arquitectura_Micromercado.Pages.Reportes;

public sealed class FiltrosDeResumen
{
    public const int TopMaximo = 100;

    [Display(Name = "Desde")]
    public DateOnly? Desde { get; set; }

    [Display(Name = "Hasta")]
    public DateOnly? Hasta { get; set; }

    [Display(Name = "Categoría")]
    public int? IdCategoria { get; set; }

    [Display(Name = "Máximo de categorías (opcional)")]
    [Range(1, TopMaximo, ErrorMessage = "El valor de «Máximo de categorías» debe ser un número entero entre 1 y 100.")]
    public int? Top { get; set; }

    public FiltrosVentasPorCategoriaDto ADto() => new(Desde, Hasta, IdCategoria, Top);
}
