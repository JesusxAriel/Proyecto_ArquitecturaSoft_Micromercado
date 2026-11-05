using System.ComponentModel.DataAnnotations;

namespace PuntoMarket.Usuarios.Api.Contracts.Autenticacion;

public sealed record IniciarSesionSolicitud
{
    [Required]
    [EmailAddress]
    public string Correo { get; init; } = string.Empty;

    [Required]
    public string Contrasena { get; init; } = string.Empty;
}
