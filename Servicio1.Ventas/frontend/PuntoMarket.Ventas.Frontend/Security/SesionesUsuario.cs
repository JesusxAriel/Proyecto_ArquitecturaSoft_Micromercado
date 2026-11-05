using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;
namespace PuntoMarket.Ventas.Frontend.Security;

public sealed class SesionesUsuario(IMemoryCache cache, ValidadorTokenUsuarios validador, TimeProvider reloj)
{
    private sealed class Sesion(string jwt, IdentidadTokenUsuarios identidad)
    {
        public string Jwt { get; } = jwt;
        public IdentidadTokenUsuarios Identidad { get; } = identidad;
    }
    private static string Clave(string id) => "usuarios_sesion:" + id;
    public string Crear(string jwt)
    {
        var identidad = validador.Validar(jwt);
        if (identidad.VenceEn <= reloj.GetUtcNow()) throw new TokenUsuariosInvalidoException();
        var id = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        cache.Set(Clave(id), new Sesion(jwt, identidad), identidad.VenceEn);
        return id;
    }
    private Sesion? Obtener(string? id)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Length != 64 || !cache.TryGetValue<Sesion>(Clave(id), out var sesion) || sesion is null) return null;
        if (sesion.Identidad.VenceEn <= reloj.GetUtcNow()) { cache.Remove(Clave(id)); return null; }
        return sesion;
    }
    public bool Existe(string? id) => Obtener(id) is not null;
    public IdentidadTokenUsuarios? Identidad(string? id) => Obtener(id)?.Identidad;
    // Uso interno del servidor Razor. Nunca enlazar a JSON, cookies o modelos de página.
    public string? Token(string? id) => Obtener(id)?.Jwt;
    public void Cerrar(string? id) { if (id is not null) cache.Remove(Clave(id)); }
}
