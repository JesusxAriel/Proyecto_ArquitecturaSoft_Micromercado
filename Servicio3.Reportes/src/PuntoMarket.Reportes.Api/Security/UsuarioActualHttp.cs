using System.Security.Claims;
using PuntoMarket.Reportes.Application.Ports.Comunes;

namespace PuntoMarket.Reportes.Api.Security;

public class UsuarioActualHttp : IUsuarioActual
{
    private const string ClaimDeNombre = "unique_name";

    private readonly IHttpContextAccessor _accesor;

    public UsuarioActualHttp(IHttpContextAccessor accesor)
    {
        _accesor = accesor;
    }

    public int Id => int.Parse(ObtenerClaim("sub"));

    public string NombreDeUsuario => ObtenerClaim(ClaimDeNombre);

    private string ObtenerClaim(string tipo)
    {
        var usuario = _accesor.HttpContext?.User;
        if (usuario?.Identity?.IsAuthenticated != true)
            throw new InvalidOperationException("No hay un usuario autenticado en la solicitud actual.");

        return BuscarClaim(usuario, tipo)
            ?? throw new InvalidOperationException($"El token no contiene el claim '{tipo}'.");
    }

    private static string? BuscarClaim(ClaimsPrincipal usuario, string tipo) =>
        usuario.FindFirst(tipo)?.Value;
}
