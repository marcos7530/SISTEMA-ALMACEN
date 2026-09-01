using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaAlmacen.Data.Entities;

namespace SistemaAlmacen.Data.Configurations;

public class MovimientoStockConfiguration : IEntityTypeConfiguration<MovimientoStock>
{
    public void Configure(EntityTypeBuilder<MovimientoStock> builder)
    {
        builder.ToTable("MovimientosStock");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Tipo)
            .IsRequired();

        builder.Property(m => m.Cantidad)
            .IsRequired();

        builder.Property(m => m.StockAnterior)
            .IsRequired();

        builder.Property(m => m.StockNuevo)
            .IsRequired();

        builder.Property(m => m.Observacion)
            .HasMaxLength(200);

        builder.Property(m => m.Fecha)
            .IsRequired();

        builder.HasIndex(m => m.ProductoId);
        builder.HasIndex(m => m.Fecha);

        // FK a Producto con RESTRICT (no borrar productos con movimientos)
        builder.HasOne(m => m.Producto)
            .WithMany()
            .HasForeignKey(m => m.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK a Usuario con RESTRICT
        builder.HasOne(m => m.Usuario)
            .WithMany()
            .HasForeignKey(m => m.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK opcional a Compra con RESTRICT
        builder.HasOne(m => m.Compra)
            .WithMany()
            .HasForeignKey(m => m.CompraId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
