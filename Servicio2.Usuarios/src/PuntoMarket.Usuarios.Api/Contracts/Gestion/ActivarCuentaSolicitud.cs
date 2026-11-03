namespace PuntoMarket.Usuarios.Api.Contracts.Gestion;

public sealed record ActivarCuentaSolicitud(
    string Token,
    string NuevaContrasena,
    string ConfirmarContrasena);
