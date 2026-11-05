using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PuntoMarket.Ventas.Frontend.Clients;
using PuntoMarket.Ventas.Frontend.Security;

namespace PuntoMarket.Ventas.Frontend.Pages.Account;

[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CambiarPasswordModel(
    IUsuariosApiClient api,
    ResolucionSesion resolucionSesion) : PageModel
{
    [BindProperty]
    public EntradaCambioPassword Input { get; set; } = new();

    public string? ErrorCambio { get; private set; }

    public sealed class EntradaCambioPassword
    {
        public string ContrasenaActual { get; set; } = "";
        public string NuevaContrasena { get; set; } = "";
        public string ConfirmarContrasena { get; set; } = "";
    }

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var actual = Input.ContrasenaActual;
        var nueva = Input.NuevaContrasena;
        var confirmacion = Input.ConfirmarContrasena;
        Input = new EntradaCambioPassword();
        ModelState.Clear();

        if (string.IsNullOrWhiteSpace(actual))
            return Error("Ingrese su contraseña actual.");
        if (!CumplePolitica(nueva))
            return Error("La nueva contraseña debe tener al menos 8 caracteres, una mayúscula, una minúscula, un número y un carácter especial.");
        if (!string.Equals(nueva, confirmacion, StringComparison.Ordinal))
            return Error("La nueva contraseña y su confirmación no coinciden.");

        var token = resolucionSesion.ObtenerToken();
        if (token is null)
        {
            await resolucionSesion.InvalidarAsync();
            return RedirectToPage("/Account/Login");
        }

        try
        {
            await api.CambiarPassword(token, actual, nueva, cancellationToken);
            TempData["CambioPasswordCompletado"] = "La contraseña se cambió correctamente. Inicie sesión con su nueva contraseña.";
            await resolucionSesion.InvalidarAsync();
            return RedirectToPage("/Account/Login");
        }
        catch (UsuariosApiException exception) when (exception.Estado == 401)
        {
            await resolucionSesion.InvalidarAsync();
            return RedirectToPage("/Account/Login");
        }
        catch (UsuariosApiException exception) when (exception.Estado == 403)
        {
            await resolucionSesion.InvalidarAsync();
            return RedirectToPage("/Account/Login");
        }
        catch (UsuariosApiException exception)
        {
            return Error(exception.Message, exception.Estado);
        }
        finally
        {
            actual = null;
            nueva = null;
            confirmacion = null;
            token = null;
        }
    }

    private IActionResult Error(string mensaje, int statusCode = StatusCodes.Status400BadRequest)
    {
        ErrorCambio = mensaje;
        Response.StatusCode = statusCode;
        return Page();
    }

    private static bool CumplePolitica(string? contrasena) =>
        !string.IsNullOrWhiteSpace(contrasena) &&
        contrasena.Length >= 8 &&
        contrasena.Any(char.IsUpper) &&
        contrasena.Any(char.IsLower) &&
        contrasena.Any(char.IsDigit) &&
        contrasena.Any(c => !char.IsLetterOrDigit(c));
}
