using System.Text;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Security;

public sealed class JwtValidacionOpciones
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;

    public void Validar()
    {
        ValidarIdentificador(Issuer, "Jwt:Issuer");
        ValidarIdentificador(Audience, "Jwt:Audience");

        var clave = Encoding.UTF8.GetBytes(Key ?? string.Empty);
        if (clave.Length < 32)
            throw new InvalidOperationException("Jwt:Key debe contener al menos 32 bytes UTF-8.");

        var claveNormalizada = Encoding.UTF8.GetString(clave).Trim();
        var valoresDeEjemplo = new[]
        {
            "change_me",
            "changeme",
            "your-secret",
            "replace-me",
            "development-key",
            "secret"
        };
        if (valoresDeEjemplo.Any(valor =>
                claveNormalizada.Contains(valor, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                "Jwt:Key debe ser un secreto aleatorio y no un valor de ejemplo.");
        }
    }

    private static void ValidarIdentificador(string? valor, string claveConfiguracion)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new InvalidOperationException($"Configure {claveConfiguracion}.");
        if (!string.Equals(valor, valor.Trim(), StringComparison.Ordinal) ||
            valor.Any(char.IsControl))
        {
            throw new InvalidOperationException(
                $"{claveConfiguracion} no puede tener espacios externos ni caracteres de control.");
        }
    }
}
