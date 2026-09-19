namespace SistemaAlmacen.Business.Interfaces;

/// <summary>
/// Abstracción para el envío de correos electrónicos del sistema
/// (recuperación de contraseña, notificaciones, etc.).
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Envía un correo electrónico con cuerpo HTML.
    /// </summary>
    /// <param name="to">Dirección de correo del destinatario.</param>
    /// <param name="subject">Asunto del correo.</param>
    /// <param name="htmlBody">Cuerpo del correo en formato HTML.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);
}
