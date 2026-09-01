using SistemaAlmacen.Business.Interfaces;
using SistemaAlmacen.Data.Entities;
using SistemaAlmacen.Data.Repositories;
using SistemaAlmacen.Shared.Common;
using SistemaAlmacen.Shared.DTOs;
using SistemaAlmacen.Shared.DTOs.Proveedores;

namespace SistemaAlmacen.Business.Services;

/// <summary>
/// Implementación del servicio de proveedores con validaciones de negocio.
/// </summary>
public class ProveedorService : IProveedorService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProveedorService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<PaginatedResult<ProveedorDto>> GetProveedoresAsync(ProveedorFilter filter)
    {
        var result = await _unitOfWork.Proveedores.GetActivosAsync(filter);

        return new PaginatedResult<ProveedorDto>
        {
            Items = result.Items.Select(MapToDto).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
    }

    /// <inheritdoc />
    public async Task<ProveedorDto?> GetByIdAsync(int id)
    {
        var proveedor = await _unitOfWork.Proveedores.GetByIdAsync(id);

        if (proveedor is null || !proveedor.Activo)
            return null;

        return MapToDto(proveedor);
    }

    /// <inheritdoc />
    public async Task<Result<ProveedorDto>> CreateAsync(CreateProveedorRequest request)
    {
        var validationResult = ValidateRequest(request.Nombre, request.Email);
        if (validationResult is not null)
            return validationResult;

        var cuit = string.IsNullOrWhiteSpace(request.Cuit) ? null : request.Cuit.Trim();

        if (cuit is not null && await _unitOfWork.Proveedores.ExisteCuitActivoAsync(cuit))
            return Result<ProveedorDto>.Failure("Ya existe un proveedor activo con ese CUIT.", "CUIT_DUPLICADO");

        var now = DateTime.UtcNow;
        var proveedor = new Proveedor
        {
            Nombre = request.Nombre.Trim(),
            Cuit = cuit,
            CondicionIva = request.CondicionIva,
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            Telefono = string.IsNullOrWhiteSpace(request.Telefono) ? null : request.Telefono.Trim(),
            Direccion = string.IsNullOrWhiteSpace(request.Direccion) ? null : request.Direccion.Trim(),
            Activo = true,
            FechaCreacion = now,
            FechaModificacion = now
        };

        await _unitOfWork.Proveedores.AddAsync(proveedor);
        await _unitOfWork.SaveChangesAsync();

        return Result<ProveedorDto>.Success(MapToDto(proveedor));
    }

    /// <inheritdoc />
    public async Task<Result<ProveedorDto>> UpdateAsync(int id, UpdateProveedorRequest request)
    {
        var proveedor = await _unitOfWork.Proveedores.GetByIdAsync(id);

        if (proveedor is null || !proveedor.Activo)
            return Result<ProveedorDto>.Failure("El proveedor no fue encontrado.", "NOT_FOUND");

        var validationResult = ValidateRequest(request.Nombre, request.Email);
        if (validationResult is not null)
            return validationResult;

        var cuit = string.IsNullOrWhiteSpace(request.Cuit) ? null : request.Cuit.Trim();

        if (cuit is not null && await _unitOfWork.Proveedores.ExisteCuitActivoAsync(cuit, id))
            return Result<ProveedorDto>.Failure("Ya existe un proveedor activo con ese CUIT.", "CUIT_DUPLICADO");

        proveedor.Nombre = request.Nombre.Trim();
        proveedor.Cuit = cuit;
        proveedor.CondicionIva = request.CondicionIva;
        proveedor.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        proveedor.Telefono = string.IsNullOrWhiteSpace(request.Telefono) ? null : request.Telefono.Trim();
        proveedor.Direccion = string.IsNullOrWhiteSpace(request.Direccion) ? null : request.Direccion.Trim();
        proveedor.FechaModificacion = DateTime.UtcNow;

        _unitOfWork.Proveedores.Update(proveedor);
        await _unitOfWork.SaveChangesAsync();

        return Result<ProveedorDto>.Success(MapToDto(proveedor));
    }

    /// <inheritdoc />
    public async Task<Result> DeactivateAsync(int id)
    {
        var proveedor = await _unitOfWork.Proveedores.GetByIdAsync(id);

        if (proveedor is null || !proveedor.Activo)
            return Result.Failure("El proveedor no fue encontrado.", "NOT_FOUND");

        proveedor.Activo = false;
        proveedor.FechaModificacion = DateTime.UtcNow;

        _unitOfWork.Proveedores.Update(proveedor);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    private static Result<ProveedorDto>? ValidateRequest(string nombre, string? email)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(nombre))
            errors["Nombre"] = new[] { "El nombre es requerido." };
        else if (nombre.Trim().Length > 150)
            errors["Nombre"] = new[] { "El nombre no puede exceder 150 caracteres." };

        if (!string.IsNullOrWhiteSpace(email) && email.Trim().Length > 254)
            errors["Email"] = new[] { "El correo no puede exceder 254 caracteres." };

        if (errors.Count > 0)
            return Result<ProveedorDto>.ValidationFailure(errors);

        return null;
    }

    private static ProveedorDto MapToDto(Proveedor proveedor)
    {
        return new ProveedorDto
        {
            Id = proveedor.Id,
            Nombre = proveedor.Nombre,
            Cuit = proveedor.Cuit,
            CondicionIva = proveedor.CondicionIva,
            Email = proveedor.Email,
            Telefono = proveedor.Telefono,
            Direccion = proveedor.Direccion,
            Activo = proveedor.Activo
        };
    }
}
