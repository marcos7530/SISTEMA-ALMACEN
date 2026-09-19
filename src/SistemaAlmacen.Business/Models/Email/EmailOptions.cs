namespace SistemaAlmacen.Business.Models.Email;

/// <summary>
/// Opciones de configuración para el envío de correos (sección "Email" en appsettings).
/// </summary>
public class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>
    /// Si es true, no se envía el correo por SMTP; se registra en el log.
    /// Útil en desarrollo cuando no hay servidor SMTP configurado.
    /// </summary>
    public bool UseMock { get; set; } = true;

    /// <summary>Host del servidor SMTP (ej: smtp.gmail.com).</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>Puerto SMTP (ej: 587 para STARTTLS, 465 para SSL implícito).</summary>
    public int Port { get; set; } = 587;

    /// <summary>Usuario de autenticación SMTP.</summary>
    public string User { get; set; } = string.Empty;

    /// <summary>Contraseña o app password de autenticación SMTP.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Habilita SSL/TLS en la conexión SMTP.</summary>
    public bool EnableSsl { get; set; } = true;

    /// <summary>Dirección de correo remitente.</summary>
    public string FromAddress { get; set; } = "no-reply@sistemaalmacen.local";

    /// <summary>Nombre visible del remitente.</summary>
    public string FromName { get; set; } = "Sistema Almacén";

    /// <summary>
    /// URL base de la aplicación cliente, usada para construir los enlaces
    /// de los correos (ej: https://midominio.com). Sin barra final.
    /// </summary>
    public string BaseUrl { get; set; } = "https://localhost:5001";
}
