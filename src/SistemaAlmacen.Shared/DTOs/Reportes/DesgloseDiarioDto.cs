namespace SistemaAlmacen.Shared.DTOs.Reportes;

/// <summary>
/// DTO con desglose diario para reportes de ventas.
/// </summary>
public class DesgloseDiarioDto
{
    public DateTime Fecha { get; set; }
    public decimal Monto { get; set; }
    public int Transacciones { get; set; }
}
