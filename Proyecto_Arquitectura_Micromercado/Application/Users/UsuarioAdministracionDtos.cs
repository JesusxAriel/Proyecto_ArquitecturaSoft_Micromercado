namespace Proyecto_Arquitectura_Micromercado.Application.Users;

// Solo datos proporcionados por el administrador. Las reglas se validan en el servicio.
public sealed record UsuarioCreacionDto(string Nombre, string Apellido, string Correo, int IdRol);

public sealed record UsuarioListadoDto(
    int Id, string Nombre, string Apellido, string Correo, string Username,
    string Rol, string Estado, DateTime FechaCreacion);

public sealed record RolActivoDto(int Id, string Nombre);

// Puerto interno de escritura: nunca se utiliza como entrada de un formulario ni como respuesta.
public sealed record UsuarioAltaInterna(
    string Nombre, string Apellido, string Correo, string Username,
    int IdRol, string HashContrasena, int IdUsuarioCreacion);
