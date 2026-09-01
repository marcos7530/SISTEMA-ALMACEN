using SistemaAlmacen.Shared.Common;
using SistemaAlmacen.Shared.DTOs;
using SistemaAlmacen.Shared.DTOs.Proveedores;

namespace SistemaAlmacen.Business.Interfaces;

/// <summary>
/// Servicio de lógica de negocio para la gestión de proveedores.
/// </summary>
public interface IProveedorService
{
    Task<PaginatedResult<ProveedorDto>> GetProveedoresAsync(ProveedorFilter filter);
    Task<ProveedorDto?> GetByIdAsync(int id);
    Task<Result<ProveedorDto>> CreateAsync(CreateProveedorRequest request);
    Task<Result<ProveedorDto>> UpdateAsync(int id, UpdateProveedorRequest request);
    Task<Result> DeactivateAsync(int id);
}
