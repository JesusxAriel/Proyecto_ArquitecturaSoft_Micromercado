using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using PuntoMarket.Usuarios.Application.Ports.Autenticacion;
using PuntoMarket.Usuarios.Application.Ports.Comunes;

namespace PuntoMarket.Usuarios.Infrastructure.Security;

public sealed class JwtEmisorToken : IEmisorToken
{
    private readonly string _issuer;
    private readonly string _audience;
    private readonly byte[] _key;
    private readonly int _expireMinutes;
    private readonly IReloj _reloj;

    public JwtEmisorToken(JwtEmisorTokenOpciones opciones, IReloj reloj)
    {
        ArgumentNullException.ThrowIfNull(opciones);
        ArgumentNullException.ThrowIfNull(reloj);

        opciones.Validar();
        _issuer = opciones.Issuer;
        _audience = opciones.Audience;
        _key = Encoding.UTF8.GetBytes(opciones.Key);
        _expireMinutes = opciones.ExpireMinutes;
        _reloj = reloj;
    }

    public string Emitir(
        int usuarioId,
        string username,
        string nombreCompleto,
        string rol,
        bool debeCambiarPassword)
    {
        var ahora = _reloj.UtcNow.UtcDateTime;
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuarioId.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, username),
            new Claim("nombre", nombreCompleto),
            new Claim("role", rol),
            new Claim("debeCambiarPassword", debeCambiarPassword.ToString().ToLowerInvariant())
        };
        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(_key),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: ahora,
            expires: ahora.AddMinutes(_expireMinutes),
            signingCredentials: credenciales);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
