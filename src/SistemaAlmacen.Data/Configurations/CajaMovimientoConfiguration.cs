using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaAlmacen.Data.Entities;

namespace SistemaAlmacen.Data.Configurations;

public class CajaMovimientoConfiguration : IEntityTypeConfiguration<CajaMovimiento>
{
    public void Configure(EntityTypeBuilder<CajaMovimiento> builder)
    {
        builder.ToTable("CajaMovimientos");

        builder.HasKey(cm => cm.Id);

        builder.Property(cm => cm.Tipo)
            .IsRequired();

        builder.Property(cm => cm.Monto)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(cm => cm.Motivo)
            .HasMaxLength(200);

        builder.Property(cm => cm.Fecha)
            .IsRequired();

        // FK a Caja con CASCADE (configurada desde CajaConfiguration)

        // FK a Usuario con RESTRICT
        builder.HasOne(cm => cm.Usuario)
            .WithMany(u => u.CajaMovimientos)
            .HasForeignKey(cm => cm.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
