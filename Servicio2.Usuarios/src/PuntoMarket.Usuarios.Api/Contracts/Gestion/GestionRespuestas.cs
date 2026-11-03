namespace PuntoMarket.Usuarios.Api.Contracts.Gestion;

public sealed record ListarUsuariosRespuesta(IReadOnlyList<UsuarioListadoRespuesta> Usuarios);

public sealed record UsuarioListadoRespuesta(
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

public sealed record RolActivoRespuesta(int Id, string Nombre);

public sealed record CrearUsuarioRespuesta(int Id);

public sealed record ReenviarActivacionRespuesta(bool Reenviado);

public sealed record ActivarCuentaRespuesta(bool Activada);
