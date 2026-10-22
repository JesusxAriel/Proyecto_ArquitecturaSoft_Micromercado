using Microsoft.Extensions.Options;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Email;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class SmtpEmailSenderTests
{
    [Fact]
    public async Task Rechaza_configuracion_smtp_incompleta()
    {
        var sender = new SmtpEmailSender(Options.Create(new SmtpEmailOptions()));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.EnviarAsync("persona@ejemplo.com", "Asunto", "<p>Mensaje</p>"));

        Assert.Contains("Host", error.Message);
    }

    [Fact]
    public async Task Rechaza_credenciales_smtp_incompletas()
    {
        var sender = new SmtpEmailSender(Options.Create(new SmtpEmailOptions
        {
            Host = "smtp.ejemplo.com",
            FromAddress = "tienda@ejemplo.com",
            UserName = "usuario"
        }));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.EnviarAsync("persona@ejemplo.com", "Asunto", "<p>Mensaje</p>"));

        Assert.Contains("UserName y Password", error.Message);
    }
}
