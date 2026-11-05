using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;
using Proyecto_Arquitectura_Micromercado.Application.Security;
using Proyecto_Arquitectura_Micromercado.Dtos.Usuarios;
using Proyecto_Arquitectura_Micromercado.Pages.Shared;
using Proyecto_Arquitectura_Micromercado.Servicios;
using System.Security.Claims;
using System.Text.Json;

namespace Proyecto_Arquitectura_Micromercado.Pages.Users;

[Authorize]
public sealed class CambiarPasswordModel(UsuariosApiClient usuariosApi) : PageModel
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

        try
        {
            await usuariosApi.CambiarPasswordAsync(
                new CambiarPasswordDto(Input.ContrasenaActual, Input.NuevaContrasena),
                cancellationToken);
        }
        catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.Unauthorized)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToPage("/Login", new { SesionVencida = true });
        }
        catch (HttpRequestException exception)
        {
            MensajeDeError = exception.StatusCode switch
            {
                HttpStatusCode.BadRequest or HttpStatusCode.Forbidden => exception.Message,
                _ => "El servicio de usuarios no está disponible. Intente nuevamente."
            };
            LimpiarContrasenas();
            return Page();
        }
        catch (JsonException)
        {
            MensajeDeError = "El servicio de usuarios devolvió una respuesta inválida. Intente nuevamente.";
            LimpiarContrasenas();
            return Page();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            MensajeDeError = "El servicio de usuarios no está disponible. Intente nuevamente.";
            LimpiarContrasenas();
            return Page();
        }

        if (User.Identity is not ClaimsIdentity identidad)
        {
            return RedirectToPage("/Login");
        }

        var claimCambioPassword = identidad.FindFirst(ClaimsDeIdentidad.DebeCambiarPassword);
        if (claimCambioPassword is not null)
            identidad.RemoveClaim(claimCambioPassword);

        identidad.AddClaim(new Claim(ClaimsDeIdentidad.DebeCambiarPassword, bool.FalseString));
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identidad));

        TempData["MensajeCambioPassword"] = "Tu nueva contraseña se usará en tu próximo inicio de sesión.";
        return RedirectToPage("/Index");
    }

    private void LimpiarContrasenas()
    {
        ModelState.Remove("Input.ContrasenaActual");
        ModelState.Remove("Input.NuevaContrasena");
        ModelState.Remove("Input.ConfirmarContrasena");
        Input.ContrasenaActual = string.Empty;
        Input.NuevaContrasena = string.Empty;
        Input.ConfirmarContrasena = string.Empty;
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