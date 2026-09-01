using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaAlmacen.Data.Entities;

namespace SistemaAlmacen.Data.Configurations;

public class CategoriaConfiguration : IEntityTypeConfiguration<Categoria>
{
    public void Configure(EntityTypeBuilder<Categoria> builder)
    {
        builder.ToTable("Categorias");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Nombre)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Descripcion)
            .HasMaxLength(200);

        builder.Property(c => c.MargenGanancia)
            .HasColumnType("decimal(5,2)");

        builder.Property(c => c.FechaCreacion)
            .IsRequired();

        // Índice único en Nombre
        builder.HasIndex(c => c.Nombre)
            .IsUnique();
    }
}
