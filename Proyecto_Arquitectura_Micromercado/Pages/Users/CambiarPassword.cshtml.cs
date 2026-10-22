using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Arquitectura_Micromercado.Application.Common;
using Proyecto_Arquitectura_Micromercado.Application.Security;

namespace Proyecto_Arquitectura_Micromercado.Pages.Users;

[Authorize]
public sealed class CambiarPasswordModel(IAutenticacionService autenticacion) : PageModel
{
    [BindProperty]
    public CambiarPasswordInput Input { get; set; } = new();

    public string? MensajeDeError { get; private set; }

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (Input.NuevaContrasena != Input.ConfirmarContrasena)
        {
            MensajeDeError = "La nueva contraseña y la confirmación no coinciden.";
            return Page();
        }

        // Obtenemos el ID del usuario actual desde los claims de la sesión
        var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdString, out var usuarioId))
        {
            return RedirectToPage("/Login");
        }

        try
        {
            var actualizado = await autenticacion.CambiarPasswordAsync(
                usuarioId,
                Input.ContrasenaActual,
                Input.NuevaContrasena,
                cancellationToken);
            if (!actualizado)
            {
                MensajeDeError = "La contraseña actual es incorrecta o la cuenta ya no está activa.";
                return Page();
            }
        }
        catch (ErrorDePersistenciaException)
        {
            MensajeDeError = "Error al actualizar la contraseña en la base de datos. Intente nuevamente.";
            return Page();
        }

        // Una vez cambiada con éxito, redirigimos al inicio
        return RedirectToPage("/Index");
    }

    public sealed class CambiarPasswordInput
    {
        [Required(ErrorMessage = "Ingrese su contraseña actual.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña actual")]
        public string ContrasenaActual { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingrese la nueva contraseña.")]
        [DataType(DataType.Password)]
        [PasswordSegura]
        [Display(Name = "Nueva contraseña")]
        public string NuevaContrasena { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirme su nueva contraseña.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar nueva contraseña")]
        [Compare("NuevaContrasena", ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmarContrasena { get; set; } = string.Empty;
    }
}