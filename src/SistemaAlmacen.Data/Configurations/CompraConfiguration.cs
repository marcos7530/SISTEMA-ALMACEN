using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaAlmacen.Data.Entities;

namespace SistemaAlmacen.Data.Configurations;

public class CompraConfiguration : IEntityTypeConfiguration<Compra>
{
    public void Configure(EntityTypeBuilder<Compra> builder)
    {
        builder.ToTable("Compras");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.NumeroComprobante)
            .HasMaxLength(50);

        builder.Property(c => c.Total)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(c => c.Estado)
            .IsRequired();

        builder.Property(c => c.Fecha)
            .IsRequired();

        builder.Property(c => c.FechaCreacion)
            .IsRequired();

        // FK a Proveedor con RESTRICT
        builder.HasOne(c => c.Proveedor)
            .WithMany(p => p.Compras)
            .HasForeignKey(c => c.ProveedorId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK a Usuario con RESTRICT
        builder.HasOne(c => c.Usuario)
            .WithMany()
            .HasForeignKey(c => c.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Detalles con CASCADE
        builder.HasMany(c => c.Detalles)
            .WithOne(d => d.Compra)
            .HasForeignKey(d => d.CompraId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
