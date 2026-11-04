namespace PuntoMarket.Usuarios.Application.UseCases.Autenticacion.IniciarSesion;

public sealed record IniciarSesionSalida(
    string Token,
    int UsuarioId,
    string Username,
    string NombreCompleto,
    string Rol,
    bool DebeCambiarPassword);
