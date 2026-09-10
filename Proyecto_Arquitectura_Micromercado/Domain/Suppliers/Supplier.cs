using System.ComponentModel.DataAnnotations;

namespace Proyecto_Arquitectura_Micromercado.Domain.Suppliers;

public sealed class Supplier : IValidatableObject
{
    public int Id { get; set; }

    [Display(Name = "Nombre de la empresa")]
    [Required(ErrorMessage = "Campo obligatorio.")]
    [StringLength(150)]
    [SupplierText]
    public string NombreEmpresa { get; set; } = string.Empty;

    [Display(Name = "Teléfono de contacto")]
    [Required(ErrorMessage = "Campo obligatorio.")]
    [RegularExpression(SupplierValidation.TelefonoPattern, ErrorMessage = SupplierValidation.TelefonoMessage)]
    public string NumeroEmpresa { get; set; } = string.Empty;

    [Display(Name = "Correo de referencia")]
    [RegularExpression(SupplierValidation.CorreoPattern, ErrorMessage = SupplierValidation.CorreoMessage)]
    [StringLength(150)]
    public string? CorreoReferencia { get; set; }

    [Display(Name = "Proveedor autogestionado")]
    public bool EsAutogestionado { get; set; }

    public bool EstaActivo { get; set; } = true;

    /// <summary>
    /// Regla que involucra a más de un campo, por lo que no puede resolverse
    /// con un atributo sobre una sola propiedad.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EsAutogestionado && string.IsNullOrWhiteSpace(CorreoReferencia))
        {
            yield return new ValidationResult(
                SupplierValidation.CorreoRequeridoParaAutogestionadoMessage,
                [nameof(CorreoReferencia)]);
        }
    }
}

public sealed class SupplierListItem
{
    public int Id { get; init; }
    public string NombreEmpresa { get; init; } = string.Empty;
    public string NumeroEmpresa { get; init; } = string.Empty;
    public string CorreoReferencia { get; init; } = string.Empty;
    public bool EsAutogestionado { get; init; }
}