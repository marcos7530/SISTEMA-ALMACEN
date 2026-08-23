using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaAlmacen.Data.Entities;

namespace SistemaAlmacen.Data.Configurations;

public class ComprobanteConfiguration : IEntityTypeConfiguration<Comprobante>
{
    public void Configure(EntityTypeBuilder<Comprobante> builder)
    {
        builder.ToTable("Comprobantes");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.TipoComprobante)
            .IsRequired();

        builder.Property(c => c.NumeroComprobante)
            .IsRequired();

        builder.Property(c => c.CAE)
            .HasMaxLength(14);

        builder.Property(c => c.Estado)
            .IsRequired();

        builder.Property(c => c.ErrorDetalle)
            .HasMaxLength(500);

        builder.Property(c => c.FechaEmision)
            .IsRequired();

        // Relación uno a uno con Venta, cascade delete
        builder.HasOne(c => c.Venta)
            .WithOne(v => v.Comprobante)
            .HasForeignKey<Comprobante>(c => c.VentaId)
            .OnDelete(DeleteBehavior.Cascade);

        // Índice único en VentaId (relación 1:1)
        builder.HasIndex(c => c.VentaId)
            .IsUnique();
    }
}
