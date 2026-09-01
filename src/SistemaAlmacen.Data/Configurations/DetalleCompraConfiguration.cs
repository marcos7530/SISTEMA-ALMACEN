using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaAlmacen.Data.Entities;

namespace SistemaAlmacen.Data.Configurations;

public class DetalleCompraConfiguration : IEntityTypeConfiguration<DetalleCompra>
{
    public void Configure(EntityTypeBuilder<DetalleCompra> builder)
    {
        builder.ToTable("DetallesCompra");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Cantidad)
            .IsRequired();

        builder.Property(d => d.CostoUnitario)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(d => d.Subtotal)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        // FK a Producto con RESTRICT (la FK a Compra se define desde CompraConfiguration)
        builder.HasOne(d => d.Producto)
            .WithMany()
            .HasForeignKey(d => d.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
