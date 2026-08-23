using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SistemaAlmacen.Data.Entities;
using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Data.Context;

/// <summary>
/// Servicio para poblar la base de datos con datos iniciales que requieren procesamiento en runtime
/// (por ejemplo, hashing de contraseñas con BCrypt).
/// </summary>
public static class DataSeeder
{
    /// <summary>
    /// Aplica migraciones pendientes y crea el usuario Administrador inicial si no existe.
    /// </summary>
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            // Aplicar migraciones pendientes
            await context.Database.MigrateAsync();

            // Seed: Usuario Administrador inicial
            if (!await context.Usuarios.AnyAsync(u => u.Rol == Rol.Administrador))
            {
                var adminPasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!");

                var admin = new Usuario
                {
                    Nombre = "Administrador",
                    Email = "admin@sistema.local",
                    PasswordHash = adminPasswordHash,
                    Rol = Rol.Administrador,
                    Activo = true,
                    IntentosFallidos = 0,
                    FechaCreacion = DateTime.UtcNow
                };

                context.Usuarios.Add(admin);
                await context.SaveChangesAsync();

                logger.LogInformation("Usuario Administrador inicial creado exitosamente.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error durante el seed de datos iniciales.");
            throw;
        }
    }
}
