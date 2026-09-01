using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Shared.DTOs.Clientes;

/// <summary>
/// DTO de respuesta con datos de un cliente.
/// </summary>
public class ClienteDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Documento { get; set; }
    public CondicionIva CondicionIva { get; set; }
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public bool CuentaCorrienteHabilitada { get; set; }
    public decimal LimiteCredito { get; set; }
    public bool Activo { get; set; }

    /// <summary>Saldo deudor actual en cuenta corriente.</summary>
    public decimal SaldoCuentaCorriente { get; set; }
}
