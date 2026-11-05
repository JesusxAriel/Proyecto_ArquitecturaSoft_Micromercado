using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
namespace PuntoMarket.Ventas.Frontend.Security;
// Protección de reenvíos en el frontend; no sustituye la unicidad del correo en Usuarios.
public sealed class ControlAltaUsuarios(IMemoryCache cache)
{
    private readonly object _candado=new();
    private sealed class Solicitud(string sesion) { public string Sesion {get;}=sesion; public bool Usada {get;set;} }
    private static string Correo(string sesion,string correo)=>"alta_correo:"+Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sesion+"|"+correo.Trim().ToLowerInvariant())));
    public string Nueva(string sesion,DateTimeOffset vence) { var id=Convert.ToHexString(RandomNumberGenerator.GetBytes(32));cache.Set("alta_solicitud:"+id,new Solicitud(sesion),vence);return id; }
    public bool Iniciar(string sesion,string? solicitud,string correo,DateTimeOffset vence)
    {
        lock(_candado) {
            if(solicitud is null||!cache.TryGetValue<Solicitud>("alta_solicitud:"+solicitud,out var s)||s is null||s.Sesion!=sesion||s.Usada) return false;
            s.Usada=true;
            var clave=Correo(sesion,correo);
            if(cache.TryGetValue(clave,out _)) return false;
            cache.Set(clave,true,vence);return true;
        }
    }
    public void PermitirCorreccion(string sesion,string correo) { lock(_candado) cache.Remove(Correo(sesion,correo)); }
}
