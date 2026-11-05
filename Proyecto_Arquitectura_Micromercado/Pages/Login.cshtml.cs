using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.IdentityModel.Tokens;
using Proyecto_Arquitectura_Micromercado.Application.Security;
using Proyecto_Arquitectura_Micromercado.Dtos.Usuarios;
using Proyecto_Arquitectura_Micromercado.Pages.Shared;
using Proyecto_Arquitectura_Micromercado.Servicios;
using System.Globalization;
using System.Text.Json;

namespace Proyecto_Arquitectura_Micromercado.Pages;

[AllowAnonymous]
public sealed class LoginModel(
    UsuariosApiClient usuariosApi,
    IValidadorJwt validadorJwt) : PageModel
{
    private const string MensajeCredencialesInvalidas = "Correo o contraseña incorrectos.";

    [BindProperty]
    public CredencialesInput Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool SesionVencida { get; set; }

    public string? MensajeDeError { get; private set; }

    public IActionResult OnGet() =>
        User.Identity?.IsAuthenticated == true ? RedirectToPage("/Index") : Page();

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.Session.Clear();

        JwtValidado jwt;
        string token;
        try
        {
            var respuesta = await usuariosApi.IniciarSesionAsync(
                new IniciarSesionDto(Input.Correo, Input.Contrasena),
                cancellationToken);
            token = respuesta.Token;
            jwt = validadorJwt.ValidarConExpiracion(token);
        }
        catch (HttpRequestException exception)
        {
            MensajeDeError = exception.StatusCode switch
            {
                System.Net.HttpStatusCode.Unauthorized => MensajeCredencialesInvalidas,
                System.Net.HttpStatusCode.BadRequest => "Ingrese un correo válido y una contraseña.",
                _ => "El servicio de usuarios no está disponible. Intente nuevamente."
            };
            LimpiarContrasena();
            return Page();
        }
        catch (JsonException)
        {
            MensajeDeError = "El servicio de usuarios devolvió una respuesta inválida. Intente nuevamente.";
            LimpiarContrasena();
            return Page();
        }
        catch (SecurityTokenException)
        {
            MensajeDeError = "No se pudo validar la respuesta de autenticación. Intente nuevamente.";
            LimpiarContrasena();
            return Page();
        }
        catch (ArgumentException)
        {
            MensajeDeError = "No se pudo validar la respuesta de autenticación. Intente nuevamente.";
            LimpiarContrasena();
            return Page();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            MensajeDeError = "El servicio de usuarios no está disponible. Intente nuevamente.";
            LimpiarContrasena();
            return Page();
        }

        var principalValidado = jwt.Principal;
        var id = int.Parse(
            principalValidado.FindFirst("sub")!.Value,
            NumberStyles.None,
            CultureInfo.InvariantCulture);
        var nombreUsuario = principalValidado.FindFirst("unique_name")!.Value;
        var nombreCompleto = principalValidado.FindFirst("nombre")!.Value;
        var rol = principalValidado.FindFirst("role")!.Value;
        var debeCambiarPassword = bool.Parse(
            principalValidado.FindFirst("debeCambiarPassword")!.Value);

        var principal = PrincipalDeSesion.Crear(
            id,
            nombreUsuario,
            rol,
            CookieAuthenticationDefaults.AuthenticationScheme,
            debeCambiarPassword,
            jwt.ExpiraUtc);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        HttpContext.Session.SetString(VariablesDeSesion.Jwt, token);
        HttpContext.Session.SetString(VariablesDeSesion.NombreParaMostrar, nombreCompleto);
        HttpContext.Session.SetString(
            VariablesDeSesion.InicioDeSesion,
            DateTime.Now.ToString("dd/MM/yyyy HH:mm"));

        if (debeCambiarPassword)
        {
            return RedirectToPage("/Users/CambiarPassword");
        }

        return !string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl)
            ? LocalRedirect(ReturnUrl)
            : RedirectToPage("/Index");
    }

    private void LimpiarContrasena()
    {
        ModelState.Remove("Input.Contrasena");
        Input.Contrasena = string.Empty;
    }

    public sealed class CredencialesInput
    {
        [Required(ErrorMessage = "Ingrese su correo electrónico.")]
        [EmailAddress(ErrorMessage = "El formato del correo no es válido.")]
        [Display(Name = "Correo electrónico")]
        public string Correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingrese su contraseña.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Contrasena { get; set; } = string.Empty;
    }
}