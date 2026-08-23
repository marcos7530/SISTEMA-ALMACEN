using SistemaAlmacen.Business.Interfaces;
using SistemaAlmacen.Data.Repositories;
using SistemaAlmacen.Shared.Common;

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
}
