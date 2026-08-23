using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaAlmacen.Data.Entities;

namespace SistemaAlmacen.Data.Configurations;

public class DetalleVentaConfiguration : IEntityTypeConfiguration<DetalleVenta>
{
    public void Configure(EntityTypeBuilder<DetalleVenta> builder)
    {
        builder.ToTable("DetallesVenta");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Cantidad)
            .IsRequired();

        builder.Property(d => d.PrecioUnitario)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(d => d.Subtotal)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        // FK a Venta con CASCADE (configurada desde VentaConfiguration)
        // FK a Producto con RESTRICT
        builder.HasOne(d => d.Producto)
            .WithMany(p => p.DetallesVenta)
            .HasForeignKey(d => d.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
