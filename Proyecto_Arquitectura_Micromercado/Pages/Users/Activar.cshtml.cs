using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Arquitectura_Micromercado.Application.Security;

namespace Proyecto_Arquitectura_Micromercado.Pages.Users;

[AllowAnonymous]
public sealed class ActivarModel(IActivacionUsuarioService activacion) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Token { get; set; }

    [BindProperty]
    public ActivarInput Input { get; set; } = new();

    public bool TokenInvalido { get; private set; }
    public bool ActivacionCompleta { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        EvitarCacheYReferencias();
        TokenInvalido = !await activacion.TokenValidoAsync(Token ?? string.Empty, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        EvitarCacheYReferencias();
        if (!await activacion.TokenValidoAsync(Token ?? string.Empty, cancellationToken))
        {
            TokenInvalido = true;
            return Page();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (Input.Contrasena != Input.ConfirmarContrasena)
        {
            ModelState.AddModelError("Input.ConfirmarContrasena", "Las contraseñas no coinciden.");
            return Page();
        }

        if (!await activacion.ActivarAsync(Token ?? string.Empty, Input.Contrasena, cancellationToken))
        {
            TokenInvalido = true;
            return Page();
        }

        ActivacionCompleta = true;
        return Page();
    }

    private void EvitarCacheYReferencias()
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers["Referrer-Policy"] = "no-referrer";
    }

    public sealed class ActivarInput
    {
        [Required(ErrorMessage = "Ingrese una contraseña.")]
        [DataType(DataType.Password)]
        [PasswordSegura]
        [Display(Name = "Nueva contraseña")]
        public string Contrasena { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirme la contraseña.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Contrasena), ErrorMessage = "Las contraseñas no coinciden.")]
        [Display(Name = "Confirmar contraseña")]
        public string ConfirmarContrasena { get; set; } = string.Empty;
    }
}
