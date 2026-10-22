using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Arquitectura_Micromercado.Application.Common;
using Proyecto_Arquitectura_Micromercado.Application.Security;
using Proyecto_Arquitectura_Micromercado.Pages.Shared;

namespace Proyecto_Arquitectura_Micromercado.Pages;

[AllowAnonymous]
public sealed class LoginModel(IAutenticacionService autenticacion) : PageModel
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

        UsuarioAutenticado? usuario;

        try
        {
            usuario = await autenticacion.AutenticarAsync(
                Input.Correo,
                Input.Contrasena,
                cancellationToken);
        }
        catch (ErrorDePersistenciaException)
        {
            MensajeDeError = "No se pudo conectar con la base de datos. Intente nuevamente.";
            return Page();
        }

        if (usuario is null)
        {
            MensajeDeError = MensajeCredencialesInvalidas;
            ModelState.Remove("Input.Contrasena");
            Input.Contrasena = string.Empty;
            return Page();
        }

        HttpContext.Session.Clear();

        var principal = PrincipalDeSesion.Crear(
            usuario.Id,
            usuario.NombreUsuario,
            usuario.Rol,
            CookieAuthenticationDefaults.AuthenticationScheme,
            usuario.DebeCambiarPassword);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        HttpContext.Session.SetString(VariablesDeSesion.NombreParaMostrar, usuario.NombreCompleto);
        HttpContext.Session.SetString(
            VariablesDeSesion.InicioDeSesion,
            DateTime.Now.ToString("dd/MM/yyyy HH:mm"));

        // REQUISITO: Si debe cambiar contraseña en el primer acceso, redirigir obligatoriamente
        if (usuario.DebeCambiarPassword)
        {
            return RedirectToPage("/Users/CambiarPassword");
        }

        return !string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl)
            ? LocalRedirect(ReturnUrl)
            : RedirectToPage("/Index");
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