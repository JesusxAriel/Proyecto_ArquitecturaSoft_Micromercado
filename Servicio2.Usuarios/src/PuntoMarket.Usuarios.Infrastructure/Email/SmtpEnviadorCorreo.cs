using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text;
using PuntoMarket.Usuarios.Application.Ports.Gestion;

namespace PuntoMarket.Usuarios.Infrastructure.Email;

public sealed class SmtpEnviadorCorreo(SmtpEnviadorCorreoOpciones opciones) : IEnviadorCorreo
{
    public async Task EnviarActivacionAsync(
        string correo,
        string token,
        DateTimeOffset expira,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(correo);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        ArgumentNullException.ThrowIfNull(opciones);

        if (string.IsNullOrWhiteSpace(opciones.Host)
            || string.IsNullOrWhiteSpace(opciones.FromAddress)
            || opciones.Port is < 1 or > 65535)
        {
            throw new InvalidOperationException(
                "La configuración SMTP requiere Host, FromAddress y un Port válido.");
        }

        var usaUsuario = !string.IsNullOrWhiteSpace(opciones.UserName);
        var usaContrasena = !string.IsNullOrWhiteSpace(opciones.Password);
        if (usaUsuario != usaContrasena)
        {
            throw new InvalidOperationException(
                "La configuración SMTP debe incluir UserName y Password juntos.");
        }

        if (string.IsNullOrWhiteSpace(opciones.BaseUrl)
            || !Uri.TryCreate(opciones.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute, out var origen)
            || origen.Scheme is not ("http" or "https")
            || !string.IsNullOrEmpty(origen.UserInfo)
            || !string.IsNullOrEmpty(origen.Query)
            || !string.IsNullOrEmpty(origen.Fragment))
        {
            throw new InvalidOperationException(
                "Configure BaseUrl con la URL pública HTTP/HTTPS del frontend, sin credenciales, query ni fragmento.");
        }

        var enlace = new Uri(origen, $"Users/Activar?token={Uri.EscapeDataString(token)}").AbsoluteUri;
        var enlaceHtml = WebUtility.HtmlEncode(enlace);
        var vencimiento = expira.ToUniversalTime().ToString("dd/MM/yyyy HH:mm 'UTC'", CultureInfo.InvariantCulture);

        using var mensaje = new MailMessage
        {
            From = new MailAddress(opciones.FromAddress, opciones.FromName),
            Subject = "Activa tu cuenta de PuntoMarket",
            SubjectEncoding = Encoding.UTF8,
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = true,
            Body = $"""
                <p>Se creó una cuenta para usted en PuntoMarket.</p>
                <p>Para comenzar a utilizarla, debe definir su contraseña y activar la cuenta.</p>
                <p><a href="{enlaceHtml}">Definir contraseña y activar cuenta</a></p>
                <p>Este enlace tiene una vigencia limitada y vence el {vencimiento}.</p>
                <p>Si no esperaba este mensaje, puede ignorarlo.</p>
                """
        };
        mensaje.To.Add(new MailAddress(correo));

        using var cliente = new SmtpClient(opciones.Host, opciones.Port)
        {
            EnableSsl = opciones.EnableSsl,
            UseDefaultCredentials = !usaUsuario
        };
        if (usaUsuario)
        {
            cliente.Credentials = new NetworkCredential(opciones.UserName, opciones.Password);
        }

        await cliente.SendMailAsync(mensaje, cancellationToken);
    }
}
