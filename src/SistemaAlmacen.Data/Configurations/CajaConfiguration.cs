using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaAlmacen.Data.Entities;

namespace SistemaAlmacen.Data.Configurations;

public class CajaConfiguration : IEntityTypeConfiguration<Caja>
{
    public void Configure(EntityTypeBuilder<Caja> builder)
    {
        builder.ToTable("Cajas");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.PuntoDeVentaId)
            .IsRequired();

        builder.Property(c => c.MontoInicial)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(c => c.MontoRealCierre)
            .HasColumnType("decimal(18,2)");

        builder.Property(c => c.SaldoEsperado)
            .HasColumnType("decimal(18,2)");

        builder.Property(c => c.Diferencia)
            .HasColumnType("decimal(18,2)");

        builder.Property(c => c.Estado)
            .IsRequired();

        builder.Property(c => c.FechaApertura)
            .IsRequired();

        // FK a Usuario Apertura con RESTRICT
        builder.HasOne(c => c.UsuarioApertura)
            .WithMany(u => u.CajasAbiertas)
            .HasForeignKey(c => c.UsuarioAperturaId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK a Usuario Cierre con RESTRICT (opcional)
        builder.HasOne(c => c.UsuarioCierre)
            .WithMany(u => u.CajasCerradas)
            .HasForeignKey(c => c.UsuarioCierreId)
            .OnDelete(DeleteBehavior.Restrict);

        // CASCADE a Movimientos
        builder.HasMany(c => c.Movimientos)
            .WithOne(m => m.Caja)
            .HasForeignKey(m => m.CajaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
