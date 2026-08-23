using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaAlmacen.Data.Entities;

namespace SistemaAlmacen.Data.Configurations;

public class VentaConfiguration : IEntityTypeConfiguration<Venta>
{
    public void Configure(EntityTypeBuilder<Venta> builder)
    {
        builder.ToTable("Ventas");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Fecha)
            .IsRequired();

        builder.Property(v => v.Total)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(v => v.Estado)
            .IsRequired();

        builder.Property(v => v.FechaCreacion)
            .IsRequired();

        // FK a Usuario con RESTRICT
        builder.HasOne(v => v.Usuario)
            .WithMany(u => u.Ventas)
            .HasForeignKey(v => v.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        // CASCADE a Detalles
        builder.HasMany(v => v.Detalles)
            .WithOne(d => d.Venta)
            .HasForeignKey(d => d.VentaId)
            .OnDelete(DeleteBehavior.Cascade);

        // CASCADE a Pagos
        builder.HasMany(v => v.Pagos)
            .WithOne(p => p.Venta)
            .HasForeignKey(p => p.VentaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
