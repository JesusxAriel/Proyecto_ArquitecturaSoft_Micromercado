using Microsoft.AspNetCore.Authentication;
namespace PuntoMarket.Ventas.Frontend.Security;

// Scoped. Solo acepta la identidad autenticada por la cookie de Usuarios.
public sealed class ResolucionSesion(IHttpContextAccessor contexto, SesionesUsuario reales)
{
    private string? _id; private bool _resuelta; private bool _invalidada;
    public string? ObtenerToken()
    {
        var c = contexto.HttpContext;
        if (c is null || _invalidada) return null;
        if (!_resuelta)
        {
            _resuelta = true;
            var p = c.User;
            if (p.Identities.Count(i => i.IsAuthenticated) != 1 ||
                p.Identity?.AuthenticationType != AccesoUsuariosPreparado.Esquema ||
                p.HasClaim(x => x.Type == "demo_session")) return null;
            var ids = p.FindAll(AccesoUsuariosPreparado.ClaimSesion).ToArray();
            if (ids.Length != 1) return null;
            _id = ids[0].Value;
        }
        return reales.Token(_id);
    }
    public async Task InvalidarAsync()
    {
        _invalidada = true;
        if (_id is not null) reales.Cerrar(_id);
        var c = contexto.HttpContext;
        if (c is null) return;
        c.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity());
        if (!c.Response.HasStarted) {
            c.Response.Headers["X-PuntoMarket-Login"] = "/Account/Login";
            if (c.RequestServices?.GetService<IAuthenticationService>() is not null) await c.SignOutAsync(AccesoUsuariosPreparado.Esquema);
        }
    }
}
