using SistemaAlmacen.Shared.Common;
using SistemaAlmacen.Shared.DTOs.Productos;

namespace SistemaAlmacen.Business.Interfaces;

/// <summary>
/// Servicio de lógica de negocio para la gestión y validación de stock de productos.
/// </summary>
public interface IStockService
{
    /// <summary>
    /// Da de baja stock de un producto por un motivo determinado (rotura, vencimiento, etc.)
    /// y registra el movimiento en el historial.
    /// </summary>
    /// <param name="productoId">ID del producto al que se le da de baja stock.</param>
    /// <param name="request">Datos de la baja (cantidad, motivo, observación).</param>
    /// <param name="usuarioId">ID del usuario que realiza la operación.</param>
    /// <returns>El producto actualizado o un error.</returns>
    Task<Result<ProductoDto>> RegistrarBajaAsync(int productoId, AjusteBajaStockRequest request, int usuarioId);

    /// <summary>
    /// Incrementa (repone) el stock de un producto identificado por su código de barras
    /// y registra el movimiento en el historial.
    /// </summary>
    /// <param name="request">Datos de la reposición (código de barras, cantidad, observación).</param>
    /// <param name="usuarioId">ID del usuario que realiza la operación.</param>
    /// <returns>El producto actualizado o un error.</returns>
    Task<Result<ProductoDto>> IncrementarPorCodigoBarrasAsync(ReponerStockRequest request, int usuarioId);

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

    /// <summary>
    /// Repone (incrementa) stock de forma atómica para una lista de deducciones,
    /// por ejemplo al anular una venta. Registra los movimientos de stock como Ingreso.
    /// NO persiste cambios (SaveChanges): debe ejecutarse dentro de una transacción.
    /// </summary>
    /// <param name="restorations">Lista de reposiciones de stock a aplicar.</param>
    /// <param name="usuarioId">Usuario que origina la reposición.</param>
    /// <param name="observacion">Observación a registrar en el movimiento de stock.</param>
    Task<Result> RestoreStockAsync(List<StockDeduction> restorations, int usuarioId, string observacion);
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
