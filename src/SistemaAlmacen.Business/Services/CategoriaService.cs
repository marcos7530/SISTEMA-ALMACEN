using SistemaAlmacen.Business.Interfaces;
using SistemaAlmacen.Data.Entities;
using SistemaAlmacen.Data.Repositories;
using SistemaAlmacen.Shared.Common;
using SistemaAlmacen.Shared.DTOs.Categorias;

namespace SistemaAlmacen.Business.Services;

/// <summary>
/// Implementación del servicio de categorías con validaciones de negocio.
/// </summary>
public class CategoriaService : ICategoriaService
{
    private readonly IUnitOfWork _unitOfWork;

    public CategoriaService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<List<CategoriaDto>> GetAllAsync()
    {
        var categorias = await _unitOfWork.Categorias.GetAllAsync();

        return categorias
            .OrderBy(c => c.Nombre)
            .Select(MapToDto)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<Result<CategoriaDto>> CreateAsync(CreateCategoriaRequest request)
    {
        var validationResult = ValidateRequest(request.Nombre, request.Descripcion);
        if (!validationResult.IsSuccess)
            return Result<CategoriaDto>.Failure(validationResult.ErrorMessage!);

        // Verificar unicidad de nombre
        var existing = await _unitOfWork.Categorias.GetByNameAsync(request.Nombre.Trim());
        if (existing is not null)
            return Result<CategoriaDto>.Failure("El nombre de categoría ya está en uso.", "NOMBRE_DUPLICADO");

        var categoria = new Categoria
        {
            Nombre = request.Nombre.Trim(),
            Descripcion = request.Descripcion?.Trim(),
            FechaCreacion = DateTime.UtcNow
        };

        await _unitOfWork.Categorias.AddAsync(categoria);
        await _unitOfWork.SaveChangesAsync();

        return Result<CategoriaDto>.Success(MapToDto(categoria));
    }

    /// <inheritdoc />
    public async Task<Result<CategoriaDto>> UpdateAsync(int id, UpdateCategoriaRequest request)
    {
        var categoria = await _unitOfWork.Categorias.GetByIdAsync(id);
        if (categoria is null)
            return Result<CategoriaDto>.Failure("La categoría no fue encontrada.", "NO_ENCONTRADA");

        var validationResult = ValidateRequest(request.Nombre, request.Descripcion);
        if (!validationResult.IsSuccess)
            return Result<CategoriaDto>.Failure(validationResult.ErrorMessage!);

        // Verificar unicidad de nombre excluyendo la categoría actual
        var existing = await _unitOfWork.Categorias.GetByNameAsync(request.Nombre.Trim());
        if (existing is not null && existing.Id != id)
            return Result<CategoriaDto>.Failure("El nombre de categoría ya está en uso.", "NOMBRE_DUPLICADO");

        categoria.Nombre = request.Nombre.Trim();
        categoria.Descripcion = request.Descripcion?.Trim();

        _unitOfWork.Categorias.Update(categoria);
        await _unitOfWork.SaveChangesAsync();

        return Result<CategoriaDto>.Success(MapToDto(categoria));
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(int id)
    {
        var categoria = await _unitOfWork.Categorias.GetByIdAsync(id);
        if (categoria is null)
            return Result.Failure("La categoría no fue encontrada.", "NO_ENCONTRADA");

        var hasProductos = await _unitOfWork.Categorias.HasProductosAsync(id);
        if (hasProductos)
            return Result.Failure("No se puede eliminar la categoría porque tiene productos asociados.", "TIENE_PRODUCTOS");

        _unitOfWork.Categorias.Delete(categoria);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    /// <summary>
    /// Valida el nombre y la descripción de la categoría.
    /// </summary>
    private static Result ValidateRequest(string nombre, string? descripcion)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return Result.Failure("El nombre es requerido y no puede ser solo espacios en blanco.");

        var trimmedNombre = nombre.Trim();

        if (trimmedNombre.Length < 3)
            return Result.Failure("El nombre debe tener al menos 3 caracteres.");

        if (trimmedNombre.Length > 50)
            return Result.Failure("El nombre no puede exceder 50 caracteres.");

        if (descripcion is not null && descripcion.Trim().Length > 200)
            return Result.Failure("La descripción no puede exceder 200 caracteres.");

        return Result.Success();
    }

    /// <summary>
    /// Mapea una entidad Categoria a CategoriaDto.
    /// </summary>
    private static CategoriaDto MapToDto(Categoria categoria)
    {
        return new CategoriaDto
        {
            Id = categoria.Id,
            Nombre = categoria.Nombre,
            Descripcion = categoria.Descripcion,
            FechaCreacion = categoria.FechaCreacion
        };
    }
}
