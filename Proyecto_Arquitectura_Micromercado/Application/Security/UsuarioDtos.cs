namespace Proyecto_Arquitectura_Micromercado.Application.Security;

// Lo que el login necesita saber de un usuario guardado, incluido el hash de su
// contrasena. Nunca sale de Application hacia la UI: la Page solo recibe
// UsuarioAutenticado.
public sealed record CredencialesDeUsuario(
    int Id,
    string NombreUsuario,
    string NombreCompleto,
    string Rol,
    string HashContrasena,
    bool EstaActivo);

// Resultado de un login correcto: los datos que la UI puede conservar en la cookie y en
// la sesion. No contiene la contrasena ni su hash.
public sealed record UsuarioAutenticado(
    int Id,
    string NombreUsuario,
    string NombreCompleto,
    string Rol);
