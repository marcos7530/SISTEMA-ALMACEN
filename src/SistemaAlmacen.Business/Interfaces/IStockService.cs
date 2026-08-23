using SistemaAlmacen.Shared.Common;

namespace SistemaAlmacen.Business.Interfaces;

/// <summary>
/// Servicio de lógica de negocio para la gestión y validación de stock de productos.
/// </summary>
public interface IStockService
{
    /// <summary>
    /// Verifica si hay stock suficiente para la cantidad solicitada de un producto.
    /// </summary>
    /// <param name="productoId">ID del producto a verificar.</param>
    /// <param name="cantidad">Cantidad solicitada.</param>
    /// <returns>True si hay stock suficiente, false en caso contrario.</returns>
    Task<bool> HasSufficientStockAsync(int productoId, int cantidad);

    /// <summary>
    /// Descuenta stock de forma atómica para una lista de deducciones.
    /// Debe ejecutarse dentro de una transacción.
    /// </summary>
    /// <param name="deductions">Lista de deducciones de stock a aplicar.</param>
    /// <returns>Resultado exitoso o fallido con mensaje de error.</returns>
    Task<Result> DeductStockAsync(List<StockDeduction> deductions);
}

/// <summary>
/// Representa una deducción de stock para un producto específico.
/// </summary>
public class StockDeduction
{
    /// <summary>
    /// ID del producto al que se le deduce stock.
    /// </summary>
    public int ProductoId { get; set; }

    /// <summary>
    /// Cantidad a deducir del stock.
    /// </summary>
    public int Cantidad { get; set; }
}
