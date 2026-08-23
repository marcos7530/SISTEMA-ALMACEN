using Microsoft.EntityFrameworkCore;
using SistemaAlmacen.Data.Context;
using SistemaAlmacen.Data.Entities;
using SistemaAlmacen.Shared.DTOs;
using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Data.Repositories;

/// <summary>
/// Implementación del repositorio de usuarios.
/// </summary>
public class UsuarioRepository : Repository<Usuario>, IUsuarioRepository
{
    public UsuarioRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Usuario?> GetByEmailAsync(string email)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
    }

    public async Task<PaginatedResult<Usuario>> GetActiveUsuariosAsync(string? searchTerm, int page, int pageSize)
    {
        var query = _dbSet.Where(u => u.Activo);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(u =>
                u.Nombre.ToLower().Contains(term) ||
                u.Email.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(u => u.Nombre)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResult<Usuario>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<int> CountActiveAdminsAsync()
    {
        return await _dbSet.CountAsync(u => u.Activo && u.Rol == Rol.Administrador);
    }
}
