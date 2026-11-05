using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PuntoMarket.Ventas.Frontend.Clients;
using PuntoMarket.Ventas.Frontend.Security;
namespace PuntoMarket.Ventas.Frontend.Pages.Account;

[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CambiarPasswordModel(IUsuariosCuentaApiClient api, SesionesUsuario sesiones) : PageModel
{
    public const string MensajeExito = "Su contraseña fue actualizada. Inicie sesión con su nueva contraseña.";

    public sealed class EntradaCambio
    {
        [Required(ErrorMessage = "Ingrese su contraseña actual.")]
        [DataType(DataType.Password)] public string ContrasenaActual { get; set; } = "";
        [Required(ErrorMessage = "Ingrese la nueva contraseña.")]
        [DataType(DataType.Password)] public string NuevaContrasena { get; set; } = "";
        [Required(ErrorMessage = "Confirme la nueva contraseña.")]
        [DataType(DataType.Password)] public string ConfirmarContrasena { get; set; } = "";
    }

    [BindProperty] public EntradaCambio Input { get; set; } = new();
    public string? ErrorCambio { get; private set; }
    public bool EsObligatorio => User.FindFirst("debeCambiarPassword")?.Value == bool.TrueString.ToLowerInvariant();
    public string Titulo => EsObligatorio ? "Cambio de contraseña obligatorio" : "Cambiar contraseña";

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var actual = Input.ContrasenaActual;
        var nueva = Input.NuevaContrasena;
        var confirmacion = Input.ConfirmarContrasena;
        LimpiarContrasenas();
        if (!ValidarEntrada(actual, nueva, confirmacion)) { Response.StatusCode = 400; return Page(); }
        try
        {
            await api.CambiarPassword(actual, nueva, ct);
        }
        catch (UsuariosCuentaException error) when (error.Estado == 401)
        {
            await CerrarSesion();
            return RedirectToPage("/Account/Login");
        }
        catch (UsuariosCuentaException error)
        {
            ErrorCambio = error.Message;
            Response.StatusCode = error.Estado;
            return Page();
        }

        await CerrarSesion();
        TempData[MensajeInformativoLogin.Clave] = MensajeExito;
        return RedirectToPage("/Account/Login");
    }

    private bool ValidarEntrada(string actual, string nueva, string confirmacion)
    {
        if (string.IsNullOrEmpty(actual)) ModelState.AddModelError("Input.ContrasenaActual", "Ingrese su contraseña actual.");
        var incumplimiento = PoliticaDePassword.PrimerIncumplimiento(nueva);
        if (incumplimiento is not null) ModelState.AddModelError("Input.NuevaContrasena", incumplimiento);
        if (string.IsNullOrEmpty(confirmacion)) ModelState.AddModelError("Input.ConfirmarContrasena", "Confirme la nueva contraseña.");
        else if (!string.Equals(nueva, confirmacion, StringComparison.Ordinal)) ModelState.AddModelError("Input.ConfirmarContrasena", "La nueva contraseña y su confirmación no coinciden.");
        if (!string.IsNullOrEmpty(actual) && string.Equals(actual, nueva, StringComparison.Ordinal)) ModelState.AddModelError("Input.NuevaContrasena", "La nueva contraseña no puede ser igual a la contraseña actual.");
        return ModelState.ErrorCount == 0;
    }

    private void LimpiarContrasenas()
    {
        ModelState.Clear();
        Input = new EntradaCambio();
    }

    private async Task CerrarSesion()
    {
        sesiones.Cerrar(User.FindFirst(AccesoUsuariosPreparado.ClaimSesion)?.Value);
        await HttpContext.SignOutAsync(AccesoUsuariosPreparado.Esquema);
    }
}
