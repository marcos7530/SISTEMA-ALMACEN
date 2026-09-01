using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Shared.DTOs.CuentaCorriente;

/// <summary>
/// DTO de un movimiento de cuenta corriente.
/// </summary>
public class MovimientoCuentaCorrienteDto
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public TipoMovimientoCuentaCorriente Tipo { get; set; }
    public decimal Monto { get; set; }
    public int? VentaId { get; set; }
    public string? Descripcion { get; set; }
    public DateTime Fecha { get; set; }
}
