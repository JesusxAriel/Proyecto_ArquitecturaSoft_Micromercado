using PuntoMarket.Usuarios.Domain.Enums;

namespace PuntoMarket.Usuarios.Domain.Entities;

public sealed class Usuario
{
    public int Id { get; init; }
    public required string Username { get; init; }
    public required string PasswordHash { get; init; }
    public required string NombreCompleto { get; init; }
    public string? Nombre { get; init; }
    public string? Apellido { get; init; }
    public string? Correo { get; init; }
    public int IdRol { get; init; }
    public EstadoUsuario Estado { get; init; } = EstadoUsuario.ACTIVO;
    public bool EstaActivo => Estado == EstadoUsuario.ACTIVO;
    public bool DebeCambiarPassword { get; init; }
    public int? IdUsuarioCreacion { get; init; }
    public int? IdUsuarioActualizacion { get; init; }
    public string? TokenActivacionHash { get; init; }
    public DateTime? TokenActivacionExpira { get; init; }
    public DateTime? FechaActivacion { get; init; }
    public int? IdUsuarioEliminacion { get; init; }
    public DateTime? FechaEliminacion { get; init; }
    public DateTime FechaCreacion { get; init; }
    public DateTime? FechaActualizacion { get; init; }
}
