using PuntoMarket.Ventas.Application.Security;

namespace PuntoMarket.Ventas.Tests;

public sealed class UsuarioActualEnMemoria(
    int id = 1,
    string nombreUsuario = "admin",
    string rol = "Administrador") : IUsuarioActual
{
    public int Id { get; } = id;

    public string NombreUsuario { get; } = nombreUsuario;

    public string Rol { get; } = rol;
}
