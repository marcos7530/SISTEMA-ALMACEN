using SistemaAlmacen.Business.Interfaces;
using SistemaAlmacen.Data.Entities;
using SistemaAlmacen.Data.Repositories;
using SistemaAlmacen.Shared.Common;
using SistemaAlmacen.Shared.DTOs.MediosPago;
using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Business.Services;

/// <summary>
/// Implementación del servicio de medios de pago con validaciones de negocio.
/// </summary>
public class MedioPagoService : IMedioPagoService
{
    private readonly IUnitOfWork _unitOfWork;

    public MedioPagoService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<List<MedioPagoDto>> GetActivosAsync()
    {
        var mediosPago = await _unitOfWork.MediosPago.GetActivosAsync();

        return mediosPago.Select(MapToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<List<MedioPagoDto>> GetAllAsync()
    {
        var mediosPago = await _unitOfWork.MediosPago.GetAllAsync();

        return mediosPago
            .OrderBy(mp => mp.Nombre)
            .Select(MapToDto)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<Result<MedioPagoDto>> CreateAsync(CreateMedioPagoRequest request)
    {
        var validationResult = ValidateNombre(request.Nombre);
        if (!validationResult.IsSuccess)
            return Result<MedioPagoDto>.Failure(validationResult.ErrorMessage!);

        var nombreTrimmed = request.Nombre.Trim();

        // Verificar unicidad de nombre (case-insensitive, entre TODOS incluyendo inactivos)
        var existing = await _unitOfWork.MediosPago.GetByNameAsync(nombreTrimmed);
        if (existing is not null)
            return Result<MedioPagoDto>.Failure("Ya existe un medio de pago con ese nombre.", "NOMBRE_DUPLICADO");

        var medioPago = new MedioPago
        {
            Nombre = nombreTrimmed,
            Activo = true,
            EsSistema = false
        };

        await _unitOfWork.MediosPago.AddAsync(medioPago);
        await _unitOfWork.SaveChangesAsync();

        return Result<MedioPagoDto>.Success(MapToDto(medioPago));
    }

    /// <inheritdoc />
    public async Task<Result<MedioPagoDto>> UpdateAsync(int id, UpdateMedioPagoRequest request)
    {
        var medioPago = await _unitOfWork.MediosPago.GetByIdAsync(id);
        if (medioPago is null)
            return Result<MedioPagoDto>.Failure("El medio de pago no fue encontrado.", "NO_ENCONTRADO");

        // No permitir modificar medios de sistema
        if (medioPago.EsSistema)
            return Result<MedioPagoDto>.Failure("No se puede modificar un medio de pago del sistema.", "ES_SISTEMA");

        var validationResult = ValidateNombre(request.Nombre);
        if (!validationResult.IsSuccess)
            return Result<MedioPagoDto>.Failure(validationResult.ErrorMessage!);

        var nombreTrimmed = request.Nombre.Trim();

        // Verificar unicidad de nombre excluyendo el actual (case-insensitive, entre TODOS)
        var existing = await _unitOfWork.MediosPago.GetByNameAsync(nombreTrimmed);
        if (existing is not null && existing.Id != id)
            return Result<MedioPagoDto>.Failure("Ya existe un medio de pago con ese nombre.", "NOMBRE_DUPLICADO");

        medioPago.Nombre = nombreTrimmed;
        _unitOfWork.MediosPago.Update(medioPago);
        await _unitOfWork.SaveChangesAsync();

        return Result<MedioPagoDto>.Success(MapToDto(medioPago));
    }

    /// <inheritdoc />
    public async Task<Result> DeactivateAsync(int id)
    {
        var medioPago = await _unitOfWork.MediosPago.GetByIdAsync(id);
        if (medioPago is null)
            return Result.Failure("El medio de pago no fue encontrado.", "NO_ENCONTRADO");

        // No permitir desactivar medios de sistema (Efectivo)
        if (medioPago.EsSistema)
            return Result.Failure("No se puede desactivar un medio de pago del sistema.", "ES_SISTEMA");

        // No permitir desactivar si hay caja abierta
        var hayCajaAbierta = await _unitOfWork.Cajas.AnyAsync(c => c.Estado == EstadoCaja.Abierta);
        if (hayCajaAbierta)
            return Result.Failure("No se puede desactivar un medio de pago mientras hay una caja abierta.", "CAJA_ABIERTA");

        medioPago.Activo = false;
        _unitOfWork.MediosPago.Update(medioPago);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    /// <summary>
    /// Valida el nombre del medio de pago: no vacío, no solo whitespace, entre 3 y 50 caracteres.
    /// </summary>
    private static Result ValidateNombre(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return Result.Failure("El nombre es requerido y no puede ser solo espacios en blanco.");

        var trimmed = nombre.Trim();

        if (trimmed.Length < 3)
            return Result.Failure("El nombre debe tener al menos 3 caracteres.");

        if (trimmed.Length > 50)
            return Result.Failure("El nombre no puede exceder 50 caracteres.");

        return Result.Success();
    }

    /// <summary>
    /// Mapea una entidad MedioPago a MedioPagoDto.
    /// </summary>
    private static MedioPagoDto MapToDto(MedioPago medioPago)
    {
        return new MedioPagoDto
        {
            Id = medioPago.Id,
            Nombre = medioPago.Nombre,
            Activo = medioPago.Activo,
            EsSistema = medioPago.EsSistema
        };
    }
}
