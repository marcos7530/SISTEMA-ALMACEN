using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaAlmacen.Data.Entities;

namespace SistemaAlmacen.Data.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Nombre)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(254);

        builder.Property(u => u.PasswordHash)
            .IsRequired();

        builder.Property(u => u.Rol)
            .IsRequired();

        builder.Property(u => u.Activo)
            .IsRequired();

        builder.Property(u => u.IntentosFallidos)
            .IsRequired();

        builder.Property(u => u.FechaCreacion)
            .IsRequired();

        // Índice único en Email solo para usuarios activos
        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasFilter("[Activo] = 1");
    }
}
