namespace SistemaAlmacen.Shared.Enums;

/// <summary>
/// Tipo de movimiento de stock de un producto.
/// </summary>
public enum TipoMovimientoStock
{
    /// <summary>
    /// Ingreso de stock (reposición / compra).
    /// </summary>
    Ingreso = 1,

    /// <summary>
    /// Baja de stock por rotura, vencimiento, pérdida, etc.
    /// </summary>
    Baja = 2,

    /// <summary>
    /// Egreso de stock por anulación de una compra previamente ingresada.
    /// </summary>
    AnulacionCompra = 3
}
