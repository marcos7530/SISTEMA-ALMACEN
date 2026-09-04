using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SistemaAlmacen.Data.Entities;
using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Data.Context;

/// <summary>
/// Servicio para poblar la base de datos con datos iniciales y datos de prueba.
/// </summary>
public static class DataSeeder
{
    /// <summary>
    /// Aplica migraciones pendientes y crea datos iniciales + datos de prueba.
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

            // Seed: Datos de prueba
            await SeedTestDataAsync(context, logger);

            // Seed idempotente de clientes y proveedores (corre también en bases ya existentes)
            await SeedClientesYProveedoresAsync(context, logger);

            // Seed idempotente de ventas fiadas (cuenta corriente) para pruebas
            await SeedVentasFiadasAsync(context, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error durante el seed de datos iniciales.");
            throw;
        }
    }

    /// <summary>
    /// Siembra clientes y proveedores de prueba de forma idempotente: solo agrega los que
    /// aún no existen (por documento/CUIT o nombre), sin depender del resto de datos de prueba.
    /// Esto permite poblarlos también en bases de datos que ya tenían información cargada.
    /// </summary>
    private static async Task SeedClientesYProveedoresAsync(ApplicationDbContext context, ILogger logger)
    {
        var now = DateTime.UtcNow;

        // ── Clientes de prueba ──
        var clientesSeed = new List<Cliente>
        {
            new() { Nombre = "Consumidor Final", Documento = null, CondicionIva = CondicionIva.ConsumidorFinal, CuentaCorrienteHabilitada = false, LimiteCredito = 0m, Activo = true, FechaCreacion = now, FechaModificacion = now },
            new() { Nombre = "Kiosco El Sol", Documento = "30712345678", CondicionIva = CondicionIva.ResponsableInscripto, Email = "elsol@mail.com", Telefono = "1145678900", Direccion = "Av. Siempreviva 742", CuentaCorrienteHabilitada = true, LimiteCredito = 100000m, Activo = true, FechaCreacion = now, FechaModificacion = now },
            new() { Nombre = "Juan Pérez", Documento = "20304050607", CondicionIva = CondicionIva.Monotributista, Email = "juanperez@mail.com", Telefono = "1156781234", CuentaCorrienteHabilitada = true, LimiteCredito = 0m, Activo = true, FechaCreacion = now, FechaModificacion = now },
            new() { Nombre = "Almacén Doña Rosa", Documento = "27285647389", CondicionIva = CondicionIva.Monotributista, Email = "donarosa@mail.com", Telefono = "1167894561", Direccion = "Belgrano 1234", CuentaCorrienteHabilitada = true, LimiteCredito = 50000m, Activo = true, FechaCreacion = now, FechaModificacion = now },
        };

        var clientesAgregados = 0;
        foreach (var cliente in clientesSeed)
        {
            bool existe = cliente.Documento is not null
                ? await context.Clientes.AnyAsync(c => c.Documento == cliente.Documento)
                : await context.Clientes.AnyAsync(c => c.Nombre == cliente.Nombre);

            if (!existe)
            {
                context.Clientes.Add(cliente);
                clientesAgregados++;
            }
        }

        // ── Proveedores de prueba ──
        var proveedoresSeed = new List<Proveedor>
        {
            new() { Nombre = "Distribuidora Central S.A.", Cuit = "30707070701", CondicionIva = CondicionIva.ResponsableInscripto, Email = "ventas@distcentral.com", Telefono = "1143210000", Direccion = "Parque Industrial 100", Activo = true, FechaCreacion = now, FechaModificacion = now },
            new() { Nombre = "Mayorista del Barrio", Cuit = "30808080802", CondicionIva = CondicionIva.ResponsableInscripto, Email = "pedidos@mayoristabarrio.com", Telefono = "1149998888", Direccion = "Calle Comercio 500", Activo = true, FechaCreacion = now, FechaModificacion = now },
            new() { Nombre = "Lácteos del Sur", Cuit = "30909090903", CondicionIva = CondicionIva.Monotributista, Email = "contacto@lacteossur.com", Telefono = "1152223344", Activo = true, FechaCreacion = now, FechaModificacion = now },
            new() { Nombre = "Bebidas y Más", Cuit = "30611223344", CondicionIva = CondicionIva.ResponsableInscripto, Email = "info@bebidasymas.com", Telefono = "1144556677", Direccion = "Ruta 8 Km 45", Activo = true, FechaCreacion = now, FechaModificacion = now },
        };

        var proveedoresAgregados = 0;
        foreach (var proveedor in proveedoresSeed)
        {
            bool existe = proveedor.Cuit is not null
                ? await context.Proveedores.AnyAsync(p => p.Cuit == proveedor.Cuit)
                : await context.Proveedores.AnyAsync(p => p.Nombre == proveedor.Nombre);

            if (!existe)
            {
                context.Proveedores.Add(proveedor);
                proveedoresAgregados++;
            }
        }

        if (clientesAgregados > 0 || proveedoresAgregados > 0)
        {
            await context.SaveChangesAsync();
            logger.LogInformation(
                "Seed idempotente: se agregaron {Clientes} cliente(s) y {Proveedores} proveedor(es) de prueba.",
                clientesAgregados, proveedoresAgregados);
        }
    }

    /// <summary>
    /// Siembra ventas "fiadas" (a cuenta corriente) de forma idempotente para poder probar
    /// el módulo de cuentas corrientes. Solo se ejecuta si aún no existen movimientos de
    /// cuenta corriente, y depende de que existan clientes con CC habilitada, productos y
    /// al menos un usuario. El saldo del cliente se deriva de estos movimientos
    /// (Cargo por venta a crédito, Pago por cobro parcial).
    /// </summary>
    private static async Task SeedVentasFiadasAsync(ApplicationDbContext context, ILogger logger)
    {
        // Idempotencia: si ya hay movimientos de cuenta corriente, no hacer nada.
        if (await context.Set<MovimientoCuentaCorriente>().AnyAsync())
        {
            return;
        }

        // Necesitamos clientes con cuenta corriente habilitada.
        var clientesCC = await context.Clientes
            .Where(c => c.CuentaCorrienteHabilitada && c.Activo)
            .OrderBy(c => c.Id)
            .ToListAsync();

        if (clientesCC.Count == 0)
        {
            logger.LogInformation("Seed ventas fiadas: no hay clientes con cuenta corriente habilitada. Se omite.");
            return;
        }

        // Necesitamos algunos productos para armar los detalles.
        var productos = await context.Productos
            .Where(p => p.Activo)
            .OrderBy(p => p.Id)
            .Take(6)
            .ToListAsync();

        if (productos.Count == 0)
        {
            logger.LogInformation("Seed ventas fiadas: no hay productos cargados. Se omite.");
            return;
        }

        // Un usuario que registra las ventas (preferimos un vendedor, si no el admin).
        var usuario = await context.Usuarios.FirstOrDefaultAsync(u => u.Rol == Rol.Vendedor && u.Activo)
                      ?? await context.Usuarios.FirstOrDefaultAsync(u => u.Activo);

        if (usuario is null)
        {
            logger.LogInformation("Seed ventas fiadas: no hay usuarios cargados. Se omite.");
            return;
        }

        var now = DateTime.UtcNow;
        const int medioPagoCuentaCorriente = 5; // "Cuenta Corriente" (EsCuentaCorriente = true)

        var ventas = new List<Venta>();
        var detalles = new List<DetalleVenta>();
        var pagos = new List<VentaPago>();
        var movimientos = new List<MovimientoCuentaCorriente>();

        // Genera una venta fiada completa: Venta + Detalles + Pago (cuenta corriente) + Cargo en CC.
        void CrearVentaFiada(Cliente cliente, DateTime fecha, params (Producto producto, int cantidad)[] items)
        {
            var total = items.Sum(i => i.producto.Precio * i.cantidad);

            var venta = new Venta
            {
                UsuarioId = usuario!.Id,
                ClienteId = cliente.Id,
                Fecha = fecha,
                Total = total,
                Estado = EstadoVenta.Confirmada,
                FechaCreacion = fecha
            };
            ventas.Add(venta);

            foreach (var (producto, cantidad) in items)
            {
                detalles.Add(new DetalleVenta
                {
                    Venta = venta,
                    ProductoId = producto.Id,
                    Cantidad = cantidad,
                    PrecioUnitario = producto.Precio,
                    Subtotal = producto.Precio * cantidad
                });
            }

            pagos.Add(new VentaPago
            {
                Venta = venta,
                MedioPagoId = medioPagoCuentaCorriente,
                Monto = total
            });

            movimientos.Add(new MovimientoCuentaCorriente
            {
                ClienteId = cliente.Id,
                Venta = venta,
                Tipo = TipoMovimientoCuentaCorriente.Cargo,
                Monto = total,
                UsuarioId = usuario.Id,
                Descripcion = "Venta a crédito (fiado) de prueba",
                Fecha = fecha
            });
        }

        // Registra un pago (cobro) parcial en la cuenta corriente de un cliente.
        void CrearPago(Cliente cliente, decimal monto, DateTime fecha, string descripcion)
        {
            if (monto <= 0) return;
            movimientos.Add(new MovimientoCuentaCorriente
            {
                ClienteId = cliente.Id,
                Tipo = TipoMovimientoCuentaCorriente.Pago,
                Monto = monto,
                UsuarioId = usuario!.Id,
                Descripcion = descripcion,
                Fecha = fecha
            });
        }

        Producto P(int i) => productos[i % productos.Count];

        // ── Escenario 1: cliente con varias ventas y un pago parcial (saldo deudor intermedio) ──
        var clienteA = clientesCC[0];
        CrearVentaFiada(clienteA, now.AddDays(-12).Date.AddHours(11), (P(0), 3), (P(1), 2));
        CrearVentaFiada(clienteA, now.AddDays(-6).Date.AddHours(16), (P(2), 1), (P(3), 4));
        var totalClienteA = movimientos.Where(m => m.ClienteId == clienteA.Id).Sum(m => m.Monto);
        CrearPago(clienteA, Math.Round(totalClienteA * 0.4m, 2), now.AddDays(-2).Date.AddHours(12),
            "Pago parcial de cuenta corriente (prueba)");

        // ── Escenario 2: segundo cliente con una venta fiada, saldo completo pendiente ──
        if (clientesCC.Count > 1)
        {
            var clienteB = clientesCC[1];
            CrearVentaFiada(clienteB, now.AddDays(-4).Date.AddHours(10), (P(4), 2), (P(5), 1));
            CrearVentaFiada(clienteB, now.AddDays(-1).Date.AddHours(18), (P(0), 5));
        }

        // ── Escenario 3: cliente dejado cerca de su límite de crédito ──
        // Buscamos un cliente con límite de crédito definido (> 0) para provocar el escenario
        // de "límite excedido" al intentar una nueva venta fiada desde la aplicación.
        var clienteConLimite = clientesCC
            .Where(c => c.LimiteCredito > 0 && c.Id != clienteA.Id)
            .OrderBy(c => c.LimiteCredito)
            .FirstOrDefault();

        if (clienteConLimite is not null)
        {
            // Dejar el saldo al ~90% del límite: una venta fiada adicional lo superará.
            var objetivoSaldo = Math.Round(clienteConLimite.LimiteCredito * 0.9m, 2);

            // Cargo directo (sin venta asociada) para fijar el saldo con precisión.
            movimientos.Add(new MovimientoCuentaCorriente
            {
                ClienteId = clienteConLimite.Id,
                Tipo = TipoMovimientoCuentaCorriente.Cargo,
                Monto = objetivoSaldo,
                UsuarioId = usuario.Id,
                Descripcion = "Saldo inicial cercano al límite de crédito (prueba)",
                Fecha = now.AddDays(-8).Date.AddHours(9)
            });
        }

        context.Ventas.AddRange(ventas);
        context.DetallesVenta.AddRange(detalles);
        context.VentaPagos.AddRange(pagos);
        context.Set<MovimientoCuentaCorriente>().AddRange(movimientos);
        await context.SaveChangesAsync();

        logger.LogInformation(
            "Seed ventas fiadas: se crearon {Ventas} venta(s) a cuenta corriente y {Movimientos} movimiento(s) de cuenta corriente para pruebas.",
            ventas.Count, movimientos.Count);
    }

    /// <summary>
    /// Crea datos de prueba si no existen ya (categorías, productos, vendedores, ventas, etc.)
    /// </summary>
    private static async Task SeedTestDataAsync(ApplicationDbContext context, ILogger logger)
    {
        // Solo seedear si no hay categorías (indica que los datos de prueba no se han cargado)
        if (await context.Categorias.AnyAsync())
        {
            return;
        }

        logger.LogInformation("Iniciando seed de datos de prueba...");

        var now = DateTime.UtcNow;

        // ─────────────────────────────────────────────
        // 1. CATEGORÍAS
        // ─────────────────────────────────────────────
        var categorias = new List<Categoria>
        {
            new() { Nombre = "Bebidas", Descripcion = "Bebidas con y sin alcohol", FechaCreacion = now },
            new() { Nombre = "Lácteos", Descripcion = "Leche, yogur, quesos y derivados", FechaCreacion = now },
            new() { Nombre = "Panadería", Descripcion = "Pan, facturas, galletas y productos de horno", FechaCreacion = now },
            new() { Nombre = "Limpieza", Descripcion = "Productos de limpieza para el hogar", FechaCreacion = now },
            new() { Nombre = "Almacén", Descripcion = "Arroz, fideos, aceite, enlatados y secos", FechaCreacion = now },
            new() { Nombre = "Fiambrería", Descripcion = "Fiambres, embutidos y quesos por peso", FechaCreacion = now },
            new() { Nombre = "Snacks", Descripcion = "Papas fritas, galletitas dulces y golosinas", FechaCreacion = now },
            new() { Nombre = "Higiene Personal", Descripcion = "Jabón, shampoo, pasta dental y cuidado personal", FechaCreacion = now },
        };

        context.Categorias.AddRange(categorias);
        await context.SaveChangesAsync();

        // ─────────────────────────────────────────────
        // 2. PRODUCTOS (variados por categoría)
        // ─────────────────────────────────────────────
        var productos = new List<Producto>
        {
            // Bebidas
            new() { Nombre = "Coca-Cola 2.25L", CodigoBarras = "7790895000584", Descripcion = "Gaseosa sabor cola", Precio = 2500.00m, Stock = 48, CategoriaId = categorias[0].Id, Activo = true, FechaCreacion = now, FechaModificacion = now },
            new() { Nombre = "Agua Mineral 1.5L", CodigoBarras = "7790315000125", Descripcion = "Agua mineral sin gas", Precio = 1200.00m, Stock = 60, CategoriaId = categorias[0].Id, Activo = true, FechaCreacion = now, FechaModificacion = now },
            new() { Nombre = "Cerveza Quilmes 1L", CodigoBarras = "7792798001015", Descripcion = "Cerveza rubia", Precio = 2800.00m, Stock = 36, CategoriaId = categorias[0].Id, Activo = true, FechaCreacion = now, FechaModificacion = now },
            new() { Nombre = "Jugo Cepita 1L", CodigoBarras = "7790895063107", Descripcion = "Jugo de naranja", Precio = 1800.00m, Stock = 24, CategoriaId = categorias[0].Id, Activo = true, FechaCreacion = now, FechaModificacion = now },
            new() { Nombre = "Fernet Branca 750ml", CodigoBarras = "8002920020016", Descripcion = "Fernet amaro italiano", Precio = 12500.00m, Stock = 12, CategoriaId = categorias[0].Id, Activo = true, FechaCreacion = now, FechaModificacion = now },

            // Lácteos
            new() { Nombre = "Leche Entera 1L", CodigoBarras = "7790742100108", Descripcion = "Leche entera sachet", Precio = 1100.00m, Stock = 40, CategoriaId = categorias[1].Id, Activo = true, FechaCreacion = now, FechaModificacion = now },
            new() { Nombre = "Yogur Firme Frutilla", CodigoBarras = "7790742200105", Descripcion = "Yogur firme 190g", Precio = 900.00m, Stock = 30, CategoriaId = categorias[1].Id, Activo = true, FechaCreacion = now, FechaModificacion = now },
            new() { Nombre = "Queso Cremoso x kg", CodigoBarras = "2000001000014", Descripcion = "Queso cremoso por kilo", Precio = 8500.00m, Stock = 10, CategoriaId = categorias[1].Id, Activo = true, FechaCreacion = now, FechaModificacion = now },
            new() { Nombre = "Manteca 200g", CodigoBarras = "7790742300102", Descripcion = "Manteca La Serenísima", Precio = 2200.00m, Stock = 20, CategoriaId = categorias[1].Id, Activo = true, FechaCreacion = now, FechaModificacion = now },

            // Panadería
            new() { Nombre = "Pan Francés x kg", CodigoBarras = "2000002000013", Descripcion = "Pan francés del día", Precio = 1800.00m, Stock = 25, CategoriaId = categorias[2].Id, Activo = true, FechaCreacion = now, FechaModificacion = now },
            new() { Nombre = "Facturas x docena", CodigoBarras = "2000003000012", Descripcion = "Medialunas y facturas surtidas", Precio = 4500.00m, Stock = 15, CategoriaId = categorias[2].Id, Activo = true, FechaCreacion = now, FechaModificacion = now },
            new() { Nombre = "Galletas de Agua x 3", CodigoBarras = "7790040195004", Descripcion = "Crackers de agua tripack", Precio = 2100.00m, Stock = 35, CategoriaId = categorias[2].Id, Activo = true, FechaCreacion = now, FechaModificacion = now },

            // Limpieza
            new() { Nombre = "Lavandina 2L", CodigoBarras = "7790250042586", Descripcion = "Lavandina concentrada", Precio = 1500.00m, Stock = 20, CategoriaId = categorias[3].Id, Activo = true, FechaCreacion = now, FechaModificacion = now },
            new() { Nombre = "Detergente 750ml", CodigoBarras = "7791290007895", Descripcion = "Detergente lavavajilla", Precio = 1800.00m, Stock = 25, CategoriaId = categorias[3].Id, Activo = true, FechaCreacion = now, FechaModificacion = now },
            new() { Nombre = "Papel Higiénico x4", CodigoBarras = "7790250051045", Descripcion = "Papel higiénico doble hoja", Precio = 3200.00m, Stock = 40, CategoriaId = categorias[3].Id, Activo = true, FechaCreacion = now, FechaModificacion = now },
            new() { Nombre = "Jabón en Polvo 800g", CodigoBarras = "7791290012899", Descripcion = "Jabón para lavar ropa", Precio = 4500.00m, Stock = 18, CategoriaId = categorias[3].Id, Activo = true, FechaCreacion = now, FechaModificacion = now },

            // Almacén
            new() { Nombre = "Arroz Largo Fino 1kg", CodigoBarras = "7790580110017", Descripcion = "Arroz grano largo fino", Precio = 1600.00m, Stock = 50, CategoriaId = categorias[4].Id, Activo = true, FechaCreacion = now, FechaModificacion = now },
            new() { Nombre = "Fideos Spaghetti 500g", CodigoBarras = "7790040100008", Descripcion = "Fideos de sémola", Precio = 1200.00m, Stock = 45, CategoriaId = categorias[4].Id, Activo = true, FechaCreacion = now, FechaModificacion = now },
            new() { Nombre = "Aceite Girasol 1.5L", CodigoBarras = "7790070100153", Descripcion = "Aceite de girasol", Precio = 3800.00m, Stock = 22, CategoriaId = categorias[4].Id, Activo = true, FechaCreacion = now, FechaModificacion = now },
            new() { Nombre = "Atún en Lata 170g", CodigoBarras = "7790360001706", Descripcion = "Atún al natural", Precio = 2500.00m, Stock = 30, CategoriaId = categorias[4].Id, Activo = true, FechaCreacion = now, FechaModificacion = now },
            new() { Nombre = "Harina 000 1kg", CodigoBarras = "7790580120016", Descripcion = "Harina de trigo 000", Precio = 900.00m, Stock = 35, CategoriaId = categorias[4].Id, Activo = true, FechaCreacion = now, FechaModificacion = now },
            new() { Nombre = "Azúcar 1kg", CodigoBarras = "7790580130015", Descripcion = "Azúcar blanca refinada", Precio = 1300.00m, Stock = 40, CategoriaId = categorias[4].Id, Activo = true, FechaCreacion = now, FechaModificacion = now },

            // Fiambrería
            new() { Nombre = "Jamón Cocido x kg", CodigoBarras = "2000004000011", Descripcion = "Jamón cocido natural", Precio = 9800.00m, Stock = 8, CategoriaId = categorias[5].Id, Activo = true, FechaCreacion = now, FechaModificacion = now },
            new() { Nombre = "Queso de Máquina x kg", CodigoBarras = "2000005000010", Descripcion = "Queso cremoso en horma", Precio = 7500.00m, Stock = 10, CategoriaId = categorias[5].Id, Activo = true, FechaCreacion = now, FechaModificacion = now },
            new() { Nombre = "Salame x kg", CodigoBarras = "2000006000019", Descripcion = "Salame tipo milán", Precio = 11000.00m, Stock = 6, CategoriaId = categorias[5].Id, Activo = true, FechaCreacion = now, FechaModificacion = now },

            // Snacks
            new() { Nombre = "Papas Fritas Lays 270g", CodigoBarras = "7790310982709", Descripcion = "Papas fritas clásicas", Precio = 3500.00m, Stock = 20, CategoriaId = categorias[6].Id, Activo = true, FechaCreacion = now, FechaModificacion = now },
            new() { Nombre = "Galletitas Oreo", CodigoBarras = "7622300489434", Descripcion = "Galletitas rellenas chocolate", Precio = 2000.00m, Stock = 30, CategoriaId = categorias[6].Id, Activo = true, FechaCreacion = now, FechaModificacion = now },
            new() { Nombre = "Chocolate Milka 150g", CodigoBarras = "7622300513092", Descripcion = "Chocolate con leche", Precio = 3800.00m, Stock = 25, CategoriaId = categorias[6].Id, Activo = true, FechaCreacion = now, FechaModificacion = now },
            new() { Nombre = "Alfajor Havanna", CodigoBarras = "7790270001012", Descripcion = "Alfajor de dulce de leche", Precio = 2200.00m, Stock = 40, CategoriaId = categorias[6].Id, Activo = true, FechaCreacion = now, FechaModificacion = now },

            // Higiene Personal
            new() { Nombre = "Shampoo Sedal 340ml", CodigoBarras = "7791293025346", Descripcion = "Shampoo para cabello normal", Precio = 4200.00m, Stock = 15, CategoriaId = categorias[7].Id, Activo = true, FechaCreacion = now, FechaModificacion = now },
            new() { Nombre = "Jabón Dove 90g", CodigoBarras = "7891150001930", Descripcion = "Jabón de tocador", Precio = 1500.00m, Stock = 25, CategoriaId = categorias[7].Id, Activo = true, FechaCreacion = now, FechaModificacion = now },
            new() { Nombre = "Pasta Dental Colgate 90g", CodigoBarras = "7891024130209", Descripcion = "Pasta dental con flúor", Precio = 2800.00m, Stock = 20, CategoriaId = categorias[7].Id, Activo = true, FechaCreacion = now, FechaModificacion = now },
            new() { Nombre = "Desodorante Rexona 150ml", CodigoBarras = "7791293012568", Descripcion = "Desodorante en aerosol", Precio = 4500.00m, Stock = 18, CategoriaId = categorias[7].Id, Activo = true, FechaCreacion = now, FechaModificacion = now },
        };

        // Derivar un precio de costo coherente (~40% de margen) para los datos de prueba
        foreach (var p in productos)
        {
            p.PrecioCosto = Math.Round(p.Precio / 1.4m, 2);
        }

        context.Productos.AddRange(productos);
        await context.SaveChangesAsync();

        // ─────────────────────────────────────────────
        // 3. USUARIOS VENDEDORES
        // ─────────────────────────────────────────────
        var vendedor1Hash = BCrypt.Net.BCrypt.HashPassword("Vendedor1!");
        var vendedor2Hash = BCrypt.Net.BCrypt.HashPassword("Vendedor2!");

        var vendedor1 = new Usuario
        {
            Nombre = "María López",
            Email = "maria.lopez@sistema.local",
            PasswordHash = vendedor1Hash,
            Rol = Rol.Vendedor,
            Activo = true,
            IntentosFallidos = 0,
            FechaCreacion = now.AddDays(-30)
        };

        var vendedor2 = new Usuario
        {
            Nombre = "Carlos García",
            Email = "carlos.garcia@sistema.local",
            PasswordHash = vendedor2Hash,
            Rol = Rol.Vendedor,
            Activo = true,
            IntentosFallidos = 0,
            FechaCreacion = now.AddDays(-15)
        };

        context.Usuarios.AddRange(vendedor1, vendedor2);
        await context.SaveChangesAsync();

        // ─────────────────────────────────────────────
        // 4. CAJAS (una abierta, una cerrada)
        // ─────────────────────────────────────────────
        var cajaCerrada = new Caja
        {
            PuntoDeVentaId = 1,
            UsuarioAperturaId = vendedor1.Id,
            UsuarioCierreId = vendedor1.Id,
            MontoInicial = 10000.00m,
            MontoRealCierre = 45800.00m,
            SaldoEsperado = 45650.00m,
            Diferencia = 150.00m,
            Estado = EstadoCaja.Cerrada,
            FechaApertura = now.AddDays(-1).Date.AddHours(8),
            FechaCierre = now.AddDays(-1).Date.AddHours(18)
        };

        var cajaAbierta = new Caja
        {
            PuntoDeVentaId = 1,
            UsuarioAperturaId = vendedor2.Id,
            MontoInicial = 10000.00m,
            Estado = EstadoCaja.Abierta,
            FechaApertura = now.Date.AddHours(8)
        };

        context.Cajas.AddRange(cajaCerrada, cajaAbierta);
        await context.SaveChangesAsync();

        // ─────────────────────────────────────────────
        // 5. MOVIMIENTOS DE CAJA
        // ─────────────────────────────────────────────
        var movimientos = new List<CajaMovimiento>
        {
            // Caja cerrada - ayer
            new() { CajaId = cajaCerrada.Id, UsuarioId = vendedor1.Id, Tipo = TipoMovimiento.Apertura, Monto = 10000.00m, Motivo = "Apertura de caja", Fecha = cajaCerrada.FechaApertura },
            new() { CajaId = cajaCerrada.Id, UsuarioId = vendedor1.Id, Tipo = TipoMovimiento.VentaEfectivo, Monto = 7500.00m, Motivo = null, Fecha = cajaCerrada.FechaApertura.AddHours(2) },
            new() { CajaId = cajaCerrada.Id, UsuarioId = vendedor1.Id, Tipo = TipoMovimiento.VentaEfectivo, Monto = 15300.00m, Motivo = null, Fecha = cajaCerrada.FechaApertura.AddHours(4) },
            new() { CajaId = cajaCerrada.Id, UsuarioId = vendedor1.Id, Tipo = TipoMovimiento.Retiro, Monto = 5000.00m, Motivo = "Retiro para compra de insumos", Fecha = cajaCerrada.FechaApertura.AddHours(6) },
            new() { CajaId = cajaCerrada.Id, UsuarioId = vendedor1.Id, Tipo = TipoMovimiento.VentaEfectivo, Monto = 12850.00m, Motivo = null, Fecha = cajaCerrada.FechaApertura.AddHours(8) },

            // Caja abierta - hoy
            new() { CajaId = cajaAbierta.Id, UsuarioId = vendedor2.Id, Tipo = TipoMovimiento.Apertura, Monto = 10000.00m, Motivo = "Apertura de caja", Fecha = cajaAbierta.FechaApertura },
            new() { CajaId = cajaAbierta.Id, UsuarioId = vendedor2.Id, Tipo = TipoMovimiento.VentaEfectivo, Monto = 5200.00m, Motivo = null, Fecha = cajaAbierta.FechaApertura.AddHours(1) },
        };

        context.CajaMovimientos.AddRange(movimientos);
        await context.SaveChangesAsync();

        // ─────────────────────────────────────────────
        // 6. VENTAS CON DETALLES Y PAGOS
        // ─────────────────────────────────────────────

        // Venta 1: Vendedora María - ayer - Confirmada
        var venta1 = new Venta
        {
            UsuarioId = vendedor1.Id,
            Fecha = now.AddDays(-1).Date.AddHours(10),
            Total = 7500.00m,
            Estado = EstadoVenta.Confirmada,
            FechaCreacion = now.AddDays(-1).Date.AddHours(10)
        };

        // Venta 2: Vendedora María - ayer - Confirmada
        var venta2 = new Venta
        {
            UsuarioId = vendedor1.Id,
            Fecha = now.AddDays(-1).Date.AddHours(14),
            Total = 15300.00m,
            Estado = EstadoVenta.Confirmada,
            FechaCreacion = now.AddDays(-1).Date.AddHours(14)
        };

        // Venta 3: Vendedora María - ayer - Confirmada
        var venta3 = new Venta
        {
            UsuarioId = vendedor1.Id,
            Fecha = now.AddDays(-1).Date.AddHours(16),
            Total = 12850.00m,
            Estado = EstadoVenta.Confirmada,
            FechaCreacion = now.AddDays(-1).Date.AddHours(16)
        };

        // Venta 4: Vendedor Carlos - hoy - Confirmada
        var venta4 = new Venta
        {
            UsuarioId = vendedor2.Id,
            Fecha = now.Date.AddHours(9),
            Total = 5200.00m,
            Estado = EstadoVenta.Confirmada,
            FechaCreacion = now.Date.AddHours(9)
        };

        // Venta 5: Vendedor Carlos - hoy - Borrador (en curso)
        var venta5 = new Venta
        {
            UsuarioId = vendedor2.Id,
            Fecha = now,
            Total = 8700.00m,
            Estado = EstadoVenta.Borrador,
            FechaCreacion = now
        };

        context.Ventas.AddRange(venta1, venta2, venta3, venta4, venta5);
        await context.SaveChangesAsync();

        // ─────────────────────────────────────────────
        // 7. DETALLES DE VENTA
        // ─────────────────────────────────────────────
        var detalles = new List<DetalleVenta>
        {
            // Venta 1: Coca-Cola x2 + Papas Lays x1 + Cerveza x1
            new() { VentaId = venta1.Id, ProductoId = productos[0].Id, Cantidad = 2, PrecioUnitario = 2500.00m, Subtotal = 5000.00m },
            new() { VentaId = venta1.Id, ProductoId = productos[25].Id, Cantidad = 1, PrecioUnitario = 3500.00m, Subtotal = 3500.00m },

            // Venta 2: Arroz x3 + Aceite x2 + Leche x4 + Fideos x2
            new() { VentaId = venta2.Id, ProductoId = productos[16].Id, Cantidad = 3, PrecioUnitario = 1600.00m, Subtotal = 4800.00m },
            new() { VentaId = venta2.Id, ProductoId = productos[18].Id, Cantidad = 2, PrecioUnitario = 3800.00m, Subtotal = 7600.00m },
            new() { VentaId = venta2.Id, ProductoId = productos[5].Id, Cantidad = 4, PrecioUnitario = 1100.00m, Subtotal = 4400.00m },

            // Venta 3: Jamón x1kg + Queso máquina x0.5 (1) + Pan x2
            new() { VentaId = venta3.Id, ProductoId = productos[22].Id, Cantidad = 1, PrecioUnitario = 9800.00m, Subtotal = 9800.00m },
            new() { VentaId = venta3.Id, ProductoId = productos[9].Id, Cantidad = 2, PrecioUnitario = 1800.00m, Subtotal = 3600.00m },

            // Venta 4: Agua x2 + Alfajor x3 + Pasta Dental x1
            new() { VentaId = venta4.Id, ProductoId = productos[1].Id, Cantidad = 2, PrecioUnitario = 1200.00m, Subtotal = 2400.00m },
            new() { VentaId = venta4.Id, ProductoId = productos[28].Id, Cantidad = 3, PrecioUnitario = 2200.00m, Subtotal = 6600.00m },

            // Venta 5 (borrador): Fernet x1 + Coca-Cola x2
            new() { VentaId = venta5.Id, ProductoId = productos[4].Id, Cantidad = 1, PrecioUnitario = 12500.00m, Subtotal = 12500.00m },
            new() { VentaId = venta5.Id, ProductoId = productos[0].Id, Cantidad = 2, PrecioUnitario = 2500.00m, Subtotal = 5000.00m },
        };

        context.DetallesVenta.AddRange(detalles);
        await context.SaveChangesAsync();

        // ─────────────────────────────────────────────
        // 8. PAGOS DE VENTAS
        // ─────────────────────────────────────────────
        var pagos = new List<VentaPago>
        {
            // Venta 1: pagada en efectivo
            new() { VentaId = venta1.Id, MedioPagoId = 1, Monto = 7500.00m },

            // Venta 2: parte efectivo, parte débito
            new() { VentaId = venta2.Id, MedioPagoId = 1, Monto = 10000.00m },
            new() { VentaId = venta2.Id, MedioPagoId = 2, Monto = 5300.00m },

            // Venta 3: tarjeta de crédito
            new() { VentaId = venta3.Id, MedioPagoId = 3, Monto = 12850.00m },

            // Venta 4: efectivo
            new() { VentaId = venta4.Id, MedioPagoId = 1, Monto = 5200.00m },
        };

        context.VentaPagos.AddRange(pagos);
        await context.SaveChangesAsync();

        // ─────────────────────────────────────────────
        // 9. COMPROBANTES (para ventas confirmadas)
        // ─────────────────────────────────────────────
        var comprobantes = new List<Comprobante>
        {
            new()
            {
                VentaId = venta1.Id,
                TipoComprobante = 6, // Factura B
                NumeroComprobante = 1,
                CAE = "74123456789012",
                FechaVencimientoCAE = now.AddDays(9),
                Estado = EstadoComprobante.Emitido,
                FechaEmision = venta1.Fecha
            },
            new()
            {
                VentaId = venta2.Id,
                TipoComprobante = 6,
                NumeroComprobante = 2,
                CAE = "74123456789013",
                FechaVencimientoCAE = now.AddDays(9),
                Estado = EstadoComprobante.Emitido,
                FechaEmision = venta2.Fecha
            },
            new()
            {
                VentaId = venta3.Id,
                TipoComprobante = 6,
                NumeroComprobante = 3,
                CAE = null,
                Estado = EstadoComprobante.Pendiente,
                FechaEmision = venta3.Fecha
            },
            new()
            {
                VentaId = venta4.Id,
                TipoComprobante = 6,
                NumeroComprobante = 4,
                CAE = "74123456789014",
                FechaVencimientoCAE = now.AddDays(10),
                Estado = EstadoComprobante.Emitido,
                FechaEmision = venta4.Fecha
            },
        };

        context.Comprobantes.AddRange(comprobantes);
        await context.SaveChangesAsync();

        // ─────────────────────────────────────────────
        // 10. LOGS DE AUDITORÍA
        // ─────────────────────────────────────────────
        var admin = await context.Usuarios.FirstAsync(u => u.Rol == Rol.Administrador);

        var auditoriaLogs = new List<AuditoriaLog>
        {
            new() { UsuarioId = admin.Id, TipoOperacion = TipoOperacion.Creacion, EntidadAfectada = "Usuario", RegistroAfectadoId = vendedor1.Id.ToString(), Descripcion = "Se creó el usuario María López con rol Vendedor", Fecha = now.AddDays(-30) },
            new() { UsuarioId = admin.Id, TipoOperacion = TipoOperacion.Creacion, EntidadAfectada = "Usuario", RegistroAfectadoId = vendedor2.Id.ToString(), Descripcion = "Se creó el usuario Carlos García con rol Vendedor", Fecha = now.AddDays(-15) },
            new() { UsuarioId = vendedor1.Id, TipoOperacion = TipoOperacion.Venta, EntidadAfectada = "Venta", RegistroAfectadoId = venta1.Id.ToString(), Descripcion = "Venta confirmada por $7500.00", Fecha = venta1.Fecha },
            new() { UsuarioId = vendedor1.Id, TipoOperacion = TipoOperacion.Venta, EntidadAfectada = "Venta", RegistroAfectadoId = venta2.Id.ToString(), Descripcion = "Venta confirmada por $15300.00", Fecha = venta2.Fecha },
            new() { UsuarioId = vendedor1.Id, TipoOperacion = TipoOperacion.Venta, EntidadAfectada = "Venta", RegistroAfectadoId = venta3.Id.ToString(), Descripcion = "Venta confirmada por $12850.00", Fecha = venta3.Fecha },
            new() { UsuarioId = vendedor2.Id, TipoOperacion = TipoOperacion.Venta, EntidadAfectada = "Venta", RegistroAfectadoId = venta4.Id.ToString(), Descripcion = "Venta confirmada por $5200.00", Fecha = venta4.Fecha },
            new() { UsuarioId = admin.Id, TipoOperacion = TipoOperacion.Creacion, EntidadAfectada = "Producto", RegistroAfectadoId = "1", Descripcion = "Se cargaron productos iniciales al sistema", Fecha = now.AddDays(-7) },
        };

        context.AuditoriaLogs.AddRange(auditoriaLogs);
        await context.SaveChangesAsync();

        logger.LogInformation("Datos de prueba cargados exitosamente: {Categorias} categorías, {Productos} productos, {Usuarios} vendedores, {Ventas} ventas.",
            categorias.Count, productos.Count, 2, 5);
    }
}
