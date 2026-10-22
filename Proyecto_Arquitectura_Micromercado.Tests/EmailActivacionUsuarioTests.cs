using Microsoft.Extensions.Options;
using Proyecto_Arquitectura_Micromercado.Application.Users;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Email;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class EmailActivacionUsuarioTests
{
    [Fact]
    public async Task Envia_enlace_con_token_y_aviso_de_expiracion()
    {
        var sender = new SenderPrueba();
        var servicio = new EmailActivacionUsuario(
            sender,
            Options.Create(new ActivacionEmailOptions { BaseUrl = "https://tienda.ejemplo.com" }));
        var expira = new DateTimeOffset(2026, 10, 22, 4, 10, 0, TimeSpan.Zero);

        await servicio.EnviarEnlaceAsync("ana@ejemplo.com", "token+/&", expira);

        Assert.Equal("ana@ejemplo.com", sender.Destinatario);
        Assert.Equal("Active su cuenta de Micromercado", sender.Asunto);
        Assert.Contains("https://tienda.ejemplo.com/Users/Activar?token=token%2B%2F%26", sender.Cuerpo);
        Assert.Contains("vence en 10 minutos", sender.Cuerpo);
        Assert.Contains("22/10/2026 04:10 UTC", sender.Cuerpo);
    }

    [Fact]
    public async Task Rechaza_url_publica_no_configurada()
    {
        var sender = new SenderPrueba();
        var servicio = new EmailActivacionUsuario(sender, Options.Create(new ActivacionEmailOptions()));

        await Assert.ThrowsAsync<ErrorEnvioCorreoException>(
            () => servicio.EnviarEnlaceAsync("ana@ejemplo.com", "token", DateTimeOffset.UtcNow));

        Assert.Null(sender.Destinatario);
    }

    private sealed class SenderPrueba : IEmailSender
    {
        public string? Destinatario { get; private set; }
        public string? Asunto { get; private set; }
        public string? Cuerpo { get; private set; }

        public Task EnviarAsync(
            string destinatario,
            string asunto,
            string cuerpoHtml,
            CancellationToken cancellationToken = default)
        {
            Destinatario = destinatario;
            Asunto = asunto;
            Cuerpo = cuerpoHtml;
            return Task.CompletedTask;
        }
    }
}
