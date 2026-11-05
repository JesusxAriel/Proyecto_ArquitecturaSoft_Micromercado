using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Arquitectura_Micromercado.Dtos.Usuarios;
using Proyecto_Arquitectura_Micromercado.Servicios;

namespace Proyecto_Arquitectura_Micromercado.Pages.Users;

[AllowAnonymous]
public sealed class ActivarModel(UsuariosApiClient usuariosApi) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Token { get; set; }

    [BindProperty]
    public ActivarInput Input { get; set; } = new();

    public bool TokenInvalido { get; private set; }
    public bool ActivacionCompleta { get; private set; }

    public void OnGet()
    {
        PrepararRespuesta();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        PrepararRespuesta();
        if (TokenInvalido || !ModelState.IsValid)
            return Page();

        try
        {
            var resultado = await usuariosApi.ActivarAsync(new ActivarCuentaDto(
                Token!, Input.Contrasena, Input.ConfirmarContrasena), cancellationToken);
            ActivacionCompleta = resultado.Activada;
            if (!ActivacionCompleta)
                ModelState.AddModelError(string.Empty, "No se pudo activar la cuenta. Solicite un nuevo enlace al administrador.");
        }
        catch (HttpRequestException ex)
        {
            ModelState.AddModelError(string.Empty, ex.StatusCode.HasValue
                ? ex.Message : "No se pudo conectar con el servicio de usuarios. Intente nuevamente.");
        }
        catch (JsonException)
        {
            ModelState.AddModelError(string.Empty, "El servicio de usuarios devolvió una respuesta inválida.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            ModelState.AddModelError(string.Empty, "El servicio de usuarios tardó demasiado en responder. Intente nuevamente.");
        }

        return Page();
    }

    private void PrepararRespuesta()
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        TokenInvalido = string.IsNullOrWhiteSpace(Token);
    }

    public sealed class ActivarInput
    {
        [Required(ErrorMessage = "Ingrese una contraseña.")]
        [DataType(DataType.Password)]
        [Display(Name = "Nueva contraseña")]
        public string Contrasena { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirme la contraseña.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Contrasena), ErrorMessage = "Las contraseñas no coinciden.")]
        [Display(Name = "Confirmar contraseña")]
        public string ConfirmarContrasena { get; set; } = string.Empty;
    }
}
