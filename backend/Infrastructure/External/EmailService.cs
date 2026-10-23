using FacturacionApi.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace FacturacionApi.Infrastructure.External;

public sealed class EmailService(ILogger<EmailService> logger) : IEmailService
{
    public Task EnviarCorreoAsync(
        string destinatario,
        string asunto,
        string mensaje,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        logger.LogInformation(
            "Correo simulado para {Destinatario}. Asunto: {Asunto}. Mensaje: {Mensaje}",
            destinatario,
            asunto,
            mensaje);
        return Task.CompletedTask;
    }
}
