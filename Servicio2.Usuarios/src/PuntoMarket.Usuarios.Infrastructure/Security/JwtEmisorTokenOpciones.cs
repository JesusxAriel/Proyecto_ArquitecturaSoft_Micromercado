using System.Text;

namespace PuntoMarket.Usuarios.Infrastructure.Security;

public sealed class JwtEmisorTokenOpciones
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public int ExpireMinutes { get; set; } = 60;

    public void Validar()
    {
        if (string.IsNullOrWhiteSpace(Issuer))
            throw new InvalidOperationException("Configure Jwt:Issuer.");
        if (string.IsNullOrWhiteSpace(Audience))
            throw new InvalidOperationException("Configure Jwt:Audience.");
        if (ExpireMinutes <= 0)
            throw new InvalidOperationException("Jwt:ExpireMinutes debe ser mayor que cero.");

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
                claveNormalizada.Contains(valor, StringComparison.OrdinalIgnoreCase)) ||
            claveNormalizada.Distinct().Count() < 12)
        {
            throw new InvalidOperationException("Jwt:Key debe ser un secreto aleatorio y no un valor de ejemplo.");
        }
    }
}
