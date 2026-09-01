using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaAlmacen.Data.Entities;

namespace SistemaAlmacen.Data.Configurations;

public class MovimientoCuentaCorrienteConfiguration : IEntityTypeConfiguration<MovimientoCuentaCorriente>
{
    public void Configure(EntityTypeBuilder<MovimientoCuentaCorriente> builder)
    {
        builder.ToTable("MovimientosCuentaCorriente");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Tipo)
            .IsRequired();

        builder.Property(m => m.Monto)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(m => m.Descripcion)
            .HasMaxLength(250);

        builder.Property(m => m.Fecha)
            .IsRequired();

        // FK a Cliente con RESTRICT (no se borra un cliente con movimientos)
        builder.HasOne(m => m.Cliente)
            .WithMany(c => c.MovimientosCuentaCorriente)
            .HasForeignKey(m => m.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK opcional a Venta con RESTRICT
        builder.HasOne(m => m.Venta)
            .WithMany(v => v.MovimientosCuentaCorriente)
            .HasForeignKey(m => m.VentaId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK a Usuario con RESTRICT
        builder.HasOne(m => m.Usuario)
            .WithMany()
            .HasForeignKey(m => m.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => m.ClienteId);
    }
}
