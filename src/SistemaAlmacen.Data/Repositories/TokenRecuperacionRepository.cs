using Microsoft.EntityFrameworkCore;
using SistemaAlmacen.Data.Context;
using SistemaAlmacen.Data.Entities;

namespace SistemaAlmacen.Data.Repositories;

/// <summary>
/// Implementación del repositorio de tokens de recuperación.
/// </summary>
public class TokenRecuperacionRepository : Repository<TokenRecuperacion>, ITokenRecuperacionRepository
{
    public TokenRecuperacionRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<TokenRecuperacion?> GetByTokenAsync(string token)
    {
        return await _dbSet
            .Include(t => t.Usuario)
            .FirstOrDefaultAsync(t => t.Token == token);
    }

    public async Task InvalidateTokensByUsuarioIdAsync(int usuarioId)
    {
        var activeTokens = await _dbSet
            .Where(t => t.UsuarioId == usuarioId && !t.Usado)
            .ToListAsync();

        foreach (var token in activeTokens)
        {
            token.Usado = true;
        }
    }
}
