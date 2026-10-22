namespace Proyecto_Arquitectura_Micromercado.Application.Security;

public sealed record CredencialesDeUsuario(
    int Id,
    string NombreUsuario,
    string NombreCompleto,
    string Rol,
    string HashContrasena,
    bool EstaActivo,
    string Estado,              // <-- Agregado
    bool DebeCambiarPassword,
    string Correo = "");

public sealed record UsuarioAutenticado(
    int Id,
    string NombreUsuario,
    string NombreCompleto,
    string Rol,
    bool DebeCambiarPassword); // <-- Agregado para que coincida con el AutenticacionService