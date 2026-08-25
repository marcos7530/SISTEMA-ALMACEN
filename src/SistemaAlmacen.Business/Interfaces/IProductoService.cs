using SistemaAlmacen.Shared.Common;
using SistemaAlmacen.Shared.DTOs;
using SistemaAlmacen.Shared.DTOs.Productos;

namespace SistemaAlmacen.Business.Interfaces;

/// <summary>
/// Servicio de lógica de negocio para la gestión de productos.
/// </summary>
public interface IProductoService
{
    /// <summary>
    /// Obtiene un listado paginado de productos activos con filtros opcionales.
    /// </summary>
    Task<PaginatedResult<ProductoDto>> GetProductosAsync(ProductoFilter filter);

    /// <summary>
    /// Obtiene un producto activo por su ID. Retorna null si no existe o está inactivo.
    /// </summary>
    Task<ProductoDto?> GetByIdAsync(int id);

    /// <summary>
    /// Obtiene un producto activo por su código de barras. Retorna null si no existe.
    /// </summary>
    Task<ProductoDto?> GetByCodigoBarrasAsync(string codigoBarras);

    /// <summary>
    /// Crea un nuevo producto con las validaciones de negocio correspondientes.
    /// </summary>
    Task<Result<ProductoDto>> CreateAsync(CreateProductoRequest request);

    /// <summary>
    /// Actualiza un producto existente con las validaciones de negocio correspondientes.
    /// </summary>
    Task<Result<ProductoDto>> UpdateAsync(int id, UpdateProductoRequest request);

    /// <summary>
    /// Realiza una eliminación lógica de un producto (marca como inactivo).
    /// </summary>
    Task<Result> DeactivateAsync(int id);
}
