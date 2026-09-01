namespace SistemaAlmacen.Shared.Enums;

/// <summary>
/// Tipo de movimiento en la cuenta corriente de un cliente.
/// Un Cargo incrementa la deuda del cliente; un Pago la reduce.
/// </summary>
public enum TipoMovimientoCuentaCorriente
{
    /// <summary>Cargo por una venta a crédito (aumenta el saldo deudor).</summary>
    Cargo = 1,

    /// <summary>Pago o cobro recibido del cliente (reduce el saldo deudor).</summary>
    Pago = 2,

    /// <summary>Ajuste por anulación de una venta a crédito (reduce el saldo deudor).</summary>
    AjusteAnulacion = 3
}
