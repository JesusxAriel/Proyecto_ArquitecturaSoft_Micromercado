namespace PuntoMarket.Usuarios.Application.UseCases.Gestion.ListarUsuarios;

public sealed record ListarUsuariosSalida(IReadOnlyList<UsuarioListadoSalida> Usuarios);

public sealed record UsuarioListadoSalida(
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
