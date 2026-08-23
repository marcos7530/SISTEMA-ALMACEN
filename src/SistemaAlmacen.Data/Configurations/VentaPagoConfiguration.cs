using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaAlmacen.Data.Entities;

namespace SistemaAlmacen.Data.Configurations;

public class VentaPagoConfiguration : IEntityTypeConfiguration<VentaPago>
{
    public void Configure(EntityTypeBuilder<VentaPago> builder)
    {
        builder.ToTable("VentaPagos");

        builder.HasKey(vp => vp.Id);

        builder.Property(vp => vp.Monto)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        // FK a MedioPago con RESTRICT
        builder.HasOne(vp => vp.MedioPago)
            .WithMany(mp => mp.VentaPagos)
            .HasForeignKey(vp => vp.MedioPagoId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK a Venta con CASCADE (configurada desde VentaConfiguration)
    }
}
