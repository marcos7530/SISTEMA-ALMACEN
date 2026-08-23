namespace SistemaAlmacen.Shared.DTOs.Reportes;

/// <summary>
/// DTO con desglose por medio de pago para reportes de ventas.
/// </summary>
public class DesgloseMedioPagoDto
{
    public string MedioPago { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public int Cantidad { get; set; }
}
