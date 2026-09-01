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

        // Autorreferencia opcional: nota de crédito -> factura original
        builder.HasOne(c => c.ComprobanteAsociado)
            .WithMany()
            .HasForeignKey(c => c.ComprobanteAsociadoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relación uno a muchos con Venta (factura + eventuales notas de crédito), cascade delete
        builder.HasOne(c => c.Venta)
            .WithMany(v => v.Comprobantes)
            .HasForeignKey(c => c.VentaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.VentaId);
    }
}
