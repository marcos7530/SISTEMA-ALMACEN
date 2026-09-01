using Microsoft.EntityFrameworkCore;
using SistemaAlmacen.Data.Entities;

namespace SistemaAlmacen.Data.Context;

/// <summary>
/// Contexto principal de Entity Framework para el Sistema de Punto de Venta.
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<DetalleVenta> DetallesVenta => Set<DetalleVenta>();
    public DbSet<VentaPago> VentaPagos => Set<VentaPago>();
    public DbSet<MedioPago> MediosPago => Set<MedioPago>();
    public DbSet<Comprobante> Comprobantes => Set<Comprobante>();
    public DbSet<Caja> Cajas => Set<Caja>();
    public DbSet<CajaMovimiento> CajaMovimientos => Set<CajaMovimiento>();
    public DbSet<MovimientoStock> MovimientosStock => Set<MovimientoStock>();
    public DbSet<AuditoriaLog> AuditoriaLogs => Set<AuditoriaLog>();
    public DbSet<TokenRecuperacion> TokensRecuperacion => Set<TokenRecuperacion>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<MovimientoCuentaCorriente> MovimientosCuentaCorriente => Set<MovimientoCuentaCorriente>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Aplicar todas las configuraciones definidas en el assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
