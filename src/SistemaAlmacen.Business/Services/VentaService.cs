using Microsoft.Extensions.Logging;
using SistemaAlmacen.Business.Interfaces;
using SistemaAlmacen.Data.Entities;
using SistemaAlmacen.Data.Repositories;
using SistemaAlmacen.Shared.Common;
using SistemaAlmacen.Shared.DTOs;
using SistemaAlmacen.Shared.DTOs.Ventas;
using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Business.Services;

/// <summary>
/// Implementación del servicio de ventas con lógica de negocio completa.
/// </summary>
public class VentaService : IVentaService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStockService _stockService;
    private readonly ICajaService _cajaService;
    private readonly ICuentaCorrienteService _cuentaCorrienteService;
    private readonly IAuditoriaService _auditoriaService;
    private readonly ILogger<VentaService> _logger;

    public VentaService(
        IUnitOfWork unitOfWork,
        IStockService stockService,
        ICajaService cajaService,
        ICuentaCorrienteService cuentaCorrienteService,
        IAuditoriaService auditoriaService,
        ILogger<VentaService> logger)
    {
        _unitOfWork = unitOfWork;
        _stockService = stockService;
        _cajaService = cajaService;
        _cuentaCorrienteService = cuentaCorrienteService;
        _auditoriaService = auditoriaService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<VentaDto> InitializeVentaAsync(int vendedorId)
    {
        var now = DateTime.UtcNow;
        var venta = new Venta
        {
            UsuarioId = vendedorId,
            Fecha = now,
            Total = 0m,
            Estado = EstadoVenta.Borrador,
            FechaCreacion = now
        };

        await _unitOfWork.Ventas.AddAsync(venta);
        await _unitOfWork.SaveChangesAsync();

        // Cargar el usuario para el nombre del vendedor
        var usuario = await _unitOfWork.Usuarios.GetByIdAsync(vendedorId);

        return new VentaDto
        {
            Id = venta.Id,
            Fecha = venta.Fecha,
            Total = venta.Total,
            Estado = venta.Estado,
            VendedorId = venta.UsuarioId,
            VendedorNombre = usuario?.Nombre ?? string.Empty
        };
    }

    /// <inheritdoc />
    public async Task<Result<DetalleVentaDto>> AddDetalleAsync(int ventaId, AddDetalleRequest request)
    {
        // Validar cantidad (1-10000)
        if (request.Cantidad < 1 || request.Cantidad > 10000)
            return Result<DetalleVentaDto>.Failure(
                "La cantidad debe estar entre 1 y 10,000.",
                "CANTIDAD_INVALIDA");

        // Obtener la venta con detalles
        var venta = await _unitOfWork.Ventas.GetVentaConDetallesAsync(ventaId);

        if (venta is null)
            return Result<DetalleVentaDto>.Failure(
                "La venta no fue encontrada.",
                "VENTA_NO_ENCONTRADA");

        if (venta.Estado != EstadoVenta.Borrador)
            return Result<DetalleVentaDto>.Failure(
                "Solo se pueden agregar detalles a ventas en estado Borrador.",
                "VENTA_NO_EDITABLE");

        // Verificar que el producto existe y está activo
        var producto = await _unitOfWork.Productos.GetByIdAsync(request.ProductoId);

        if (producto is null || !producto.Activo)
            return Result<DetalleVentaDto>.Failure(
                "El producto no fue encontrado o está inactivo.",
                "PRODUCTO_NO_ENCONTRADO");

        // Verificar stock disponible
        var hasSufficientStock = await _stockService.HasSufficientStockAsync(request.ProductoId, request.Cantidad);

        if (!hasSufficientStock)
            return Result<DetalleVentaDto>.Failure(
                $"Stock insuficiente para el producto '{producto.Nombre}'. Disponible: {producto.Stock}, solicitado: {request.Cantidad}.",
                "STOCK_INSUFICIENTE");

        // Calcular subtotal
        var subtotal = producto.Precio * request.Cantidad;

        // Crear detalle de venta
        var detalle = new DetalleVenta
        {
            VentaId = ventaId,
            ProductoId = request.ProductoId,
            Cantidad = request.Cantidad,
            PrecioUnitario = producto.Precio,
            Subtotal = subtotal
        };

        venta.Detalles.Add(detalle);

        // Recalcular total
        venta.Total = venta.Detalles.Sum(d => d.Subtotal);
        _unitOfWork.Ventas.Update(venta);

        await _unitOfWork.SaveChangesAsync();

        return Result<DetalleVentaDto>.Success(new DetalleVentaDto
        {
            Id = detalle.Id,
            ProductoId = detalle.ProductoId,
            ProductoNombre = producto.Nombre,
            Cantidad = detalle.Cantidad,
            PrecioUnitario = detalle.PrecioUnitario,
            Subtotal = detalle.Subtotal
        });
    }

    /// <inheritdoc />
    public async Task<Result> RemoveDetalleAsync(int ventaId, int detalleId)
    {
        var venta = await _unitOfWork.Ventas.GetVentaConDetallesAsync(ventaId);

        if (venta is null)
            return Result.Failure("La venta no fue encontrada.", "VENTA_NO_ENCONTRADA");

        if (venta.Estado != EstadoVenta.Borrador)
            return Result.Failure("Solo se pueden eliminar detalles de ventas en estado Borrador.", "VENTA_NO_EDITABLE");

        var detalle = venta.Detalles.FirstOrDefault(d => d.Id == detalleId);

        if (detalle is null)
            return Result.Failure("El detalle de venta no fue encontrado.", "DETALLE_NO_ENCONTRADO");

        venta.Detalles.Remove(detalle);

        // Recalcular total
        venta.Total = venta.Detalles.Sum(d => d.Subtotal);
        _unitOfWork.Ventas.Update(venta);

        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<VentaDto>> ConfirmVentaAsync(int ventaId, ConfirmVentaRequest request)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var venta = await _unitOfWork.Ventas.GetVentaConDetallesAsync(ventaId);

            if (venta is null)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Result<VentaDto>.Failure("La venta no fue encontrada.", "VENTA_NO_ENCONTRADA");
            }

            if (venta.Estado != EstadoVenta.Borrador)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Result<VentaDto>.Failure("Solo se pueden confirmar ventas en estado Borrador.", "VENTA_NO_EDITABLE");
            }

            // Validar que la venta tiene al menos un detalle
            if (!venta.Detalles.Any())
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Result<VentaDto>.Failure(
                    "La venta debe tener al menos un producto.",
                    "VENTA_SIN_DETALLES");
            }

            // --- Validación de pagos ---

            // 1. Validar que se especificó al menos un medio de pago
            if (request.Pagos is null || !request.Pagos.Any())
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Result<VentaDto>.Failure(
                    "Debe especificar al menos un medio de pago.",
                    "PAGOS_REQUERIDOS");
            }

            // 2. Validar cada pago: MedioPagoId referencia un medio activo y Monto > 0
            var mediosPagoActivos = await _unitOfWork.MediosPago.GetActivosAsync();
            var mediosPagoDict = mediosPagoActivos.ToDictionary(m => m.Id);

            foreach (var pago in request.Pagos)
            {
                if (pago.Monto <= 0)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return Result<VentaDto>.Failure(
                        "El monto de cada pago debe ser mayor a cero.",
                        "PAGO_MONTO_INVALIDO");
                }

                if (!mediosPagoDict.ContainsKey(pago.MedioPagoId))
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return Result<VentaDto>.Failure(
                        $"El medio de pago con ID {pago.MedioPagoId} no existe o no está activo.",
                        "MEDIO_PAGO_INVALIDO");
                }
            }

            // 3. Validar que la suma de montos == total de la venta
            var sumaPagos = request.Pagos.Sum(p => p.Monto);
            if (sumaPagos != venta.Total)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Result<VentaDto>.Failure(
                    $"La suma de los pagos ({sumaPagos:C}) no coincide con el total de la venta ({venta.Total:C}).",
                    "PAGO_MONTO_NO_COINCIDE");
            }

            // 4. Determinar montos por efectivo y por cuenta corriente
            var montoEfectivo = 0m;
            var montoCuentaCorriente = 0m;
            foreach (var pago in request.Pagos)
            {
                var medioPago = mediosPagoDict[pago.MedioPagoId];
                if (medioPago.EsSistema) // Efectivo es el medio de pago del sistema
                {
                    montoEfectivo += pago.Monto;
                }
                else if (medioPago.EsCuentaCorriente)
                {
                    montoCuentaCorriente += pago.Monto;
                }
            }

            if (montoEfectivo > 0)
            {
                // Verificar que hay caja abierta (puntoDeVentaId = 1 por defecto)
                var hayCajaAbierta = await _cajaService.HayCajaAbiertaAsync(1);
                if (!hayCajaAbierta)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return Result<VentaDto>.Failure(
                        "No se puede aceptar pago en efectivo sin una caja abierta.",
                        "CAJA_NO_ABIERTA");
                }
            }

            // 5. Si hay pago en cuenta corriente, exigir cliente asociado
            if (montoCuentaCorriente > 0 && request.ClienteId is null)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Result<VentaDto>.Failure(
                    "Debe seleccionar un cliente para vender en cuenta corriente.",
                    "CLIENTE_REQUERIDO");
            }

            // Asociar cliente a la venta (si se indicó)
            if (request.ClienteId is not null)
            {
                venta.ClienteId = request.ClienteId;
            }

            // --- Fin validación de pagos ---

            // Verificar stock para todos los items
            var deductions = venta.Detalles.Select(d => new StockDeduction
            {
                ProductoId = d.ProductoId,
                Cantidad = d.Cantidad
            }).ToList();

            // Descontar stock atómicamente
            var deductResult = await _stockService.DeductStockAsync(deductions);

            if (!deductResult.IsSuccess)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Result<VentaDto>.Failure(deductResult.ErrorMessage!, deductResult.ErrorCode);
            }

            // Registrar desglose de pagos (VentaPago) para cada medio de pago
            foreach (var pago in request.Pagos)
            {
                var ventaPago = new VentaPago
                {
                    VentaId = ventaId,
                    MedioPagoId = pago.MedioPagoId,
                    Monto = pago.Monto
                };
                venta.Pagos.Add(ventaPago);
            }

            // Registrar cargo en cuenta corriente (participa de la transacción, no persiste solo)
            if (montoCuentaCorriente > 0)
            {
                var cargoResult = await _cuentaCorrienteService.RegistrarCargoVentaAsync(
                    request.ClienteId!.Value, ventaId, montoCuentaCorriente, venta.UsuarioId);

                if (!cargoResult.IsSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return Result<VentaDto>.Failure(cargoResult.ErrorMessage!, cargoResult.ErrorCode);
                }
            }

            // Marcar venta como confirmada
            venta.Estado = EstadoVenta.Confirmada;
            _unitOfWork.Ventas.Update(venta);

            await _unitOfWork.SaveChangesAsync();

            // Registrar monto en efectivo como ingreso de caja (solo efectivo)
            if (montoEfectivo > 0)
            {
                var cajaResult = await _cajaService.RegistrarVentaEfectivoAsync(
                    montoEfectivo, venta.UsuarioId, 1);

                if (!cajaResult.IsSuccess)
                {
                    _logger.LogWarning(
                        "No se pudo registrar ingreso de caja para venta {VentaId}: {Error}",
                        ventaId, cajaResult.ErrorMessage);
                }
            }

            await _unitOfWork.CommitTransactionAsync();

            return Result<VentaDto>.Success(new VentaDto
            {
                Id = venta.Id,
                Fecha = venta.Fecha,
                Total = venta.Total,
                Estado = venta.Estado,
                VendedorId = venta.UsuarioId,
                VendedorNombre = venta.Usuario?.Nombre ?? string.Empty
            });
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Conflicto de concurrencia al confirmar venta {VentaId}", ventaId);
            return Result<VentaDto>.Failure(
                "Conflicto de concurrencia. Otro usuario modificó los datos. Intente nuevamente.",
                "CONCURRENCIA");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Error confirmando venta {VentaId}", ventaId);
            return Result<VentaDto>.Failure(
                "La operación no pudo completarse. Los datos no fueron modificados.",
                "ERROR_INTERNO");
        }
    }

    /// <inheritdoc />
    public async Task<PaginatedResult<VentaResumenDto>> GetHistorialAsync(VentaFilter filter)
    {
        var result = await _unitOfWork.Ventas.GetHistorialAsync(filter);

        return new PaginatedResult<VentaResumenDto>
        {
            Items = result.Items.Select(v => new VentaResumenDto
            {
                Id = v.Id,
                Fecha = v.Fecha,
                Vendedor = v.Usuario?.Nombre ?? string.Empty,
                CantidadProductos = v.Detalles.Count,
                Total = v.Total,
                Estado = v.Estado,
                ClienteNombre = v.Cliente?.Nombre
            }).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
    }

    /// <inheritdoc />
    public async Task<VentaDetalleCompletoDto?> GetDetalleCompletoAsync(int ventaId)
    {
        var venta = await _unitOfWork.Ventas.GetVentaConDetallesAsync(ventaId);

        if (venta is null)
            return null;

        return new VentaDetalleCompletoDto
        {
            Id = venta.Id,
            Fecha = venta.Fecha,
            Total = venta.Total,
            Estado = venta.Estado,
            Vendedor = venta.Usuario?.Nombre ?? string.Empty,
            ClienteId = venta.ClienteId,
            ClienteNombre = venta.Cliente?.Nombre,
            Detalles = venta.Detalles.Select(d => new DetalleVentaDto
            {
                Id = d.Id,
                ProductoId = d.ProductoId,
                ProductoNombre = d.Producto?.Nombre ?? string.Empty,
                Cantidad = d.Cantidad,
                PrecioUnitario = d.PrecioUnitario,
                Subtotal = d.Subtotal
            }).ToList(),
            Pagos = venta.Pagos.Select(p => new VentaPagoDto
            {
                Id = p.Id,
                MedioPagoNombre = p.MedioPago?.Nombre ?? string.Empty,
                Monto = p.Monto
            }).ToList()
        };
    }

    /// <inheritdoc />
    public async Task<Result<VentaDto>> AnularVentaAsync(int ventaId, AnularVentaRequest request, int usuarioId)
    {
        if (string.IsNullOrWhiteSpace(request.Motivo))
            return Result<VentaDto>.Failure("Debe indicar el motivo de la anulación.", "MOTIVO_REQUERIDO");

        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var venta = await _unitOfWork.Ventas.GetVentaConDetallesAsync(ventaId);

            if (venta is null)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Result<VentaDto>.Failure("La venta no fue encontrada.", "NOT_FOUND");
            }

            // Solo se pueden anular ventas confirmadas o pendientes de facturación
            if (venta.Estado != EstadoVenta.Confirmada && venta.Estado != EstadoVenta.PendienteFacturacion)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Result<VentaDto>.Failure(
                    "Solo se pueden anular ventas confirmadas.",
                    "VENTA_NO_ANULABLE");
            }

            // 1. Reponer stock de todos los detalles
            var restorations = venta.Detalles.Select(d => new StockDeduction
            {
                ProductoId = d.ProductoId,
                Cantidad = d.Cantidad
            }).ToList();

            var restoreResult = await _stockService.RestoreStockAsync(
                restorations, usuarioId, $"Reposición por anulación de venta #{ventaId}");

            if (!restoreResult.IsSuccess)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Result<VentaDto>.Failure(restoreResult.ErrorMessage!, restoreResult.ErrorCode);
            }

            // 2. Determinar montos por medio de pago para revertir caja y cuenta corriente
            var mediosPago = await _unitOfWork.MediosPago.GetAllAsync();
            var mediosPagoDict = mediosPago.ToDictionary(m => m.Id);

            var montoEfectivo = 0m;
            var montoCuentaCorriente = 0m;
            foreach (var pago in venta.Pagos)
            {
                if (mediosPagoDict.TryGetValue(pago.MedioPagoId, out var mp))
                {
                    if (mp.EsSistema)
                        montoEfectivo += pago.Monto;
                    else if (mp.EsCuentaCorriente)
                        montoCuentaCorriente += pago.Monto;
                }
            }

            // 3. Revertir ingreso de caja en efectivo (si corresponde)
            if (montoEfectivo > 0)
            {
                var reversoCaja = await _cajaService.RevertirVentaEfectivoAsync(
                    montoEfectivo, usuarioId, ventaId, 1);

                if (!reversoCaja.IsSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return Result<VentaDto>.Failure(reversoCaja.ErrorMessage!, reversoCaja.ErrorCode);
                }
            }

            // 4. Revertir cargo en cuenta corriente (si corresponde)
            if (montoCuentaCorriente > 0 && venta.ClienteId is not null)
            {
                await _cuentaCorrienteService.RevertirCargoVentaAsync(
                    venta.ClienteId.Value, ventaId, montoCuentaCorriente, usuarioId);
            }

            // 5. Cambiar estado a Anulada
            venta.Estado = EstadoVenta.Anulada;
            _unitOfWork.Ventas.Update(venta);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            // 6. Registrar auditoría explícita con el motivo (fuera de la transacción principal)
            await _auditoriaService.RegistrarOperacionAsync(
                usuarioId,
                TipoOperacion.Anulacion,
                nameof(Venta),
                ventaId.ToString(),
                $"Anulación de venta #{ventaId}. Motivo: {request.Motivo.Trim()}");

            return Result<VentaDto>.Success(new VentaDto
            {
                Id = venta.Id,
                Fecha = venta.Fecha,
                Total = venta.Total,
                Estado = venta.Estado,
                VendedorId = venta.UsuarioId,
                VendedorNombre = venta.Usuario?.Nombre ?? string.Empty
            });
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Error anulando venta {VentaId}", ventaId);
            return Result<VentaDto>.Failure(
                "La operación no pudo completarse. Los datos no fueron modificados.",
                "ERROR_INTERNO");
        }
    }
}
