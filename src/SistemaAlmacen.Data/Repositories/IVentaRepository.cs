using SistemaAlmacen.Data.Entities;
using SistemaAlmacen.Shared.DTOs;
using SistemaAlmacen.Shared.DTOs.Ventas;

namespace SistemaAlmacen.Data.Repositories;

/// <summary>
/// Repositorio específico para la entidad Venta.
/// </summary>
public interface IVentaRepository : IRepository<Venta>
{
    Task<Venta?> GetVentaConDetallesAsync(int id);
    Task<PaginatedResult<Venta>> GetHistorialAsync(VentaFilter filter);
}
