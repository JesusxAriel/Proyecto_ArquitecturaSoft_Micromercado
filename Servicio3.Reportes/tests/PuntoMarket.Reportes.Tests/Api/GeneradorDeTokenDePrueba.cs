using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace PuntoMarket.Reportes.Tests.Api;

public class GeneradorDeTokenDePrueba
{
    public const string Emisor = "emisor-de-prueba";
    public const string Audiencia = "audiencia-de-prueba";

    public string Clave { get; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    public string Generar(int id, string nombreDeUsuario, string? rol = null)
    {
        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Clave)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            Emisor,
            Audiencia,
            CrearClaims(id, nombreDeUsuario, rol),
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credenciales);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static List<Claim> CrearClaims(int id, string nombreDeUsuario, string? rol)
    {
        var claims = new List<Claim> { new("sub", id.ToString()), new("unique_name", nombreDeUsuario) };
        if (rol is not null)
            claims.Add(new Claim("role", rol));

        return claims;
    }
}
