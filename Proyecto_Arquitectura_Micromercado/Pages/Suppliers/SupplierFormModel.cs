using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Proyecto_Arquitectura_Micromercado.Domain.Suppliers;

namespace Proyecto_Arquitectura_Micromercado.Pages.Suppliers;

// Modelo del formulario de proveedores (alta y edición).
//
// Es lo que el navegador envía y lo que la pantalla valida: lleva los atributos de
// DataAnnotations que generan la validación del lado del cliente y los mensajes junto a
// cada campo. La entidad Supplier de Domain queda libre de tecnología web; la Page
// convierte este modelo con ToSupplier() antes de llamar al servicio, que vuelve a
// aplicar las reglas de negocio por su cuenta.
//
// No incluye EstaActivo: el estado lo maneja el servidor, nunca el formulario.
public sealed class SupplierFormModel : IValidatableObject
{
    public int Id { get; set; }

    [Display(Name = "Nombre de la empresa")]
    [Required(ErrorMessage = "Campo obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre no puede exceder los 150 caracteres.")]
    [SupplierText]
    public string NombreEmpresa { get; set; } = string.Empty;

    [Display(Name = "Teléfono de contacto")]
    [Required(ErrorMessage = "Campo obligatorio.")]
    [Telefono(ErrorMessage = SupplierValidation.TelefonoMessage)]
    public string NumeroEmpresa { get; set; } = string.Empty;

    [Display(Name = "Correo de referencia")]
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = SupplierValidation.CorreoMessage)]
    [StringLength(150, ErrorMessage = "El correo no puede exceder los 150 caracteres.")]
    public string? CorreoReferencia { get; set; }

    [Display(Name = "Proveedor autogestionado")]
    public bool EsAutogestionado { get; set; }

    // Validación entre campos: es de pantalla porque depende de cómo se llenó el
    // formulario. La misma regla de negocio la aplica también SupplierService.
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EsAutogestionado && string.IsNullOrWhiteSpace(CorreoReferencia))
        {
            yield return new ValidationResult(
                SupplierValidation.CorreoRequeridoParaAutogestionadoMessage,
                [nameof(CorreoReferencia)]);
        }
    }

    public Supplier ToSupplier() => new()
    {
        Id = Id,
        NombreEmpresa = NombreEmpresa,
        NumeroEmpresa = NumeroEmpresa,
        CorreoReferencia = CorreoReferencia,
        EsAutogestionado = EsAutogestionado
    };
}

public sealed class SupplierTextAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is not string text || string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        return !text.Any(char.IsControl);
    }

    public override string FormatErrorMessage(string name) =>
        $"El campo {name} debe tener palabras separadas por un solo espacio.";
}

public sealed class TelefonoAttribute : ValidationAttribute
{
    private static readonly Regex Pattern =
        new(SupplierValidation.TelefonoPattern, RegexOptions.CultureInvariant);

    public override bool IsValid(object? value)
    {
        if (value is not string text || string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        return Pattern.IsMatch(text.Trim());
    }

    public override string FormatErrorMessage(string name) =>
        SupplierValidation.TelefonoMessage;
}
