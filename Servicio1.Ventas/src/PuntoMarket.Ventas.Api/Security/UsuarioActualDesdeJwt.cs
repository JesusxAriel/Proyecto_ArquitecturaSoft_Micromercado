using System.Globalization;
using System.Security.Claims;
using PuntoMarket.Ventas.Application.Security;

namespace PuntoMarket.Ventas.Api.Security;

public sealed class UsuarioActualDesdeJwt(IHttpContextAccessor accessor) : IUsuarioActual
{
    private ClaimsPrincipal Usuario => accessor.HttpContext?.User is { Identity.IsAuthenticated: true } usuario
        ? usuario : throw new InvalidOperationException("No hay una identidad autenticada.");

    public int Id => int.TryParse(Usuario.FindFirst("sub")?.Value, NumberStyles.None,
        CultureInfo.InvariantCulture, out var id) && id > 0
        ? id : throw new InvalidOperationException("Identificador de identidad inválido.");
    public string NombreUsuario => Usuario.FindFirst("unique_name")?.Value
        ?? throw new InvalidOperationException("Falta el nombre de usuario autenticado.");
    public string Rol => Usuario.FindFirst("role")?.Value
        ?? throw new InvalidOperationException("Falta el rol autenticado.");
}
