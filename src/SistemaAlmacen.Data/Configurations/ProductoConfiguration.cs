using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaAlmacen.Data.Entities;

namespace SistemaAlmacen.Data.Configurations;

public class ProductoConfiguration : IEntityTypeConfiguration<Producto>
{
    public void Configure(EntityTypeBuilder<Producto> builder)
    {
        builder.ToTable("Productos");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Nombre)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.CodigoBarras)
            .HasMaxLength(50);

        // Índice único filtrado en CodigoBarras (solo entre productos activos con código asignado)
        builder.HasIndex(p => p.CodigoBarras)
            .IsUnique()
            .HasFilter("[CodigoBarras] IS NOT NULL AND [Activo] = 1");

        builder.Property(p => p.Descripcion)
            .HasMaxLength(500);

        builder.Property(p => p.Precio)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.Stock)
            .IsRequired();

        builder.Property(p => p.Activo)
            .IsRequired();

        builder.Property(p => p.FechaCreacion)
            .IsRequired();

        builder.Property(p => p.FechaModificacion)
            .IsRequired();

        // FK a Categoria con RESTRICT
        builder.HasOne(p => p.Categoria)
            .WithMany(c => c.Productos)
            .HasForeignKey(p => p.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
