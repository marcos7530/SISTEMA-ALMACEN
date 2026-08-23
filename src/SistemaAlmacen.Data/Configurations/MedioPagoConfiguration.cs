using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaAlmacen.Data.Entities;

namespace SistemaAlmacen.Data.Configurations;

public class MedioPagoConfiguration : IEntityTypeConfiguration<MedioPago>
{
    public void Configure(EntityTypeBuilder<MedioPago> builder)
    {
        builder.ToTable("MediosPago");

        builder.HasKey(mp => mp.Id);

        builder.Property(mp => mp.Nombre)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(mp => mp.Activo)
            .IsRequired();

        builder.Property(mp => mp.EsSistema)
            .IsRequired();

        // Índice único en Nombre
        builder.HasIndex(mp => mp.Nombre)
            .IsUnique();

        // Seed: Medios de pago predeterminados
        builder.HasData(
            new MedioPago { Id = 1, Nombre = "Efectivo", Activo = true, EsSistema = true },
            new MedioPago { Id = 2, Nombre = "Tarjeta de Débito", Activo = true, EsSistema = false },
            new MedioPago { Id = 3, Nombre = "Tarjeta de Crédito", Activo = true, EsSistema = false },
            new MedioPago { Id = 4, Nombre = "Transferencia Bancaria", Activo = true, EsSistema = false }
        );
    }
}
