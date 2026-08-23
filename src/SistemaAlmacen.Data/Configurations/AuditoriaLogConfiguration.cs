using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaAlmacen.Data.Entities;

namespace SistemaAlmacen.Data.Configurations;

public class AuditoriaLogConfiguration : IEntityTypeConfiguration<AuditoriaLog>
{
    public void Configure(EntityTypeBuilder<AuditoriaLog> builder)
    {
        builder.ToTable("AuditoriaLogs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.TipoOperacion)
            .IsRequired();

        builder.Property(a => a.EntidadAfectada)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.RegistroAfectadoId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.Descripcion)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(a => a.Fecha)
            .IsRequired();

        // FK a Usuario con RESTRICT
        builder.HasOne(a => a.Usuario)
            .WithMany(u => u.AuditoriaLogs)
            .HasForeignKey(a => a.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        // Índice en Fecha descendente para consultas eficientes
        builder.HasIndex(a => a.Fecha)
            .IsDescending();
    }
}
