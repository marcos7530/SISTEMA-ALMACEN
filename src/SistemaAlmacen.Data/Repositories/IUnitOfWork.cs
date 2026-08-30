namespace SistemaAlmacen.Data.Repositories;

/// <summary>
/// Unit of Work pattern para coordinar transacciones entre múltiples repositorios.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IUsuarioRepository Usuarios { get; }
    IProductoRepository Productos { get; }
    ICategoriaRepository Categorias { get; }
    IVentaRepository Ventas { get; }
    ICajaRepository Cajas { get; }
    IAuditoriaRepository Auditoria { get; }
    IMedioPagoRepository MediosPago { get; }
    IComprobanteRepository Comprobantes { get; }
    ITokenRecuperacionRepository TokensRecuperacion { get; }
    IMovimientoStockRepository MovimientosStock { get; }

    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
