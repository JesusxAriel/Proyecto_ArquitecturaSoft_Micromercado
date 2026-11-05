using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;

namespace PuntoMarket.Ventas.Frontend.Security;

// El navegador recibe una referencia aleatoria; el token del correo solo queda en memoria.
public sealed class ActivacionesPendientes(IMemoryCache cache, TimeProvider reloj)
{
    public string Guardar(string token)
    {
        var id = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        cache.Set("activacion:" + id, token, reloj.GetUtcNow().AddMinutes(15));
        return id;
    }
    public string? Obtener(string? id) => id is { Length: 64 } ? cache.Get<string>("activacion:" + id) : null;
    public void Eliminar(string? id) { if (id is { Length: 64 }) cache.Remove("activacion:" + id); }
}
