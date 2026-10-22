namespace Proyecto_Arquitectura_Micromercado.Application.Users;

public interface IEmailSender
{
    Task EnviarAsync(
        string destinatario,
        string asunto,
        string cuerpoHtml,
        CancellationToken cancellationToken = default);
}
