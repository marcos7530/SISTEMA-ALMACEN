using SistemaAlmacen.Shared.Common;
using SistemaAlmacen.Shared.DTOs.Categorias;

namespace SistemaAlmacen.Business.Interfaces;

/// <summary>
/// Servicio de lógica de negocio para gestión de categorías.
/// </summary>
public interface ICategoriaService
{
    /// <summary>
    /// Obtiene todas las categorías ordenadas alfabéticamente por nombre.
    /// </summary>
    Task<List<CategoriaDto>> GetAllAsync();

    /// <summary>
    /// Crea una nueva categoría validando nombre (3-50 chars), descripción (máx 200) y unicidad de nombre.
    /// </summary>
    Task<Result<CategoriaDto>> CreateAsync(CreateCategoriaRequest request);

    /// <summary>
    /// Actualiza una categoría existente con las mismas validaciones que la creación.
    /// </summary>
    Task<Result<CategoriaDto>> UpdateAsync(int id, UpdateCategoriaRequest request);

    /// <summary>
    /// Elimina una categoría si no tiene productos asociados.
    /// </summary>
    Task<Result> DeleteAsync(int id);
}
