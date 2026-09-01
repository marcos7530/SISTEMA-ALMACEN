using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SistemaAlmacen.Business.Interfaces;
using SistemaAlmacen.Data.Entities;
using SistemaAlmacen.Data.Repositories;
using SistemaAlmacen.Shared.Common;
using SistemaAlmacen.Shared.DTOs;
using SistemaAlmacen.Shared.DTOs.Compras;
using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Business.Services;

/// <summary>
/// Implementación del servicio de compras de mercadería.
/// </summary>
public class CompraService : ICompraService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly IAuditoriaService _auditoriaService;
    private readonly ILogger<CompraService> _logger;

    public CompraService(
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        IAuditoriaService auditoriaService,
        ILogger<CompraService> logger)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _auditoriaService = auditoriaService;
        _logger = logger;
    }

    private decimal MargenDefecto => _configuration.GetValue<decimal>("Compras:MargenDefecto", 40m);

    /// <inheritdoc />
    public async Task<Result<CompraDto>> RegistrarCompraAsync(CreateCompraRequest request, int usuarioId)
    {
        if (request.Detalles is null || request.Detalles.Count == 0)
            return Result<CompraDto>.Failure("La compra debe incluir al menos un producto.", "COMPRA_SIN_DETALLES");

        // Validaciones básicas de líneas
        foreach (var d in request.Detalles)
        {
            if (d.Cantidad < 1)
                return Result<CompraDto>.Failure("La cantidad de cada línea debe ser mayor o igual a 1.", "CANTIDAD_INVALIDA");
            if (d.CostoUnitario < 0)
                return Result<CompraDto>.Failure("El costo unitario no puede ser negativo.", "COSTO_INVALIDO");
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var proveedor = await _unitOfWork.Proveedores.GetByIdAsync(request.ProveedorId);
            if (proveedor is null || !proveedor.Activo)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Result<CompraDto>.Failure("El proveedor no fue encontrado o está inactivo.", "PROVEEDOR_NO_ENCONTRADO");
            }

            var now = DateTime.UtcNow;
            var compra = new Compra
            {
                ProveedorId = proveedor.Id,
                UsuarioId = usuarioId,
                Fecha = now,
                NumeroComprobante = string.IsNullOrWhiteSpace(request.NumeroComprobante) ? null : request.NumeroComprobante.Trim(),
                Total = 0m,
                Estado = EstadoCompra.Confirmada,
                FechaCreacion = now
            };

            await _unitOfWork.Compras.AddAsync(compra);
            await _unitOfWork.SaveChangesAsync(); // Necesitamos el Id de la compra para los movimientos

            decimal total = 0m;

            foreach (var linea in request.Detalles)
            {
                var producto = await _unitOfWork.Productos.GetByIdAsync(linea.ProductoId);
                if (producto is null || !producto.Activo)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return Result<CompraDto>.Failure(
                        $"El producto con ID {linea.ProductoId} no fue encontrado o está inactivo.",
                        "PRODUCTO_NO_ENCONTRADO");
                }

                var subtotal = linea.CostoUnitario * linea.Cantidad;
                total += subtotal;

                // Registrar detalle de compra
                compra.Detalles.Add(new DetalleCompra
                {
                    CompraId = compra.Id,
                    ProductoId = producto.Id,
                    Cantidad = linea.Cantidad,
                    CostoUnitario = linea.CostoUnitario,
                    Subtotal = subtotal
                });

                // Sumar stock + registrar movimiento
                var stockAnterior = producto.Stock;
                producto.Stock += linea.Cantidad;

                // Actualizar precio de costo (último costo)
                producto.PrecioCosto = linea.CostoUnitario;

                // Aplicar nuevo precio de venta si fue indicado
                if (linea.NuevoPrecioVenta.HasValue && linea.NuevoPrecioVenta.Value > 0)
                {
                    producto.Precio = linea.NuevoPrecioVenta.Value;
                }

                producto.FechaModificacion = now;
                _unitOfWork.Productos.Update(producto);

                await _unitOfWork.MovimientosStock.AddAsync(new MovimientoStock
                {
                    ProductoId = producto.Id,
                    UsuarioId = usuarioId,
                    Tipo = TipoMovimientoStock.Ingreso,
                    Motivo = null,
                    Cantidad = linea.Cantidad,
                    StockAnterior = stockAnterior,
                    StockNuevo = producto.Stock,
                    Observacion = $"Ingreso por compra #{compra.Id}",
                    CompraId = compra.Id,
                    Fecha = now
                });
            }

            compra.Total = total;
            _unitOfWork.Compras.Update(compra);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            var completa = await _unitOfWork.Compras.GetCompraConDetallesAsync(compra.Id);
            return Result<CompraDto>.Success(MapToDto(completa!));
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Error registrando compra para proveedor {ProveedorId}", request.ProveedorId);
            return Result<CompraDto>.Failure(
                "La operación no pudo completarse. Los datos no fueron modificados.",
                "ERROR_INTERNO");
        }
    }

    /// <inheritdoc />
    public async Task<Result<CompraDto>> AnularCompraAsync(int compraId, int usuarioId)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var compra = await _unitOfWork.Compras.GetCompraConDetallesAsync(compraId);
            if (compra is null)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Result<CompraDto>.Failure("La compra no fue encontrada.", "NOT_FOUND");
            }

            if (compra.Estado != EstadoCompra.Confirmada)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return Result<CompraDto>.Failure("Solo se pueden anular compras confirmadas.", "COMPRA_NO_ANULABLE");
            }

            var now = DateTime.UtcNow;

            // Validar que hay stock suficiente para revertir cada línea
            foreach (var detalle in compra.Detalles)
            {
                var producto = await _unitOfWork.Productos.GetByIdAsync(detalle.ProductoId);
                if (producto is null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return Result<CompraDto>.Failure(
                        $"El producto con ID {detalle.ProductoId} no fue encontrado.",
                        "PRODUCTO_NO_ENCONTRADO");
                }

                if (producto.Stock < detalle.Cantidad)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return Result<CompraDto>.Failure(
                        $"No se puede anular la compra: el producto '{producto.Nombre}' no tiene stock suficiente para revertir el ingreso (disponible: {producto.Stock}, a revertir: {detalle.Cantidad}).",
                        "STOCK_INSUFICIENTE");
                }

                var stockAnterior = producto.Stock;
                producto.Stock -= detalle.Cantidad;
                producto.FechaModificacion = now;
                _unitOfWork.Productos.Update(producto);

                await _unitOfWork.MovimientosStock.AddAsync(new MovimientoStock
                {
                    ProductoId = producto.Id,
                    UsuarioId = usuarioId,
                    Tipo = TipoMovimientoStock.AnulacionCompra,
                    Motivo = null,
                    Cantidad = detalle.Cantidad,
                    StockAnterior = stockAnterior,
                    StockNuevo = producto.Stock,
                    Observacion = $"Egreso por anulación de compra #{compra.Id}",
                    CompraId = compra.Id,
                    Fecha = now
                });
            }

            compra.Estado = EstadoCompra.Anulada;
            _unitOfWork.Compras.Update(compra);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            await _auditoriaService.RegistrarOperacionAsync(
                usuarioId,
                TipoOperacion.Anulacion,
                nameof(Compra),
                compraId.ToString(),
                $"Anulación de compra #{compraId}. Se revirtió el stock ingresado.");

            var completa = await _unitOfWork.Compras.GetCompraConDetallesAsync(compra.Id);
            return Result<CompraDto>.Success(MapToDto(completa!));
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Error anulando compra {CompraId}", compraId);
            return Result<CompraDto>.Failure(
                "La operación no pudo completarse. Los datos no fueron modificados.",
                "ERROR_INTERNO");
        }
    }

    /// <inheritdoc />
    public async Task<PaginatedResult<CompraResumenDto>> GetHistorialAsync(CompraFilter filter)
    {
        var result = await _unitOfWork.Compras.GetHistorialAsync(filter);

        return new PaginatedResult<CompraResumenDto>
        {
            Items = result.Items.Select(c => new CompraResumenDto
            {
                Id = c.Id,
                Fecha = c.Fecha,
                ProveedorNombre = c.Proveedor?.Nombre ?? string.Empty,
                NumeroComprobante = c.NumeroComprobante,
                CantidadItems = c.Detalles.Count,
                Total = c.Total,
                Estado = c.Estado
            }).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
    }

    /// <inheritdoc />
    public async Task<CompraDto?> GetDetalleAsync(int compraId)
    {
        var compra = await _unitOfWork.Compras.GetCompraConDetallesAsync(compraId);
        return compra is null ? null : MapToDto(compra);
    }

    /// <inheritdoc />
    public async Task<Result<PrecioSugeridoDto>> CalcularPrecioSugeridoAsync(int productoId, decimal costoUnitario)
    {
        if (costoUnitario < 0)
            return Result<PrecioSugeridoDto>.Failure("El costo no puede ser negativo.", "COSTO_INVALIDO");

        var producto = await _unitOfWork.Productos.GetByIdAsync(productoId);
        if (producto is null || !producto.Activo)
            return Result<PrecioSugeridoDto>.Failure("El producto no fue encontrado.", "NOT_FOUND");

        if (producto.Categoria is null)
        {
            producto.Categoria = (await _unitOfWork.Categorias.GetByIdAsync(producto.CategoriaId))!;
        }

        var (margen, origen) = MargenCalculator.ResolverMargenProducto(producto, MargenDefecto);
        var precioSugerido = MargenCalculator.CalcularPrecioSugerido(costoUnitario, margen);

        return Result<PrecioSugeridoDto>.Success(new PrecioSugeridoDto
        {
            ProductoId = productoId,
            CostoUnitario = costoUnitario,
            MargenEfectivo = margen,
            OrigenMargen = origen,
            PrecioVentaSugerido = precioSugerido
        });
    }

    private static CompraDto MapToDto(Compra compra)
    {
        return new CompraDto
        {
            Id = compra.Id,
            ProveedorId = compra.ProveedorId,
            ProveedorNombre = compra.Proveedor?.Nombre ?? string.Empty,
            Fecha = compra.Fecha,
            NumeroComprobante = compra.NumeroComprobante,
            Total = compra.Total,
            Estado = compra.Estado,
            Usuario = compra.Usuario?.Nombre ?? string.Empty,
            Detalles = compra.Detalles.Select(d => new DetalleCompraDto
            {
                Id = d.Id,
                ProductoId = d.ProductoId,
                ProductoNombre = d.Producto?.Nombre ?? string.Empty,
                Cantidad = d.Cantidad,
                CostoUnitario = d.CostoUnitario,
                Subtotal = d.Subtotal
            }).ToList()
        };
    }
}
