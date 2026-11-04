using PuntoMarket.Usuarios.Domain.Enums;

namespace PuntoMarket.Usuarios.Application.Ports.Autenticacion;

public sealed record UsuarioAutenticacionDto(
    int Id,
    string Username,
    string PasswordHash,
    string NombreCompleto,
    EstadoUsuario Estado,
    bool DebeCambiarPassword,
    string Rol,
    bool RolActivo);
