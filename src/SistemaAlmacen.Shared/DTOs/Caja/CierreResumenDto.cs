namespace SistemaAlmacen.Shared.DTOs.Caja;

/// <summary>
/// DTO con resumen de cierre de caja.
/// </summary>
public class CierreResumenDto
{
    public int Id { get; set; }
    public DateTime FechaApertura { get; set; }
    public DateTime FechaCierre { get; set; }
    public decimal MontoInicial { get; set; }
    public decimal SaldoEsperado { get; set; }
    public decimal MontoRealCierre { get; set; }
    public decimal Diferencia { get; set; }
    public bool DiferenciaSignificativa { get; set; }
    public List<MovimientoDto> Movimientos { get; set; } = new();
}
