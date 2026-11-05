using System.ComponentModel.DataAnnotations;

namespace PuntoMarket.Usuarios.Api.Contracts.Autenticacion;

public sealed record CambiarPasswordSolicitud
{
    [Required]
    public string ContrasenaActual { get; init; } = string.Empty;

    [Required]
    public string NuevaContrasena { get; init; } = string.Empty;
}
