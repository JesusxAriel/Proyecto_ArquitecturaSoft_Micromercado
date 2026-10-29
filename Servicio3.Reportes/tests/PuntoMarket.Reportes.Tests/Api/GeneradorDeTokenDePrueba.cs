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

    public string Generar(int id, string nombreDeUsuario)
    {
        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Clave)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            Emisor,
            Audiencia,
            [new Claim("sub", id.ToString()), new Claim("unique_name", nombreDeUsuario)],
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credenciales);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
