namespace Proyecto_Arquitectura_Micromercado.Application.Security;

public interface IUsuarioActual
{
    int Id { get; }

    string NombreUsuario { get; }

    string Rol { get; }
}
