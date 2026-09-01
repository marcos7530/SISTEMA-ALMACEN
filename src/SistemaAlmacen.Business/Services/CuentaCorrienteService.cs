using SistemaAlmacen.Business.Interfaces;
using SistemaAlmacen.Data.Entities;
using SistemaAlmacen.Data.Repositories;
using SistemaAlmacen.Shared.Common;
using SistemaAlmacen.Shared.DTOs.CuentaCorriente;
using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Business.Services;

/// <summary>
/// Implementación del servicio de cuenta corriente de clientes.
/// </summary>
public class CuentaCorrienteService : ICuentaCorrienteService
{
    private readonly IUnitOfWork _unitOfWork;

    public CuentaCorrienteService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result<EstadoCuentaCorrienteDto>> GetEstadoCuentaAsync(int clienteId)
    {
        var cliente = await _unitOfWork.Clientes.GetByIdAsync(clienteId);
        if (cliente is null || !cliente.Activo)
            return Result<EstadoCuentaCorrienteDto>.Failure("El cliente no fue encontrado.", "NOT_FOUND");

        if (!cliente.CuentaCorrienteHabilitada)
            return Result<EstadoCuentaCorrienteDto>.Failure(
                "El cliente no tiene cuenta corriente habilitada.", "CTA_CTE_NO_HABILITADA");

        var saldo = await _unitOfWork.Clientes.GetSaldoAsync(clienteId);
        var movimientos = await _unitOfWork.Clientes.GetMovimientosAsync(clienteId);

        var dto = new EstadoCuentaCorrienteDto
        {
            ClienteId = cliente.Id,
            ClienteNombre = cliente.Nombre,
            LimiteCredito = cliente.LimiteCredito,
            Saldo = saldo,
            CreditoDisponible = cliente.LimiteCredito > 0 ? cliente.LimiteCredito - saldo : null,
            Movimientos = movimientos.Select(MapToDto).ToList()
        };

        return Result<EstadoCuentaCorrienteDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<Result<MovimientoCuentaCorrienteDto>> RegistrarPagoAsync(int clienteId, RegistrarPagoRequest request, int usuarioId)
    {
        var cliente = await _unitOfWork.Clientes.GetByIdAsync(clienteId);
        if (cliente is null || !cliente.Activo)
            return Result<MovimientoCuentaCorrienteDto>.Failure("El cliente no fue encontrado.", "NOT_FOUND");

        if (!cliente.CuentaCorrienteHabilitada)
            return Result<MovimientoCuentaCorrienteDto>.Failure(
                "El cliente no tiene cuenta corriente habilitada.", "CTA_CTE_NO_HABILITADA");

        if (request.Monto <= 0)
            return Result<MovimientoCuentaCorrienteDto>.Failure("El monto debe ser mayor a cero.", "MONTO_INVALIDO");

        var saldo = await _unitOfWork.Clientes.GetSaldoAsync(clienteId);
        if (request.Monto > saldo)
            return Result<MovimientoCuentaCorrienteDto>.Failure(
                $"El monto del pago ({request.Monto:C}) no puede superar el saldo deudor ({saldo:C}).",
                "PAGO_EXCEDE_SALDO");

        var movimiento = new MovimientoCuentaCorriente
        {
            ClienteId = clienteId,
            Tipo = TipoMovimientoCuentaCorriente.Pago,
            Monto = request.Monto,
            UsuarioId = usuarioId,
            Descripcion = string.IsNullOrWhiteSpace(request.Descripcion) ? "Cobro recibido" : request.Descripcion.Trim(),
            Fecha = DateTime.UtcNow
        };

        await _unitOfWork.Clientes.AddMovimientoAsync(movimiento);
        await _unitOfWork.SaveChangesAsync();

        return Result<MovimientoCuentaCorrienteDto>.Success(MapToDto(movimiento));
    }

    /// <inheritdoc />
    public async Task<Result> RegistrarCargoVentaAsync(int clienteId, int ventaId, decimal monto, int usuarioId)
    {
        var cliente = await _unitOfWork.Clientes.GetByIdAsync(clienteId);
        if (cliente is null || !cliente.Activo)
            return Result.Failure("El cliente no fue encontrado.", "CLIENTE_NO_ENCONTRADO");

        if (!cliente.CuentaCorrienteHabilitada)
            return Result.Failure(
                "El cliente no tiene cuenta corriente habilitada.", "CTA_CTE_NO_HABILITADA");

        // Validar límite de crédito (0 = sin límite)
        if (cliente.LimiteCredito > 0)
        {
            var saldoActual = await _unitOfWork.Clientes.GetSaldoAsync(clienteId);
            if (saldoActual + monto > cliente.LimiteCredito)
            {
                var disponible = cliente.LimiteCredito - saldoActual;
                return Result.Failure(
                    $"El cargo excede el límite de crédito. Disponible: {disponible:C}, requerido: {monto:C}.",
                    "LIMITE_CREDITO_EXCEDIDO");
            }
        }

        var movimiento = new MovimientoCuentaCorriente
        {
            ClienteId = clienteId,
            Tipo = TipoMovimientoCuentaCorriente.Cargo,
            Monto = monto,
            VentaId = ventaId,
            UsuarioId = usuarioId,
            Descripcion = $"Venta a crédito #{ventaId}",
            Fecha = DateTime.UtcNow
        };

        await _unitOfWork.Clientes.AddMovimientoAsync(movimiento);
        // No se llama SaveChanges: participa de la transacción de la venta.

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task RevertirCargoVentaAsync(int clienteId, int ventaId, decimal monto, int usuarioId)
    {
        var movimiento = new MovimientoCuentaCorriente
        {
            ClienteId = clienteId,
            Tipo = TipoMovimientoCuentaCorriente.AjusteAnulacion,
            Monto = monto,
            VentaId = ventaId,
            UsuarioId = usuarioId,
            Descripcion = $"Anulación de venta a crédito #{ventaId}",
            Fecha = DateTime.UtcNow
        };

        await _unitOfWork.Clientes.AddMovimientoAsync(movimiento);
        // No se llama SaveChanges: participa de la transacción de anulación.
    }

    private static MovimientoCuentaCorrienteDto MapToDto(MovimientoCuentaCorriente m)
    {
        return new MovimientoCuentaCorrienteDto
        {
            Id = m.Id,
            ClienteId = m.ClienteId,
            Tipo = m.Tipo,
            Monto = m.Monto,
            VentaId = m.VentaId,
            Descripcion = m.Descripcion,
            Fecha = m.Fecha
        };
    }
}
