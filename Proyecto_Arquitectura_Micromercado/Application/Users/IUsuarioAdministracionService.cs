namespace Proyecto_Arquitectura_Micromercado.Application.Users;

public interface IUsuarioAdministracionService
{
    Task<IReadOnlyList<UsuarioListadoDto>> ListarAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RolActivoDto>> ListarRolesActivosAsync(CancellationToken cancellationToken = default);
    Task<UsuarioCreadoDto> CrearAsync(UsuarioCreacionDto usuario, CancellationToken cancellationToken = default);
    Task<bool> ReenviarActivacionAsync(int usuarioId, CancellationToken cancellationToken = default);
    Task<bool> DesactivarAsync(int usuarioId, CancellationToken cancellationToken = default);
}
