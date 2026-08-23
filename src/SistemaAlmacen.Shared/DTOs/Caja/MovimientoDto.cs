using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Shared.DTOs.Caja;

/// <summary>
/// DTO de respuesta con datos de un movimiento de caja.
/// </summary>
public class MovimientoDto
{
    public int Id { get; set; }
    public TipoMovimiento Tipo { get; set; }
    public decimal Monto { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string Usuario { get; set; } = string.Empty;
}
