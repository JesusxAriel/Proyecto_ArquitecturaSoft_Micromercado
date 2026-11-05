namespace PuntoMarket.Usuarios.Domain.Entities;

public sealed class Rol
{
    public int Id { get; init; }
    public required string Nombre { get; init; }
    public string? Descripcion { get; init; }
    public bool EstaActivo { get; init; } = true;
    public DateTime FechaCreacion { get; init; }
    public DateTime? FechaActualizacion { get; init; }
}
