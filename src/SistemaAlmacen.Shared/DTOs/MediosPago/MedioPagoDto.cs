namespace SistemaAlmacen.Shared.DTOs.MediosPago;

/// <summary>
/// DTO de respuesta con datos de un medio de pago.
/// </summary>
public class MedioPagoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public bool EsSistema { get; set; }
}
