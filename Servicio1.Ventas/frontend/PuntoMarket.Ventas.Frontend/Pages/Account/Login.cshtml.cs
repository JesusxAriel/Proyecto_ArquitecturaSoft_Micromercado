using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PuntoMarket.Ventas.Frontend.Clients;
using PuntoMarket.Ventas.Frontend.Security;
namespace PuntoMarket.Ventas.Frontend.Pages.Account;

[AllowAnonymous]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class LoginModel(IUsuariosApiClient api, ValidadorTokenUsuarios validador, SesionesUsuario sesiones) : PageModel
{
    public sealed class EntradaLogin {
        [Required(ErrorMessage = "Ingrese su correo.")]
        [EmailAddress(ErrorMessage = "Ingrese un correo válido.")]
        public string Correo { get; set; } = "";
        [Required(ErrorMessage = "Ingrese su contraseña.")]
        [DataType(DataType.Password)] public string Contrasena { get; set; } = "";
    }
    [BindProperty] public EntradaLogin Input { get; set; } = new();
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }
    public string? ErrorLogin { get; private set; }
    public string? UsuarioPreparado { get; private set; }
    public string? RolPreparado { get; private set; }
    [TempData] public string? CambioPasswordCompletado { get; set; }
    private bool RetornoSeguro() => string.IsNullOrEmpty(ReturnUrl) || Url.IsLocalUrl(ReturnUrl);
    public async Task<IActionResult> OnGetAsync()
    {
        if (!RetornoSeguro()) return BadRequest();
        var actual = await HttpContext.AuthenticateAsync(AccesoUsuariosPreparado.Esquema);
        if (actual.Succeeded) { UsuarioPreparado = actual.Principal?.Identity?.Name; RolPreparado = actual.Principal?.FindFirst(ClaimTypes.Role)?.Value; }
        return Page();
    }
    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        string? contrasena = Input.Contrasena;
        var entradaValida = ModelState.IsValid;
        Input.Contrasena = "";
        ModelState.Remove("Input.Contrasena"); // Nunca repoblar ni conservar el valor intentado.
        if (!RetornoSeguro()) { contrasena = null; return BadRequest(); }
        if (!entradaValida) { contrasena = null; ErrorLogin = "Revise el correo y la contraseña."; Response.StatusCode = 400; return Page(); }
        string? jwt = null; string? creada = null;
        try
        {
            var anterior = await HttpContext.AuthenticateAsync(AccesoUsuariosPreparado.Esquema);
            sesiones.Cerrar(anterior.Principal?.FindFirst(AccesoUsuariosPreparado.ClaimSesion)?.Value);
            await HttpContext.SignOutAsync(AccesoUsuariosPreparado.Esquema);
            HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
            jwt = await api.IniciarSesion(Input.Correo, contrasena!, ct);
            var identidad = validador.Validar(jwt);
            creada = sesiones.Crear(jwt);
            var principal = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, identidad.Id.ToString(CultureInfo.InvariantCulture)),
                new Claim(ClaimTypes.Name, identidad.Usuario), new Claim(ClaimTypes.Role, identidad.Rol),
                new Claim(AccesoUsuariosPreparado.ClaimSesion, creada),
                new Claim("debeCambiarPassword", identidad.DebeCambiarPassword ? "true" : "false")
            ], AccesoUsuariosPreparado.Esquema));
            await HttpContext.SignInAsync(AccesoUsuariosPreparado.Esquema, principal, new AuthenticationProperties {
                ExpiresUtc = identidad.VenceEn, IsPersistent = false, AllowRefresh = false
            });
            creada = null;
            if (identidad.DebeCambiarPassword)
                return RedirectToPage("/Account/CambiarPassword");
            return string.IsNullOrEmpty(ReturnUrl) ? RedirectToPage("/Sales/Index") : LocalRedirect(ReturnUrl);
        }
        catch (UsuariosApiException e) { ErrorLogin = e.Message; Response.StatusCode = e.Estado; return Page(); }
        catch (TokenUsuariosInvalidoException) { ErrorLogin = "No se pudo validar la sesión. Intente iniciar sesión nuevamente."; Response.StatusCode = 401; return Page(); }
        finally { if (creada is not null) sesiones.Cerrar(creada); contrasena = null; jwt = null; }
    }
}
