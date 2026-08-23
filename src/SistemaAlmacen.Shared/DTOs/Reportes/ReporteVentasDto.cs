namespace SistemaAlmacen.Shared.DTOs.Reportes;

/// <summary>
/// DTO con resumen de reporte de ventas.
/// </summary>
public class ReporteVentasDto
{
    public decimal TotalMonetario { get; set; }
    public int CantidadTransacciones { get; set; }
    public List<DesgloseDiarioDto> DesgloseDiario { get; set; } = new();
    public List<DesgloseMedioPagoDto> DesgloseMediosPago { get; set; } = new();
}
