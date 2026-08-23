using SistemaAlmacen.Data.Entities;
using SistemaAlmacen.Shared.DTOs;
using SistemaAlmacen.Shared.DTOs.Caja;

namespace SistemaAlmacen.Data.Repositories;

/// <summary>
/// Repositorio específico para la entidad Caja.
/// </summary>
public interface ICajaRepository : IRepository<Caja>
{
    Task<Caja?> GetCajaAbiertaAsync(int puntoDeVentaId);
    Task<PaginatedResult<Caja>> GetHistorialCierresAsync(CierreFilter filter);
    Task AddMovimientoAsync(int cajaId, CajaMovimiento movimiento);
}
