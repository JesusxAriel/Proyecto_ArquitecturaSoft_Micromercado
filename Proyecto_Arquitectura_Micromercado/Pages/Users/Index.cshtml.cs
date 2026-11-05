using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Arquitectura_Micromercado.Dtos.Usuarios;
using Proyecto_Arquitectura_Micromercado.Servicios;

namespace Proyecto_Arquitectura_Micromercado.Pages.Users;

[Authorize(Roles = "Administrador")]
public class IndexModel(UsuariosApiClient usuariosApi) : PageModel
{
    public IReadOnlyList<UsuarioListadoDto> Usuarios { get; private set; } = [];
    public IReadOnlyList<RolDto> RolesActivos { get; private set; } = [];

    [BindProperty]
    public UsuarioFormulario NuevoUsuario { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public int? UsuarioCreadoId { get; set; }

    public string? UsernameGenerado { get; private set; }
    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await CargarDatosAsync(cancellationToken);
        if (UsuarioCreadoId is int id)
            UsernameGenerado = Usuarios.FirstOrDefault(usuario => usuario.Id == id)?.Username;
    }

    public async Task<IActionResult> OnPostCreateAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await CargarDatosAsync(cancellationToken);
            return Page();
        }

        try
        {
            var creado = await usuariosApi.CrearAsync(new CrearUsuarioDto(
                NuevoUsuario.Nombre, NuevoUsuario.Apellido, NuevoUsuario.Correo,
                NuevoUsuario.IdRol!.Value), cancellationToken);
            UsuarioCreadoId = creado.Id;
            StatusMessage = "Usuario creado correctamente.";
            return RedirectToPage();
        }
        catch (Exception ex) when (EsErrorDeApi(ex, cancellationToken))
        {
            ModelState.AddModelError(string.Empty, MensajeDeError(ex) +
                " Actualice el listado antes de repetir el alta para comprobar si el usuario fue creado.");
            await CargarDatosAsync(cancellationToken);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostReenviarActivacionAsync(int usuarioId, CancellationToken cancellationToken)
    {
        try
        {
            var respuesta = await usuariosApi.ReenviarActivacionAsync(usuarioId, cancellationToken);
            if (!respuesta.Reenviado)
            {
                ErrorMessage = "No se reenvió la activación. El usuario ya no está pendiente o no existe.";
                await CargarDatosAsync(cancellationToken);
                return Page();
            }

            StatusMessage = "Se envió un nuevo enlace de activación.";
            return RedirectToPage();
        }
        catch (Exception ex) when (EsErrorDeApi(ex, cancellationToken))
        {
            ErrorMessage = MensajeDeError(ex);
            await CargarDatosAsync(cancellationToken);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDesactivarAsync(int usuarioId, CancellationToken cancellationToken)
    {
        try
        {
            await usuariosApi.DarDeBajaAsync(usuarioId, cancellationToken);
            StatusMessage = "Usuario dado de baja correctamente.";
            return RedirectToPage();
        }
        catch (Exception ex) when (EsErrorDeApi(ex, cancellationToken))
        {
            ErrorMessage = MensajeDeError(ex);
            await CargarDatosAsync(cancellationToken);
            return Page();
        }
    }

    private async Task CargarDatosAsync(CancellationToken cancellationToken)
    {
        try
        {
            Usuarios = await usuariosApi.ListarAsync(cancellationToken);
        }
        catch (Exception ex) when (EsErrorDeApi(ex, cancellationToken))
        {
            ErrorMessage = string.Join(" ", new[] { ErrorMessage, "No se pudo cargar el listado de usuarios.", MensajeDeError(ex) }
                .Where(mensaje => mensaje is not null));
        }

        try
        {
            RolesActivos = await usuariosApi.ListarRolesAsync(cancellationToken);
        }
        catch (Exception ex) when (EsErrorDeApi(ex, cancellationToken))
        {
            ErrorMessage = string.Join(" ", new[] { ErrorMessage, "No se pudieron cargar los roles activos.", MensajeDeError(ex) }
                .Where(mensaje => mensaje is not null));
        }
    }

    private static bool EsErrorDeApi(Exception ex, CancellationToken cancellationToken) =>
        ex is HttpRequestException or JsonException ||
        (ex is OperationCanceledException && !cancellationToken.IsCancellationRequested);

    private static string MensajeDeError(Exception ex) => ex switch
    {
        HttpRequestException { StatusCode: not null } => ex.Message,
        JsonException => "El servicio de usuarios devolvió una respuesta inválida.",
        OperationCanceledException => "El servicio de usuarios tardó demasiado en responder.",
        _ => "No se pudo conectar con el servicio de usuarios. Intente nuevamente."
    };

    public sealed class UsuarioFormulario
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        [Display(Name = "Apellido")]
        public string Apellido { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingrese un correo válido.")]
        [Display(Name = "Correo")]
        public string Correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Seleccione un rol.")]
        [Range(1, int.MaxValue, ErrorMessage = "Seleccione un rol válido.")]
        [Display(Name = "Rol")]
        public int? IdRol { get; set; }
    }
}
