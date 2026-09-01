using Microsoft.EntityFrameworkCore;
using SistemaAlmacen.Data.Context;
using SistemaAlmacen.Data.Entities;
using SistemaAlmacen.Shared.DTOs;
using SistemaAlmacen.Shared.DTOs.Clientes;
using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Data.Repositories;

/// <summary>
/// Implementación del repositorio de clientes y cuenta corriente.
/// </summary>
public class ClienteRepository : Repository<Cliente>, IClienteRepository
{
    public ClienteRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<PaginatedResult<Cliente>> GetActivosAsync(ClienteFilter filter)
    {
        var query = _dbSet.Where(c => c.Activo);

        if (filter.SoloCuentaCorriente)
        {
            query = query.Where(c => c.CuentaCorrienteHabilitada);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.ToLower();
            query = query.Where(c =>
                c.Nombre.ToLower().Contains(term) ||
                (c.Documento != null && c.Documento.ToLower().Contains(term)) ||
                (c.Email != null && c.Email.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(c => c.Nombre)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PaginatedResult<Cliente>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<Cliente?> GetByDocumentoAsync(string documento)
    {
        return await _dbSet.FirstOrDefaultAsync(c => c.Documento == documento && c.Activo);
    }

    public async Task<bool> ExisteDocumentoActivoAsync(string documento, int? excluirClienteId = null)
    {
        return await _dbSet.AnyAsync(c =>
            c.Documento == documento &&
            c.Activo &&
            (excluirClienteId == null || c.Id != excluirClienteId.Value));
    }

    public async Task<List<MovimientoCuentaCorriente>> GetMovimientosAsync(int clienteId)
    {
        return await _context.Set<MovimientoCuentaCorriente>()
            .Where(m => m.ClienteId == clienteId)
            .OrderByDescending(m => m.Fecha)
            .ThenByDescending(m => m.Id)
            .ToListAsync();
    }

    public async Task<decimal> GetSaldoAsync(int clienteId)
    {
        var movimientos = _context.Set<MovimientoCuentaCorriente>()
            .Where(m => m.ClienteId == clienteId);

        var cargos = await movimientos
            .Where(m => m.Tipo == TipoMovimientoCuentaCorriente.Cargo)
            .SumAsync(m => (decimal?)m.Monto) ?? 0m;

        var creditos = await movimientos
            .Where(m => m.Tipo == TipoMovimientoCuentaCorriente.Pago ||
                        m.Tipo == TipoMovimientoCuentaCorriente.AjusteAnulacion)
            .SumAsync(m => (decimal?)m.Monto) ?? 0m;

        return cargos - creditos;
    }

    public async Task AddMovimientoAsync(MovimientoCuentaCorriente movimiento)
    {
        await _context.Set<MovimientoCuentaCorriente>().AddAsync(movimiento);
    }
}
