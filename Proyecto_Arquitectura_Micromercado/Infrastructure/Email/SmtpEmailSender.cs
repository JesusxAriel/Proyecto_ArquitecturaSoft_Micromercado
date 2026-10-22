using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Proyecto_Arquitectura_Micromercado.Application.Users;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Email;

public sealed class SmtpEmailSender(IOptions<SmtpEmailOptions> options) : IEmailSender
{
    public async Task EnviarAsync(
        string destinatario,
        string asunto,
        string cuerpoHtml,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinatario);
        ArgumentException.ThrowIfNullOrWhiteSpace(asunto);
        ArgumentNullException.ThrowIfNull(cuerpoHtml);

        var configuracion = options.Value;
        if (string.IsNullOrWhiteSpace(configuracion.Host)
            || string.IsNullOrWhiteSpace(configuracion.FromAddress)
            || configuracion.Port is < 1 or > 65535)
        {
            throw new InvalidOperationException(
                "La configuración SMTP requiere Host, FromAddress y un Port válido.");
        }

        var usaUsuario = !string.IsNullOrWhiteSpace(configuracion.UserName);
        var usaContrasena = !string.IsNullOrWhiteSpace(configuracion.Password);
        if (usaUsuario != usaContrasena)
        {
            throw new InvalidOperationException(
                "La configuración SMTP debe incluir UserName y Password juntos.");
        }

        using var mensaje = new MailMessage
        {
            From = new MailAddress(configuracion.FromAddress, configuracion.FromName),
            Subject = asunto,
            Body = cuerpoHtml,
            IsBodyHtml = true
        };
        mensaje.To.Add(new MailAddress(destinatario));

        using var cliente = new SmtpClient(configuracion.Host, configuracion.Port)
        {
            EnableSsl = configuracion.EnableSsl,
            UseDefaultCredentials = !usaUsuario
        };
        if (usaUsuario)
        {
            cliente.Credentials = new NetworkCredential(configuracion.UserName, configuracion.Password);
        }

        await cliente.SendMailAsync(mensaje, cancellationToken);
    }
}
