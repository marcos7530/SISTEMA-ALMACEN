namespace SistemaAlmacen.Shared.DTOs.Ventas;

/// <summary>
/// DTO de respuesta con datos de un pago asociado a una venta.
/// </summary>
public class VentaPagoDto
{
    public int Id { get; set; }
    public string MedioPagoNombre { get; set; } = string.Empty;
    public decimal Monto { get; set; }
}
