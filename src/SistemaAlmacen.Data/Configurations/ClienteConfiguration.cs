using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaAlmacen.Data.Entities;

namespace SistemaAlmacen.Data.Configurations;

public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("Clientes");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Nombre)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(c => c.Documento)
            .HasMaxLength(20);

        builder.Property(c => c.CondicionIva)
            .IsRequired();

        builder.Property(c => c.Email)
            .HasMaxLength(254);

        builder.Property(c => c.Telefono)
            .HasMaxLength(30);

        builder.Property(c => c.Direccion)
            .HasMaxLength(250);

        builder.Property(c => c.CuentaCorrienteHabilitada)
            .IsRequired();

        builder.Property(c => c.LimiteCredito)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(c => c.Activo)
            .IsRequired();

        builder.Property(c => c.FechaCreacion)
            .IsRequired();

        builder.Property(c => c.FechaModificacion)
            .IsRequired();

        // Índice único filtrado en Documento (solo entre clientes activos con documento asignado)
        builder.HasIndex(c => c.Documento)
            .IsUnique()
            .HasFilter("[Documento] IS NOT NULL AND [Activo] = 1");
    }
}
