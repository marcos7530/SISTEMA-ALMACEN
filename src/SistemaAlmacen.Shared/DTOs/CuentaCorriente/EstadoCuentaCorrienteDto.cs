namespace SistemaAlmacen.Shared.DTOs.CuentaCorriente;

/// <summary>
/// Estado de cuenta corriente de un cliente: saldo actual y detalle de movimientos.
/// </summary>
public class EstadoCuentaCorrienteDto
{
    public int ClienteId { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public decimal LimiteCredito { get; set; }

    /// <summary>Saldo deudor actual (positivo = el cliente debe).</summary>
    public decimal Saldo { get; set; }

    /// <summary>Crédito disponible restante (LimiteCredito - Saldo). Null si no tiene límite.</summary>
    public decimal? CreditoDisponible { get; set; }

    public List<MovimientoCuentaCorrienteDto> Movimientos { get; set; } = new();
}
