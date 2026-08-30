using SistemaAlmacen.Business.Interfaces;
using SistemaAlmacen.Data.Entities;
using SistemaAlmacen.Data.Repositories;
using SistemaAlmacen.Shared.Common;
using SistemaAlmacen.Shared.DTOs.Productos;
using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Business.Services;

/// <summary>
/// Implementación del servicio de gestión de stock.
/// </summary>
public class StockService : IStockService
{
    private readonly IUnitOfWork _unitOfWork;

    public StockService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<bool> HasSufficientStockAsync(int productoId, int cantidad)
    {
        var producto = await _unitOfWork.Productos.GetByIdAsync(productoId);

        if (producto is null || !producto.Activo)
            return false;

        return producto.Stock >= cantidad;
    }

    /// <inheritdoc />
    public async Task<Result> DeductStockAsync(List<StockDeduction> deductions)
    {
        foreach (var deduction in deductions)
        {
            var producto = await _unitOfWork.Productos.GetByIdAsync(deduction.ProductoId);

            if (producto is null || !producto.Activo)
                return Result.Failure(
                    $"El producto con ID {deduction.ProductoId} no fue encontrado o está inactivo.",
                    "PRODUCTO_NO_ENCONTRADO");

            if (producto.Stock < deduction.Cantidad)
                return Result.Failure(
                    $"Stock insuficiente para el producto '{producto.Nombre}'. Disponible: {producto.Stock}, solicitado: {deduction.Cantidad}.",
                    "STOCK_INSUFICIENTE");

            producto.Stock -= deduction.Cantidad;
            producto.FechaModificacion = DateTime.UtcNow;
            _unitOfWork.Productos.Update(producto);
        }

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<ProductoDto>> RegistrarBajaAsync(int productoId, AjusteBajaStockRequest request, int usuarioId)
    {
        if (request.Cantidad < 1)
            return Result<ProductoDto>.Failure("La cantidad debe ser mayor o igual a uno.", "CANTIDAD_INVALIDA");

        if (!Enum.IsDefined(typeof(MotivoBajaStock), request.Motivo))
            return Result<ProductoDto>.Failure("El motivo seleccionado no es válido.", "MOTIVO_INVALIDO");

        var producto = await _unitOfWork.Productos.GetByIdAsync(productoId);

        if (producto is null || !producto.Activo)
            return Result<ProductoDto>.Failure("El producto no fue encontrado.", "PRODUCTO_NO_ENCONTRADO");

        if (producto.Stock < request.Cantidad)
            return Result<ProductoDto>.Failure(
                $"Stock insuficiente para dar de baja. Disponible: {producto.Stock}, solicitado: {request.Cantidad}.",
                "STOCK_INSUFICIENTE");

        var stockAnterior = producto.Stock;
        var now = DateTime.UtcNow;

        producto.Stock -= request.Cantidad;
        producto.FechaModificacion = now;
        _unitOfWork.Productos.Update(producto);

        await _unitOfWork.MovimientosStock.AddAsync(new MovimientoStock
        {
            ProductoId = producto.Id,
            UsuarioId = usuarioId,
            Tipo = TipoMovimientoStock.Baja,
            Motivo = request.Motivo,
            Cantidad = request.Cantidad,
            StockAnterior = stockAnterior,
            StockNuevo = producto.Stock,
            Observacion = string.IsNullOrWhiteSpace(request.Observacion) ? null : request.Observacion.Trim(),
            Fecha = now
        });

        await _unitOfWork.SaveChangesAsync();

        return Result<ProductoDto>.Success(await MapToDtoAsync(producto));
    }

    /// <inheritdoc />
    public async Task<Result<ProductoDto>> IncrementarPorCodigoBarrasAsync(ReponerStockRequest request, int usuarioId)
    {
        if (request.Cantidad < 1)
            return Result<ProductoDto>.Failure("La cantidad debe ser mayor o igual a uno.", "CANTIDAD_INVALIDA");

        if (string.IsNullOrWhiteSpace(request.CodigoBarras))
            return Result<ProductoDto>.Failure("El código de barras es requerido.", "CODIGO_REQUERIDO");

        var codigo = request.CodigoBarras.Trim();
        var producto = await _unitOfWork.Productos.FirstOrDefaultAsync(p =>
            p.Activo && p.CodigoBarras == codigo);

        if (producto is null)
            return Result<ProductoDto>.Failure(
                $"No se encontró un producto activo con el código de barras '{codigo}'.",
                "PRODUCTO_NO_ENCONTRADO");

        var stockAnterior = producto.Stock;
        var now = DateTime.UtcNow;

        producto.Stock += request.Cantidad;
        producto.FechaModificacion = now;
        _unitOfWork.Productos.Update(producto);

        await _unitOfWork.MovimientosStock.AddAsync(new MovimientoStock
        {
            ProductoId = producto.Id,
            UsuarioId = usuarioId,
            Tipo = TipoMovimientoStock.Ingreso,
            Motivo = null,
            Cantidad = request.Cantidad,
            StockAnterior = stockAnterior,
            StockNuevo = producto.Stock,
            Observacion = string.IsNullOrWhiteSpace(request.Observacion) ? null : request.Observacion.Trim(),
            Fecha = now
        });

        await _unitOfWork.SaveChangesAsync();

        return Result<ProductoDto>.Success(await MapToDtoAsync(producto));
    }

    /// <summary>
    /// Mapea un producto a su DTO, cargando la categoría si es necesario.
    /// </summary>
    private async Task<ProductoDto> MapToDtoAsync(Producto producto)
    {
        if (producto.Categoria is null)
        {
            producto.Categoria = (await _unitOfWork.Categorias.GetByIdAsync(producto.CategoriaId))!;
        }

        return new ProductoDto
        {
            Id = producto.Id,
            Nombre = producto.Nombre,
            CodigoBarras = producto.CodigoBarras,
            Descripcion = producto.Descripcion,
            Precio = producto.Precio,
            Stock = producto.Stock,
            CategoriaId = producto.CategoriaId,
            CategoriaNombre = producto.Categoria?.Nombre ?? string.Empty,
            Activo = producto.Activo
        };
    }
}
