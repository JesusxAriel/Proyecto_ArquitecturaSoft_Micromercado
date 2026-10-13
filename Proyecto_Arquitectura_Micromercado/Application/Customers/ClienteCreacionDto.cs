using System.ComponentModel.DataAnnotations;

namespace Proyecto_Arquitectura_Micromercado.Application.Customers;

public sealed class ClienteCreacionDto
{
    [Required(ErrorMessage = "Ingrese el CI/NIT.")]
    [StringLength(30, ErrorMessage = "El CI/NIT admite hasta 30 caracteres.")]
    [Display(Name = "CI/NIT")]
    public string NitCi { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese la razón social.")]
    [StringLength(150, ErrorMessage = "La razón social admite hasta 150 caracteres.")]
    [Display(Name = "Razón social")]
    public string RazonSocial { get; set; } = string.Empty;
}
