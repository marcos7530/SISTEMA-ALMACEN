using SistemaAlmacen.Shared.Common;
using SistemaAlmacen.Shared.DTOs;
using SistemaAlmacen.Shared.DTOs.Compras;

namespace SistemaAlmacen.Business.Interfaces;

/// <summary>
/// Servicio de lógica de negocio para el registro y gestión de compras de mercadería.
/// </summary>
public interface ICompraService
{
    /// <summary>
    /// Registra una compra de forma transaccional: suma stock, registra movimientos de stock,
    /// actualiza el precio de costo (último costo) y, opcionalmente, el precio de venta de cada producto.
    /// </summary>
    Task<Result<CompraDto>> RegistrarCompraAsync(CreateCompraRequest request, int usuarioId);

    /// <summary>
    /// Anula una compra confirmada, descontando el stock que había ingresado (valida disponibilidad).
    /// </summary>
    Task<Result<CompraDto>> AnularCompraAsync(int compraId, int usuarioId);

    Task<PaginatedResult<CompraResumenDto>> GetHistorialAsync(CompraFilter filter);

    Task<CompraDto?> GetDetalleAsync(int compraId);

    /// <summary>
    /// Calcula el precio de venta sugerido para un producto dado un costo unitario,
    /// aplicando el margen efectivo (producto > categoría > default).
    /// </summary>
    Task<Result<PrecioSugeridoDto>> CalcularPrecioSugeridoAsync(int productoId, decimal costoUnitario);
}
