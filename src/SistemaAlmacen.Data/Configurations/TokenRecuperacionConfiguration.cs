using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaAlmacen.Data.Entities;

namespace SistemaAlmacen.Data.Configurations;

public class TokenRecuperacionConfiguration : IEntityTypeConfiguration<TokenRecuperacion>
{
    public void Configure(EntityTypeBuilder<TokenRecuperacion> builder)
    {
        builder.ToTable("TokensRecuperacion");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Token)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.Usado)
            .IsRequired();

        builder.Property(t => t.FechaCreacion)
            .IsRequired();

        builder.Property(t => t.FechaExpiracion)
            .IsRequired();

        // FK a Usuario con CASCADE
        builder.HasOne(t => t.Usuario)
            .WithMany(u => u.TokensRecuperacion)
            .HasForeignKey(t => t.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        // Índice único en Token
        builder.HasIndex(t => t.Token)
            .IsUnique();
    }
}
