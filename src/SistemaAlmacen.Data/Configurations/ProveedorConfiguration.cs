using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaAlmacen.Data.Entities;

namespace SistemaAlmacen.Data.Configurations;

public class ProveedorConfiguration : IEntityTypeConfiguration<Proveedor>
{
    public void Configure(EntityTypeBuilder<Proveedor> builder)
    {
        builder.ToTable("Proveedores");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Nombre)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(p => p.Cuit)
            .HasMaxLength(20);

        builder.Property(p => p.CondicionIva)
            .IsRequired();

        builder.Property(p => p.Email)
            .HasMaxLength(254);

        builder.Property(p => p.Telefono)
            .HasMaxLength(30);

        builder.Property(p => p.Direccion)
            .HasMaxLength(250);

        builder.Property(p => p.Activo)
            .IsRequired();

        builder.Property(p => p.FechaCreacion)
            .IsRequired();

        builder.Property(p => p.FechaModificacion)
            .IsRequired();

        // Índice único filtrado en CUIT (solo entre proveedores activos con CUIT asignado)
        builder.HasIndex(p => p.Cuit)
            .IsUnique()
            .HasFilter("[Cuit] IS NOT NULL AND [Activo] = 1");
    }
}
