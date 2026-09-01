using SistemaAlmacen.Shared.Common;
using SistemaAlmacen.Shared.DTOs.CuentaCorriente;

namespace SistemaAlmacen.Business.Interfaces;

/// <summary>
/// Servicio de gestión de cuenta corriente (fiado) de clientes.
/// </summary>
public interface ICuentaCorrienteService
{
    /// <summary>
    /// Obtiene el estado de cuenta corriente de un cliente: saldo actual y movimientos.
    /// </summary>
    Task<Result<EstadoCuentaCorrienteDto>> GetEstadoCuentaAsync(int clienteId);

    /// <summary>
    /// Registra un cobro/pago recibido de un cliente, reduciendo su saldo deudor.
    /// </summary>
    Task<Result<MovimientoCuentaCorrienteDto>> RegistrarPagoAsync(int clienteId, RegistrarPagoRequest request, int usuarioId);

    /// <summary>
    /// Registra un cargo por venta a crédito en la cuenta del cliente.
    /// NO persiste cambios (SaveChanges) para poder participar de la transacción de la venta.
    /// Valida existencia, habilitación de cuenta corriente y límite de crédito.
    /// </summary>
    Task<Result> RegistrarCargoVentaAsync(int clienteId, int ventaId, decimal monto, int usuarioId);

    /// <summary>
    /// Registra un ajuste que revierte el cargo de una venta anulada.
    /// NO persiste cambios (SaveChanges) para poder participar de la transacción de anulación.
    /// </summary>
    Task RevertirCargoVentaAsync(int clienteId, int ventaId, decimal monto, int usuarioId);
}
