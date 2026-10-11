using Proyecto_Arquitectura_Micromercado.Application.Security;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Web;

// Implementacion en memoria del puerto IUsuarioActual, para pruebas y para los motores
// de persistencia en memoria: devuelve el usuario con el que se construyo.
public sealed class UsuarioActualEnMemoria(
    int id = 1,
    string nombreUsuario = "admin",
    string rol = "Administrador") : IUsuarioActual
{
    public int Id { get; } = id;

    public string NombreUsuario { get; } = nombreUsuario;

    public string Rol { get; } = rol;
}
