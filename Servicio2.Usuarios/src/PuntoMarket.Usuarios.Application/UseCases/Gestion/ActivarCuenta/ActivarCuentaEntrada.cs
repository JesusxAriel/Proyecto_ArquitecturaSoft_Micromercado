namespace PuntoMarket.Usuarios.Application.UseCases.Gestion.ActivarCuenta;

public sealed record ActivarCuentaEntrada(
    string Token,
    string NuevaContrasena,
    string ConfirmarContrasena);
