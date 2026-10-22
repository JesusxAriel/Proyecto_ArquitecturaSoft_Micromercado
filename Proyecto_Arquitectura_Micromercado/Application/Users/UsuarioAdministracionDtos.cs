namespace Proyecto_Arquitectura_Micromercado.Application.Users;

public sealed record UsuarioCreacionDto(string Nombre, string Apellido, string Correo, int IdRol);

public sealed record UsuarioListadoDto(
    int Id, string Nombre, string Apellido, string Correo, string Username,
    string Rol, string Estado, DateTime FechaCreacion);

public sealed record RolActivoDto(int Id, string Nombre);

public sealed record UsuarioCreadoDto(int Id);

public sealed record UsuarioAltaInterna(
    string Nombre, string Apellido, string Correo, string Username,
    int IdRol, string HashContrasena, int IdUsuarioCreacion,
    string TokenActivacionHash, DateTime TokenActivacionExpira);
