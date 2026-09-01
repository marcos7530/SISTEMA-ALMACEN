using SistemaAlmacen.Data.Entities;
using SistemaAlmacen.Shared.DTOs;
using SistemaAlmacen.Shared.DTOs.Proveedores;

namespace SistemaAlmacen.Data.Repositories;

/// <summary>
/// Repositorio específico para la entidad Proveedor.
/// </summary>
public interface IProveedorRepository : IRepository<Proveedor>
{
    Task<PaginatedResult<Proveedor>> GetActivosAsync(ProveedorFilter filter);
    Task<bool> ExisteCuitActivoAsync(string cuit, int? excluirProveedorId = null);
    Task<bool> TieneComprasAsync(int proveedorId);
}
