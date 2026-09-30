using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SistemaAlmacen.Business.Interfaces;
using SistemaAlmacen.Business.Models.Email;

namespace SistemaAlmacen.Business.Services;

/// <summary>
/// Implementación de <see cref="IEmailSender"/> basada en SMTP (System.Net.Mail).
/// En modo mock (Email:UseMock=true) no realiza el envío real; registra el contenido
/// del correo en el log, lo que permite probar el flujo sin un servidor SMTP configurado.
/// </summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        return SendInternalAsync(to, subject, htmlBody, null, cancellationToken);
    }

    /// <inheritdoc />
    public Task SendEmailWithAttachmentAsync(
        string to,
        string subject,
        string htmlBody,
        byte[] attachmentContent,
        string attachmentFileName,
        string attachmentContentType,
        CancellationToken cancellationToken = default)
    {
        if (attachmentContent is null || attachmentContent.Length == 0)
            throw new ArgumentException("El contenido del adjunto es requerido.", nameof(attachmentContent));
        if (string.IsNullOrWhiteSpace(attachmentFileName))
            throw new ArgumentException("El nombre del adjunto es requerido.", nameof(attachmentFileName));

        var adjunto = new EmailAttachment(attachmentContent, attachmentFileName, attachmentContentType);
        return SendInternalAsync(to, subject, htmlBody, adjunto, cancellationToken);
    }

    /// <summary>
    /// Lógica común de envío. Si <paramref name="adjunto"/> no es null, lo agrega al mensaje.
    /// En modo mock solo registra en el log.
    /// </summary>
    private async Task SendInternalAsync(
        string to,
        string subject,
        string htmlBody,
        EmailAttachment? adjunto,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(to))
            throw new ArgumentException("El destinatario es requerido.", nameof(to));

        if (_options.UseMock)
        {
            var adjuntoInfo = adjunto is null ? "" : $" | Adjunto: {adjunto.FileName} ({adjunto.Content.Length} bytes)";
            _logger.LogInformation(
                "Email MOCK (no enviado). Para: {To} | Asunto: {Subject}{Adjunto}\n{Body}",
                to, subject, adjuntoInfo, htmlBody);
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.Host))
        {
            _logger.LogError("No se puede enviar el email: Email:Host no está configurado.");
            throw new InvalidOperationException("El servidor SMTP no está configurado (Email:Host vacío).");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(new MailAddress(to));

        // El stream del adjunto debe seguir vivo hasta que se envíe el mensaje.
        MemoryStream? attachmentStream = null;
        Attachment? mailAttachment = null;
        if (adjunto is not null)
        {
            attachmentStream = new MemoryStream(adjunto.Content);
            mailAttachment = new Attachment(attachmentStream, adjunto.FileName, adjunto.ContentType);
            message.Attachments.Add(mailAttachment);
        }

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        // Solo autenticar si hay credenciales configuradas (algunos relays internos no las requieren).
        if (!string.IsNullOrWhiteSpace(_options.User))
        {
            client.Credentials = new NetworkCredential(_options.User, _options.Password);
        }

        try
        {
            await client.SendMailAsync(message, cancellationToken);
            _logger.LogInformation("Email enviado correctamente a {To} (asunto: {Subject}).", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar email a {To} (asunto: {Subject}).", to, subject);
            throw;
        }
        finally
        {
            mailAttachment?.Dispose();
            attachmentStream?.Dispose();
        }
    }

    /// <summary>Representa un adjunto de correo en memoria.</summary>
    private sealed record EmailAttachment(byte[] Content, string FileName, string ContentType);
}
