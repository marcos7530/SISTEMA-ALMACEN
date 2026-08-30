using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Shared.DTOs.Productos;

/// <summary>
/// DTO de respuesta con datos de un movimiento de stock registrado.
/// </summary>
public class MovimientoStockDto
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    public TipoMovimientoStock Tipo { get; set; }
    public MotivoBajaStock? Motivo { get; set; }
    public int Cantidad { get; set; }
    public int StockAnterior { get; set; }
    public int StockNuevo { get; set; }
    public string? Observacion { get; set; }
    public int UsuarioId { get; set; }
    public DateTime Fecha { get; set; }
}
