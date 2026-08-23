using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Shared.DTOs.Caja;

/// <summary>
/// DTO de respuesta con datos de una caja.
/// </summary>
public class CajaDto
{
    public int Id { get; set; }
    public int PuntoDeVentaId { get; set; }
    public decimal MontoInicial { get; set; }
    public EstadoCaja Estado { get; set; }
    public DateTime FechaApertura { get; set; }
    public string UsuarioApertura { get; set; } = string.Empty;
}
