using SistemaAlmacen.Shared.Common;
using SistemaAlmacen.Shared.DTOs.MediosPago;

namespace SistemaAlmacen.Business.Interfaces;

/// <summary>
/// Servicio de lógica de negocio para gestión de medios de pago.
/// </summary>
public interface IMedioPagoService
{
    /// <summary>
    /// Obtiene todos los medios de pago activos.
    /// </summary>
    Task<List<MedioPagoDto>> GetActivosAsync();

    /// <summary>
    /// Obtiene todos los medios de pago (activos e inactivos) para vista de administración.
    /// </summary>
    Task<List<MedioPagoDto>> GetAllAsync();

    /// <summary>
    /// Crea un nuevo medio de pago validando nombre (3-50 chars, no whitespace) y unicidad (case-insensitive entre todos).
    /// </summary>
    Task<Result<MedioPagoDto>> CreateAsync(CreateMedioPagoRequest request);

    /// <summary>
    /// Actualiza un medio de pago existente. No permite modificar medios de sistema (EsSistema=true).
    /// </summary>
    Task<Result<MedioPagoDto>> UpdateAsync(int id, UpdateMedioPagoRequest request);

    /// <summary>
    /// Desactiva un medio de pago. No permite desactivar medios de sistema ni si hay caja abierta.
    /// </summary>
    Task<Result> DeactivateAsync(int id);
}
