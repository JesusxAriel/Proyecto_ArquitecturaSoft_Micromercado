using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Arquitectura_Micromercado.Application.Security;
using Proyecto_Arquitectura_Micromercado.Application.Users;

namespace Proyecto_Arquitectura_Micromercado.Pages.Users;

[Authorize(Roles = Roles.Administrador)]
public class IndexModel(IUsuarioAdministracionService servicio) : PageModel
{
    public IReadOnlyList<UsuarioListadoDto> Usuarios { get; private set; } = [];
    public IReadOnlyList<RolActivoDto> RolesActivos { get; private set; } = [];

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
            var usuarioCreado = await servicio.CrearAsync(new UsuarioCreacionDto(
                NuevoUsuario.Nombre, NuevoUsuario.Apellido, NuevoUsuario.Correo,
                NuevoUsuario.IdRol!.Value), cancellationToken);
            UsuarioCreadoId = usuarioCreado.Id;
        }
        catch (CorreoUsuarioDuplicadoException)
        {
            ModelState.AddModelError("NuevoUsuario.Correo", "El correo ya está registrado. Ingrese otro correo.");
        }
        catch (ErrorEnvioActivacionException)
        {
            ModelState.AddModelError(string.Empty,
                "El usuario quedó creado como PENDIENTE, pero no se pudo enviar el enlace. " +
                "Revise la configuración SMTP antes de volver a intentar el alta.");
        }
        catch (ArgumentException)
        {
            ModelState.AddModelError(string.Empty,
                "No se pudo crear el usuario. Revise el nombre, apellido y correo, y seleccione un rol activo.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not UnauthorizedAccessException)
        {
            ModelState.AddModelError(string.Empty, "No se pudo crear el usuario. Intente nuevamente.");
        }

        if (!ModelState.IsValid)
        {
            await CargarDatosAsync(cancellationToken);
            return Page();
        }

        StatusMessage = "Usuario creado correctamente.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostReenviarActivacionAsync(
        int usuarioId,
        CancellationToken cancellationToken)
    {
        try
        {
            StatusMessage = await servicio.ReenviarActivacionAsync(usuarioId, cancellationToken)
                ? "Se envió un nuevo enlace de activación. Es válido durante 10 minutos."
                : "El usuario ya no está pendiente de activación o no existe.";
        }
        catch (ErrorEnvioActivacionException)
        {
            ModelState.AddModelError(string.Empty,
                "No se pudo enviar el enlace. El token fue renovado; puedes volver a intentar el envío.");
            await CargarDatosAsync(cancellationToken);
            return Page();
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not UnauthorizedAccessException)
        {
            ModelState.AddModelError(string.Empty, "No se pudo reenviar el enlace. Intente nuevamente.");
            await CargarDatosAsync(cancellationToken);
            return Page();
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDesactivarAsync(
        int usuarioId,
        CancellationToken cancellationToken)
    {
        try
        {
            StatusMessage = await servicio.DesactivarAsync(usuarioId, cancellationToken)
                ? "Usuario desactivado correctamente."
                : "El usuario ya estaba inactivo o no existe.";
        }
        catch (NoPuedeDesactivarSuPropiaCuentaException)
        {
            ModelState.AddModelError(string.Empty, "No puedes desactivar tu propia cuenta.");
            await CargarDatosAsync(cancellationToken);
            return Page();
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not UnauthorizedAccessException)
        {
            ModelState.AddModelError(string.Empty, "No se pudo desactivar el usuario. Intente nuevamente.");
            await CargarDatosAsync(cancellationToken);
            return Page();
        }

        return RedirectToPage();
    }

    private async Task CargarDatosAsync(CancellationToken cancellationToken)
    {
        try
        {
            Usuarios = await servicio.ListarAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not UnauthorizedAccessException)
        {
            ErrorMessage = "No se pudo cargar el listado de usuarios. Recargue la página para intentar nuevamente.";
        }

        try
        {
            RolesActivos = await servicio.ListarRolesActivosAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not UnauthorizedAccessException)
        {
            ErrorMessage = string.Join(" ", new[] { ErrorMessage,
                "No se pudieron cargar los roles activos. Recargue la página para intentar nuevamente." }
                .Where(mensaje => mensaje is not null));
        }
    }

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
