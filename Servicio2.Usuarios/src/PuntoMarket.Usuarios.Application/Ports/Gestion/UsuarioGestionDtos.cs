using PuntoMarket.Usuarios.Domain.Enums;

namespace PuntoMarket.Usuarios.Application.Ports.Gestion;

public sealed record UsuarioListadoDto(
    int Id, string Nombre, string Apellido, string Correo, string Username,
    string Rol, string Estado, DateTime FechaCreacion,
    DateTime? TokenActivacionExpira = null, DateTime? FechaActivacion = null);

public sealed record RolActivoDto(int Id, string Nombre);

public sealed record UsuarioAltaInterna(
    string Nombre, string Apellido, string Correo, string Username,
    int IdRol, string HashContrasena, int IdUsuarioCreacion,
    string TokenActivacionHash, DateTime TokenActivacionExpira);

public sealed record UsuarioActivacionDto(
    int Id, string Correo, EstadoUsuario Estado,
    string? TokenActivacionHash, DateTime? TokenActivacionExpira,
    DateTime? FechaActivacion);
