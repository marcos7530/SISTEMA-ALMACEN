using Microsoft.EntityFrameworkCore.Storage;
using SistemaAlmacen.Data.Context;

namespace SistemaAlmacen.Data.Repositories;

/// <summary>
/// Implementación del Unit of Work usando ApplicationDbContext e IDbContextTransaction.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _transaction;

    private IUsuarioRepository? _usuarios;
    private IProductoRepository? _productos;
    private ICategoriaRepository? _categorias;
    private IVentaRepository? _ventas;
    private ICajaRepository? _cajas;
    private IAuditoriaRepository? _auditoria;
    private IMedioPagoRepository? _mediosPago;
    private IComprobanteRepository? _comprobantes;
    private ITokenRecuperacionRepository? _tokensRecuperacion;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IUsuarioRepository Usuarios =>
        _usuarios ??= new UsuarioRepository(_context);

    public IProductoRepository Productos =>
        _productos ??= new ProductoRepository(_context);

    public ICategoriaRepository Categorias =>
        _categorias ??= new CategoriaRepository(_context);

    public IVentaRepository Ventas =>
        _ventas ??= new VentaRepository(_context);

    public ICajaRepository Cajas =>
        _cajas ??= new CajaRepository(_context);

    public IAuditoriaRepository Auditoria =>
        _auditoria ??= new AuditoriaRepository(_context);

    public IMedioPagoRepository MediosPago =>
        _mediosPago ??= new MedioPagoRepository(_context);

    public IComprobanteRepository Comprobantes =>
        _comprobantes ??= new ComprobanteRepository(_context);

    public ITokenRecuperacionRepository TokensRecuperacion =>
        _tokensRecuperacion ??= new TokenRecuperacionRepository(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
