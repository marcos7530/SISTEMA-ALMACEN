using Microsoft.Extensions.Configuration;
using SistemaAlmacen.Business.Interfaces;
using SistemaAlmacen.Data.Entities;
using SistemaAlmacen.Data.Repositories;
using SistemaAlmacen.Shared.Common;
using SistemaAlmacen.Shared.DTOs;
using SistemaAlmacen.Shared.DTOs.Productos;

namespace SistemaAlmacen.Business.Services;

/// <summary>
/// Implementación del servicio de productos con validaciones de negocio.
/// </summary>
public class ProductoService : IProductoService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;

    public ProductoService(IUnitOfWork unitOfWork, IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
    }

    private decimal MargenDefecto => _configuration.GetValue<decimal>("Compras:MargenDefecto", 40m);

    /// <inheritdoc />
    public async Task<PaginatedResult<ProductoDto>> GetProductosAsync(ProductoFilter filter)
    {
        var result = await _unitOfWork.Productos.GetActiveProductosAsync(filter);

        return new PaginatedResult<ProductoDto>
        {
            Items = result.Items.Select(MapToDto).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
    }

    /// <inheritdoc />
    public async Task<ProductoDto?> GetByIdAsync(int id)
    {
        var producto = await _unitOfWork.Productos.GetByIdAsync(id);

        if (producto is null || !producto.Activo)
            return null;

        // Ensure Categoria is loaded for mapping
        if (producto.Categoria is null)
        {
            var categoria = await _unitOfWork.Categorias.GetByIdAsync(producto.CategoriaId);
            producto.Categoria = categoria!;
        }

        return MapToDto(producto);
    }

    /// <inheritdoc />
    public async Task<ProductoDto?> GetByCodigoBarrasAsync(string codigoBarras)
    {
        var producto = await _unitOfWork.Productos.FirstOrDefaultAsync(p =>
            p.Activo && p.CodigoBarras == codigoBarras);

        if (producto is null)
            return null;

        // Ensure Categoria is loaded for mapping
        if (producto.Categoria is null)
        {
            var categoria = await _unitOfWork.Categorias.GetByIdAsync(producto.CategoriaId);
            producto.Categoria = categoria!;
        }

        return MapToDto(producto);
    }

    /// <inheritdoc />
    public async Task<Result<ProductoDto>> CreateAsync(CreateProductoRequest request)
    {
        var validationResult = ValidateProductoRequest(request.Nombre, request.Descripcion, request.Precio, request.Stock);
        if (validationResult is not null)
            return validationResult;

        // Validar que la categoría existe
        var categoria = await _unitOfWork.Categorias.GetByIdAsync(request.CategoriaId);
        if (categoria is null)
            return Result<ProductoDto>.Failure("La categoría seleccionada no es válida.", "CATEGORIA_INVALIDA");

        // Validar unicidad de nombre por categoría (solo entre productos activos)
        var existeNombre = await _unitOfWork.Productos.AnyAsync(p =>
            p.Activo &&
            p.CategoriaId == request.CategoriaId &&
            p.Nombre.ToLower() == request.Nombre.Trim().ToLower());

        if (existeNombre)
            return Result<ProductoDto>.Failure("Ya existe un producto con ese nombre en la categoría seleccionada.", "NOMBRE_DUPLICADO");

        var now = DateTime.UtcNow;
        var producto = new Producto
        {
            Nombre = request.Nombre.Trim(),
            CodigoBarras = string.IsNullOrWhiteSpace(request.CodigoBarras) ? null : request.CodigoBarras.Trim(),
            Descripcion = request.Descripcion?.Trim(),
            Precio = request.Precio,
            PrecioCosto = request.PrecioCosto,
            MargenGanancia = request.MargenGanancia,
            Stock = request.Stock,
            CategoriaId = request.CategoriaId,
            Activo = true,
            FechaCreacion = now,
            FechaModificacion = now
        };

        await _unitOfWork.Productos.AddAsync(producto);
        await _unitOfWork.SaveChangesAsync();

        producto.Categoria = categoria;
        return Result<ProductoDto>.Success(MapToDto(producto));
    }

    /// <inheritdoc />
    public async Task<Result<ProductoDto>> UpdateAsync(int id, UpdateProductoRequest request)
    {
        var producto = await _unitOfWork.Productos.GetByIdAsync(id);

        if (producto is null || !producto.Activo)
            return Result<ProductoDto>.Failure("El producto no fue encontrado.", "PRODUCTO_NO_ENCONTRADO");

        var validationResult = ValidateProductoRequest(request.Nombre, request.Descripcion, request.Precio, request.Stock);
        if (validationResult is not null)
            return validationResult;

        // Validar que la categoría existe
        var categoria = await _unitOfWork.Categorias.GetByIdAsync(request.CategoriaId);
        if (categoria is null)
            return Result<ProductoDto>.Failure("La categoría seleccionada no es válida.", "CATEGORIA_INVALIDA");

        // Validar unicidad de nombre por categoría excluyendo el producto actual
        var existeNombre = await _unitOfWork.Productos.AnyAsync(p =>
            p.Activo &&
            p.Id != id &&
            p.CategoriaId == request.CategoriaId &&
            p.Nombre.ToLower() == request.Nombre.Trim().ToLower());

        if (existeNombre)
            return Result<ProductoDto>.Failure("Ya existe un producto con ese nombre en la categoría seleccionada.", "NOMBRE_DUPLICADO");

        producto.Nombre = request.Nombre.Trim();
        producto.CodigoBarras = string.IsNullOrWhiteSpace(request.CodigoBarras) ? null : request.CodigoBarras.Trim();
        producto.Descripcion = request.Descripcion?.Trim();
        producto.Precio = request.Precio;
        producto.PrecioCosto = request.PrecioCosto;
        producto.MargenGanancia = request.MargenGanancia;
        producto.Stock = request.Stock;
        producto.CategoriaId = request.CategoriaId;
        producto.FechaModificacion = DateTime.UtcNow;

        _unitOfWork.Productos.Update(producto);
        await _unitOfWork.SaveChangesAsync();

        producto.Categoria = categoria;
        return Result<ProductoDto>.Success(MapToDto(producto));
    }

    /// <inheritdoc />
    public async Task<Result> DeactivateAsync(int id)
    {
        var producto = await _unitOfWork.Productos.GetByIdAsync(id);

        if (producto is null || !producto.Activo)
            return Result.Failure("El producto no fue encontrado.", "PRODUCTO_NO_ENCONTRADO");

        producto.Activo = false;
        producto.FechaModificacion = DateTime.UtcNow;

        _unitOfWork.Productos.Update(producto);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    private static Result<ProductoDto>? ValidateProductoRequest(string nombre, string? descripcion, decimal precio, int stock)
    {
        var errors = new Dictionary<string, string[]>();

        // Validar nombre
        if (string.IsNullOrWhiteSpace(nombre))
            errors["Nombre"] = new[] { "El nombre es requerido." };
        else if (nombre.Trim().Length > 100)
            errors["Nombre"] = new[] { "El nombre no puede exceder 100 caracteres." };

        // Validar descripción
        if (descripcion is not null && descripcion.Trim().Length > 500)
            errors["Descripcion"] = new[] { "La descripción no puede exceder 500 caracteres." };

        // Validar precio
        if (precio < 0.01m || precio > 999_999_999.99m)
            errors["Precio"] = new[] { "El precio debe estar entre 0.01 y 999,999,999.99." };

        // Validar stock
        if (stock < 0)
            errors["Stock"] = new[] { "El stock debe ser mayor o igual a cero." };

        if (errors.Count > 0)
            return Result<ProductoDto>.ValidationFailure(errors);

        return null;
    }

    private ProductoDto MapToDto(Producto producto)
    {
        var (margenEfectivo, _) = MargenCalculator.ResolverMargenProducto(producto, MargenDefecto);

        return new ProductoDto
        {
            Id = producto.Id,
            Nombre = producto.Nombre,
            CodigoBarras = producto.CodigoBarras,
            Descripcion = producto.Descripcion,
            Precio = producto.Precio,
            PrecioCosto = producto.PrecioCosto,
            MargenGanancia = producto.MargenGanancia,
            MargenEfectivo = margenEfectivo,
            Stock = producto.Stock,
            CategoriaId = producto.CategoriaId,
            CategoriaNombre = producto.Categoria?.Nombre ?? string.Empty,
            Activo = producto.Activo
        };
    }
}
