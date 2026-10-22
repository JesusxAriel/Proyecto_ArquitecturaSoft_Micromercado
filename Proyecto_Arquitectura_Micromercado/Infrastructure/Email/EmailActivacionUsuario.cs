using System.Net;
using Microsoft.Extensions.Options;
using Proyecto_Arquitectura_Micromercado.Application.Users;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Email;

public sealed class EmailActivacionUsuario(
    IEmailSender emailSender,
    IOptions<ActivacionEmailOptions> options) : IEmailActivacionUsuario
{
    public Task EnviarEnlaceAsync(
        string correo,
        string token,
        DateTimeOffset expira,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = options.Value.BaseUrl.TrimEnd('/') + "/";
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var origen)
            || origen.Scheme is not ("http" or "https")
            || !string.IsNullOrEmpty(origen.UserInfo)
            || !string.IsNullOrEmpty(origen.Query)
            || !string.IsNullOrEmpty(origen.Fragment))
        {
            throw new InvalidOperationException(
                "Configure Activacion:BaseUrl con la URL pública HTTP/HTTPS de la aplicación.");
        }

        var enlace = new Uri(origen, $"Users/Activar?token={Uri.EscapeDataString(token)}").ToString();
        var enlaceHtml = WebUtility.HtmlEncode(enlace);
        var vencimiento = WebUtility.HtmlEncode(expira.ToString("dd/MM/yyyy HH:mm 'UTC'"));
        var cuerpo = $"""
            <p>Se creó una cuenta para ti en Micromercado.</p>
            <p><a href="{enlaceHtml}">Definir contraseña y activar cuenta</a></p>
            <p>Este enlace vence en 10 minutos (hasta {vencimiento}). Si no esperabas este mensaje, puedes ignorarlo.</p>
            """;

        return emailSender.EnviarAsync(
            correo,
            "Activa tu cuenta de Micromercado",
            cuerpo,
            cancellationToken);
    }
}
