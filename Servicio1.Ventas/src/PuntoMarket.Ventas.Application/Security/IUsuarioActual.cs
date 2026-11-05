namespace PuntoMarket.Ventas.Application.Security;

/// <summary>Identidad de auditoría independiente de HTTP, cookies y del mecanismo de autenticación.</summary>
public interface IUsuarioActual
{
    int Id { get; }

    string NombreUsuario { get; }

    string Rol { get; }
}
