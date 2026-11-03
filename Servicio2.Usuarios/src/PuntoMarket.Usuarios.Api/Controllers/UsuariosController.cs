using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PuntoMarket.Usuarios.Api.Contracts.Gestion;
using PuntoMarket.Usuarios.Api.Filters;
using PuntoMarket.Usuarios.Application.Ports.Gestion;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.CrearUsuario;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.DarDeBajaUsuario;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.ListarUsuarios;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.ReenviarActivacion;

namespace PuntoMarket.Usuarios.Api.Controllers;

[ApiController]
[Route("api/usuarios")]
[Authorize(Roles = "Administrador")]
[GestionExceptionFilter(StatusCodes.Status409Conflict)]
public sealed class UsuariosController(
    ListarUsuariosCasoDeUso listarUsuarios,
    CrearUsuarioCasoDeUso crearUsuario,
    DarDeBajaUsuarioCasoDeUso darDeBajaUsuario,
    ReenviarActivacionCasoDeUso reenviarActivacion,
    IUsuarioGestionRepositorio repositorio) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ListarUsuariosRespuesta>> Listar(CancellationToken cancellationToken)
    {
        var salida = await listarUsuarios.EjecutarAsync(new ListarUsuariosEntrada(), cancellationToken);
        var usuarios = salida.Usuarios.Select(usuario => new UsuarioListadoRespuesta(
            usuario.Id, usuario.Nombre, usuario.Apellido, usuario.Correo, usuario.Username,
            usuario.Rol, usuario.Estado, usuario.FechaCreacion, usuario.TokenActivacionExpira,
            usuario.FechaActivacion, usuario.EstaExpirado)).ToArray();

        return Ok(new ListarUsuariosRespuesta(usuarios));
    }

    [HttpGet("roles")]
    public async Task<ActionResult<IReadOnlyList<RolActivoRespuesta>>> ListarRoles(
        CancellationToken cancellationToken)
    {
        var roles = await repositorio.ListarRolesActivosAsync(cancellationToken);
        return Ok(roles.Select(rol => new RolActivoRespuesta(rol.Id, rol.Nombre)).ToArray());
    }

    [HttpPost]
    [ProducesResponseType<CrearUsuarioRespuesta>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CrearUsuarioRespuesta>> Crear(
        [FromBody] CrearUsuarioSolicitud solicitud,
        CancellationToken cancellationToken)
    {
        var salida = await crearUsuario.EjecutarAsync(new CrearUsuarioEntrada(
            solicitud.Nombre, solicitud.Apellido, solicitud.Correo, solicitud.IdRol), cancellationToken);

        return CreatedAtAction(nameof(Listar), new CrearUsuarioRespuesta(salida.Id));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DarDeBaja(int id, CancellationToken cancellationToken)
    {
        await darDeBajaUsuario.EjecutarAsync(new DarDeBajaUsuarioEntrada(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/reenviar-activacion")]
    public async Task<ActionResult<ReenviarActivacionRespuesta>> ReenviarActivacion(
        int id,
        CancellationToken cancellationToken)
    {
        var salida = await reenviarActivacion.EjecutarAsync(
            new ReenviarActivacionEntrada(id), cancellationToken);
        return Ok(new ReenviarActivacionRespuesta(salida.Reenviado));
    }
}
