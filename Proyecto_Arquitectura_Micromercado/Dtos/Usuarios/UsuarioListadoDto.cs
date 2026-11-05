namespace Proyecto_Arquitectura_Micromercado.Dtos.Usuarios;

public sealed record UsuarioListadoDto(
    int Id,
    string Nombre,
    string Apellido,
    string Correo,
    string Username,
    string Rol,
    string Estado,
    DateTime FechaCreacion,
    DateTime? TokenActivacionExpira,
    DateTime? FechaActivacion,
    bool EstaExpirado);

public sealed record ListarUsuariosRespuesta(IReadOnlyList<UsuarioListadoDto> Usuarios);
