using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using PuntoMarket.Ventas.Frontend.Security;
namespace PuntoMarket.Ventas.Tests.Frontend;
// Emisor exclusivamente de fixtures: no pertenece al frontend ni utiliza claves privadas.
internal static class SesionRealPrueba
{
    public static string Token(string clave,string rol="Cajero",TimeProvider? reloj=null) {
        var ahora=(reloj??TimeProvider.System).GetUtcNow(); var admin=rol=="Administrador";
        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken("PuntoMarket.DemoLocal","PuntoMarket.Ventas.DemoLocal",
            [new("sub",admin?"1":"2"),new("unique_name",admin?"admin":"cajero1"),new("role",rol),new("debeCambiarPassword","false")],
            ahora.AddSeconds(-1).UtcDateTime,ahora.AddMinutes(5).UtcDateTime,new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave)),SecurityAlgorithms.HmacSha256)));
    }
    public static SesionesUsuario Sesiones(IMemoryCache cache,string clave,TimeProvider? reloj=null)=>new(cache,new ValidadorTokenUsuarios(()=>clave,reloj??TimeProvider.System),reloj??TimeProvider.System);
    public static ClaimsPrincipal Principal(SesionesUsuario sesiones,string id) {
        var i=sesiones.Identidad(id)!;
        return new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier,i.Id.ToString()),new Claim(ClaimTypes.Name,i.Usuario),new Claim(ClaimTypes.Role,i.Rol),new Claim(AccesoUsuariosPreparado.ClaimSesion,id),new Claim("debeCambiarPassword","false")],AccesoUsuariosPreparado.Esquema));
    }
    public static DefaultHttpContext Contexto(SesionesUsuario sesiones,string id)=>new(){User=Principal(sesiones,id)};
}
