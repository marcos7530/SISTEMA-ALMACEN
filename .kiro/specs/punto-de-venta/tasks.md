# Implementation Plan: Sistema Punto de Venta

## Overview

Implementación incremental del Sistema POS como aplicación Blazor WebAssembly (PWA) con backend ASP.NET Core Web API, Entity Framework Core Code First sobre SQL Server, integración con AFIP/ARCA para facturación electrónica, soporte offline y gestión completa de ventas, caja, productos, usuarios y reportes.

## Tasks

- [x] 1. Configurar estructura de solución y proyectos base
  - [x] 1.1 Crear solución .sln con los 5 proyectos: SistemaAlmacen.Client (Blazor WASM PWA), SistemaAlmacen.Server (ASP.NET Core Web API), SistemaAlmacen.Business, SistemaAlmacen.Data, SistemaAlmacen.Shared
    - Configurar las referencias entre proyectos respetando la dirección de dependencias: Client → Shared, Server → Business → Data → Shared, Server → Shared
    - Instalar paquetes NuGet base: Microsoft.EntityFrameworkCore.SqlServer, Microsoft.EntityFrameworkCore.Tools, Microsoft.AspNetCore.Authentication.JwtBearer, FsCheck.Xunit, xUnit
    - Configurar Program.cs de Server con inyección de dependencias, middleware, CORS y Blazor WASM hosting
    - Configurar Program.cs de Client con HttpClient, servicios de autenticación y registro de servicios offline
    - _Requirements: 1.1, 1.3_

  - [x] 1.2 Crear proyectos de tests: SistemaAlmacen.Business.Tests, SistemaAlmacen.Data.Tests, SistemaAlmacen.Integration.Tests
    - Configurar xUnit y FsCheck.Xunit como frameworks de testing
    - Crear carpeta Properties/ para property tests y Services/ para unit tests en Business.Tests
    - _Requirements: 1.1_

- [x] 2. Implementar capa de datos: entidades, DbContext y migraciones
  - [x] 2.1 Definir entidades del modelo de datos en SistemaAlmacen.Data/Entities/
    - Crear entidades: Usuario, Categoria, Producto, Venta, DetalleVenta, VentaPago, MedioPago, Comprobante, Caja, CajaMovimiento, AuditoriaLog, TokenRecuperacion
    - Definir enumeraciones en SistemaAlmacen.Shared/Enums/: Rol, EstadoVenta, EstadoComprobante, EstadoCaja, TipoMovimiento, TipoOperacion, TipoComprobante, FormatoExportacion
    - Aplicar Data Annotations para campos obligatorios, longitudes máximas y restricciones de valores
    - _Requirements: 2.1, 2.2, 2.3, 2.5_

  - [x] 2.2 Crear DbContext y configuraciones Fluent API en SistemaAlmacen.Data/Context/ y Configurations/
    - Implementar ApplicationDbContext con DbSets para todas las entidades
    - Crear IEntityTypeConfiguration para cada entidad con relaciones, índices y restricciones
    - Configurar comportamientos de eliminación: RESTRICT para Venta-Usuario, DetalleVenta-Producto, Producto-Categoría; CASCADE para Venta-DetalleVenta, Caja-CajaMovimiento
    - Configurar precisión decimal(18,2) para campos monetarios
    - _Requirements: 2.2, 2.4, 2.5_

  - [x] 2.3 Crear migración inicial y seed de datos predeterminados
    - Generar migración inicial con `dotnet ef migrations add InitialCreate`
    - Crear seed para medios de pago por defecto: Efectivo, Tarjeta de Débito, Tarjeta de Crédito, Transferencia Bancaria
    - Crear seed para usuario Administrador inicial
    - _Requirements: 2.1, 17.1_

  - [ ]* 2.4 Escribir tests de configuración de Entity Framework
    - Verificar que las relaciones y restricciones se aplican correctamente usando SQLite in-memory
    - Verificar comportamientos de eliminación (RESTRICT/CASCADE)
    - _Requirements: 2.2, 2.4_

- [x] 3. Implementar DTOs compartidos y patrón Result
  - [x] 3.1 Crear DTOs en SistemaAlmacen.Shared/DTOs/
    - Definir DTOs de request y response para cada módulo: LoginRequest, AuthResult, CreateUsuarioRequest, UpdateUsuarioRequest, UsuarioDto, UsuarioFilter, CreateCategoriaRequest, CategoriaDto, CreateProductoRequest, ProductoDto, ProductoFilter, AddDetalleRequest, ConfirmVentaRequest, VentaDto, VentaResumenDto, VentaDetalleCompletoDto, DetalleVentaDto, AbrirCajaRequest, CerrarCajaRequest, MovimientoRequest, CajaDto, CierreResumenDto, MovimientoDto, CierreFilter, CreateMedioPagoRequest, MedioPagoDto, AuditoriaDto, AuditoriaFilter, ReporteVentasDto, ProductoMasVendidoDto, ReporteInventarioDto, ExportRequest, ComprobanteDto, VentaPendienteFacturacionDto
    - Definir PaginatedResult<T> genérico para paginación
    - _Requirements: 1.1, 1.3_

  - [x] 3.2 Implementar patrón Result y Result<T> en SistemaAlmacen.Shared/
    - Crear clases Result y Result<T> con Success, Failure, ValidationFailure
    - Incluir soporte para errores por campo (Dictionary<string, string[]>)
    - Incluir ErrorCode para identificación programática de errores
    - _Requirements: 1.2, 12.1_

  - [x] 3.3 Crear validadores compartidos con Data Annotations en SistemaAlmacen.Shared/Validators/
    - Implementar validaciones reutilizables: campos obligatorios, longitudes máximas, rangos numéricos, formato de email
    - Crear atributo personalizado para validar que strings no sean solo whitespace
    - _Requirements: 12.1, 12.2, 12.3_

- [x] 4. Implementar repositorios genéricos y específicos
  - [x] 4.1 Crear interfaces y repositorios en SistemaAlmacen.Data/Repositories/
    - Implementar IRepository<T> genérico con CRUD básico, paginación y filtrado
    - Implementar repositorios específicos: IUsuarioRepository, IProductoRepository, ICategoriaRepository, IVentaRepository, ICajaRepository, IAuditoriaRepository, IMedioPagoRepository, IComprobanteRepository
    - Implementar Unit of Work pattern para transacciones que involucran múltiples repositorios
    - _Requirements: 1.3, 2.6, 12.4_

- [x] 5. Checkpoint — Verificar compilación y estructura base
  - Ensure all tests pass, ask the user if questions arise.

- [x] 6. Implementar módulo de autenticación y seguridad
  - [x] 6.1 Implementar IAuthService, ITokenService e ISessionService en SistemaAlmacen.Business/Services/
    - Implementar login con validación de credenciales, verificación de estado activo y generación de JWT
    - Implementar bloqueo temporal tras 5 intentos fallidos consecutivos (15 minutos)
    - Implementar hash de contraseñas con BCrypt
    - Implementar expiración de sesión por inactividad (30 minutos)
    - Implementar cierre de sesión con invalidación de token
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 4.7_

  - [x] 6.2 Implementar recuperación de contraseña
    - Generar token de recuperación (mínimo 32 caracteres alfanuméricos, un solo uso)
    - Implementar validación de token (expiración 24 horas, no reutilizable)
    - Implementar restablecimiento de contraseña con requisitos de complejidad (mín 8 caracteres, mayúscula, minúscula, número)
    - Responder con mensaje genérico independientemente de si el email existe
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6_

  - [x] 6.3 Implementar control de acceso por roles en el servidor
    - Crear AuthorizationHandler personalizado para políticas de Administrador y Vendedor
    - Implementar atributos de autorización en controllers
    - Validar que no se puede eliminar el último Administrador activo
    - Validar que un usuario no puede eliminar su propia cuenta
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6_

  - [x] 6.4 Crear AuthController en SistemaAlmacen.Server/Controllers/
    - Endpoints: POST /api/auth/login, POST /api/auth/logout, POST /api/auth/recover-password, POST /api/auth/reset-password, POST /api/auth/validate-token
    - _Requirements: 4.1, 4.5, 5.1, 5.3_

  - [ ]* 6.5 Escribir property test para token de recuperación de un solo uso
    - **Property 13: Token de recuperación de un solo uso**
    - **Validates: Requirements 5.4, 5.5, 5.6**

  - [ ]* 6.6 Escribir tests unitarios para módulo de autenticación
    - Tests de login exitoso y fallido, bloqueo temporal, expiración de sesión
    - Tests de recuperación de contraseña: generación, validación, expiración de token
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 5.1, 5.4_

- [x] 7. Implementar módulo de gestión de usuarios
  - [x] 7.1 Implementar IUsuarioService en SistemaAlmacen.Business/Services/
    - CRUD completo: crear con contraseña cifrada, listar paginado (20 por página, orden alfabético), modificar, eliminación lógica
    - Búsqueda parcial por nombre o email (case-insensitive)
    - Validaciones: email único entre activos, no eliminar cuenta propia, no eliminar último admin
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8, 3.9_

  - [x] 7.2 Crear UsuariosController en SistemaAlmacen.Server/Controllers/
    - Endpoints: GET /api/usuarios (paginado + búsqueda), GET /api/usuarios/{id}, POST /api/usuarios, PUT /api/usuarios/{id}, DELETE /api/usuarios/{id}
    - Restringir acceso solo a Administrador
    - _Requirements: 3.1, 3.2, 6.2_

  - [ ]* 7.3 Escribir property test para unicidad de email
    - **Property 5: Unicidad de email de usuario activo**
    - **Validates: Requirements 3.6**

  - [ ]* 7.4 Escribir property test para validación de entrada
    - **Property 4: Validación de entrada rechaza whitespace y vacíos**
    - **Validates: Requirements 3.9, 7.1, 8.1, 12.2**

  - [ ]* 7.5 Escribir property test para eliminación lógica
    - **Property 6: Eliminación lógica excluye de listados**
    - **Validates: Requirements 3.4, 8.4**

- [x] 8. Implementar módulo de categorías
  - [x] 8.1 Implementar ICategoriaService en SistemaAlmacen.Business/Services/
    - CRUD: crear (nombre 3-50 chars, descripción máx 200), listar (orden alfabético), modificar, eliminar (solo si no tiene productos asociados)
    - Validar unicidad de nombre de categoría
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 7.6_

  - [x] 8.2 Crear CategoriasController en SistemaAlmacen.Server/Controllers/
    - Endpoints: GET /api/categorias, POST /api/categorias, PUT /api/categorias/{id}, DELETE /api/categorias/{id}
    - Restringir escritura a Administrador, lectura disponible para Vendedor
    - _Requirements: 7.1, 6.3_

- [x] 9. Implementar módulo de productos
  - [x] 9.1 Implementar IProductoService en SistemaAlmacen.Business/Services/
    - CRUD: crear (nombre máx 100, descripción máx 500, precio 0.01-999999999.99, stock ≥ 0), listar activos paginado, modificar, eliminación lógica
    - Búsqueda parcial case-insensitive por nombre o categoría
    - Validar unicidad de nombre por categoría (entre productos activos)
    - Validar que categoría referenciada existe
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6, 8.7, 8.8, 8.9_

  - [x] 9.2 Crear ProductosController en SistemaAlmacen.Server/Controllers/
    - Endpoints: GET /api/productos (paginado + búsqueda + filtro categoría), GET /api/productos/{id}, POST /api/productos, PUT /api/productos/{id}, DELETE /api/productos/{id}
    - Restringir escritura a Administrador, lectura disponible para todos los roles autenticados
    - _Requirements: 8.1, 6.2, 6.3_

  - [ ]* 9.3 Escribir property test para búsqueda parcial case-insensitive
    - **Property 9: Búsqueda parcial case-insensitive retorna coincidencias correctas**
    - **Validates: Requirements 8.5, 3.5**

- [x] 10. Checkpoint — Verificar módulos CRUD base
  - Ensure all tests pass, ask the user if questions arise.

- [x] 11. Implementar módulo de ventas
  - [x] 11.1 Implementar IVentaService e IStockService en SistemaAlmacen.Business/Services/
    - Iniciar venta (borrador) con fecha actual y vendedor asociado
    - Agregar/eliminar detalles: validar cantidad (1-10000), calcular subtotal (precio × cantidad), verificar stock disponible
    - Recalcular total automáticamente al modificar detalles
    - Confirmar venta: transacción atómica con descuento de stock, validación de al menos un detalle
    - Rollback completo ante errores de concurrencia o conexión
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5, 9.6, 9.7, 9.8, 9.9_

  - [x] 11.2 Implementar consulta de ventas con filtrado y paginación
    - Historial paginado con fecha, vendedor, cantidad de productos y total (orden descendente por fecha)
    - Filtro por rango de fechas (inclusive)
    - Detalle completo de venta con productos, cantidades, precios y subtotales
    - Vendedor solo ve sus propias ventas; Administrador ve todas
    - _Requirements: 10.1, 10.2, 10.3, 10.4, 10.5_

  - [x] 11.3 Crear VentasController en SistemaAlmacen.Server/Controllers/
    - Endpoints: POST /api/ventas (iniciar), POST /api/ventas/{id}/detalles (agregar), DELETE /api/ventas/{id}/detalles/{detalleId}, POST /api/ventas/{id}/confirmar, GET /api/ventas (historial), GET /api/ventas/{id}
    - _Requirements: 9.1, 10.1_

  - [ ]* 11.4 Escribir property test para total de venta
    - **Property 1: Total de venta es suma de subtotales**
    - **Validates: Requirements 9.4**

  - [ ]* 11.5 Escribir property test para subtotal de detalle
    - **Property 2: Subtotal es precio por cantidad**
    - **Validates: Requirements 9.2**

  - [ ]* 11.6 Escribir property test para stock no negativo
    - **Property 3: Stock no puede ser negativo tras venta**
    - **Validates: Requirements 9.3, 9.5**

- [x] 12. Implementar módulo de medios de pago y pago mixto
  - [x] 12.1 Implementar IMedioPagoService en SistemaAlmacen.Business/Services/
    - CRUD de medios de pago: crear (nombre 3-50 chars, único), modificar, desactivar (no eliminar)
    - No permitir desactivar Efectivo si hay caja abierta
    - Validar unicidad de nombre (activos e inactivos)
    - _Requirements: 17.1, 17.2, 17.3, 17.4, 17.5, 17.12_

  - [x] 12.2 Integrar pago mixto en confirmación de venta
    - Requerir especificación de medios de pago al confirmar (uno o más medios activos con montos)
    - Validar que suma de montos = total de la venta
    - Registrar desglose de pagos (VentaPago) para cada venta
    - Solo registrar monto en efectivo como ingreso de caja
    - Mostrar desglose de medios de pago en detalle de venta
    - _Requirements: 17.6, 17.7, 17.8, 17.9, 17.10_

  - [x] 12.3 Crear MediosPagoController en SistemaAlmacen.Server/Controllers/
    - Endpoints: GET /api/medios-pago (activos), GET /api/medios-pago/all (admin), POST /api/medios-pago, PUT /api/medios-pago/{id}, DELETE /api/medios-pago/{id} (desactivar)
    - _Requirements: 17.2, 17.3, 17.4_

  - [ ]* 12.4 Escribir property test para pago mixto
    - **Property 7: Pago mixto debe igualar el total**
    - **Validates: Requirements 17.6, 17.7**

- [x] 13. Implementar módulo de caja
  - [x] 13.1 Implementar ICajaService en SistemaAlmacen.Business/Services/
    - Abrir caja: validar que no existe caja abierta para el punto de venta, registrar monto inicial
    - Registrar retiros e ingresos adicionales (solo Administrador) con monto, motivo, fecha/hora
    - Registrar automáticamente ventas en efectivo como ingreso de caja
    - Cerrar caja: calcular saldo esperado (inicial + ingresos - retiros), registrar monto real, calcular diferencia
    - Generar resumen de cierre con todos los movimientos del período
    - Marcar cierre con diferencia significativa si supera umbral configurable ($500 default)
    - Impedir ventas en efectivo si no hay caja abierta
    - _Requirements: 16.1, 16.2, 16.3, 16.4, 16.5, 16.6, 16.7, 16.8, 16.9, 16.10_

  - [x] 13.2 Crear CajaController en SistemaAlmacen.Server/Controllers/
    - Endpoints: POST /api/caja/abrir, POST /api/caja/cerrar, POST /api/caja/retiro, POST /api/caja/ingreso, GET /api/caja/actual, GET /api/caja/historial-cierres
    - _Requirements: 16.1, 16.6, 16.8_

  - [ ]* 13.3 Escribir property test para saldo de caja
    - **Property 8: Saldo esperado de caja es monto inicial + ingresos - retiros**
    - **Validates: Requirements 16.6, 16.7**

- [x] 14. Checkpoint — Verificar módulos de negocio core
  - Ensure all tests pass, ask the user if questions arise.

- [x] 15. Implementar módulo de auditoría
  - [x] 15.1 Implementar IAuditoriaService e interceptor de EF Core en SistemaAlmacen.Data/
    - Crear interceptor SaveChangesInterceptor que detecte creaciones, modificaciones y eliminaciones de entidades auditables
    - Registrar automáticamente: usuario, tipo operación, entidad afectada, ID registro, fecha/hora, descripción
    - Ejecutar registro de forma asíncrona para no afectar tiempo de respuesta
    - Garantizar inmutabilidad: no exponer endpoints de modificación o eliminación de registros de auditoría
    - _Requirements: 13.1, 13.2, 13.7, 13.9_

  - [x] 15.2 Implementar consulta de auditoría con filtros
    - Listado paginado ordenado por fecha descendente
    - Filtros: por usuario, por rango de fechas, por tipo de operación, por entidad afectada
    - Acceso restringido a Administrador
    - _Requirements: 13.3, 13.4, 13.5, 13.6, 13.8_

  - [x] 15.3 Crear AuditoriaController en SistemaAlmacen.Server/Controllers/
    - Endpoint: GET /api/auditoria (paginado + filtros)
    - Restringir acceso solo a Administrador
    - _Requirements: 13.3, 13.8_

  - [ ]* 15.4 Escribir property test para inmutabilidad de auditoría
    - **Property 10: Auditoría es inmutable y completa**
    - **Validates: Requirements 13.1, 13.7**

- [x] 16. Implementar módulo de facturación electrónica AFIP
  - [x] 16.1 Implementar IAfipClientWrapper e IFacturacionService en SistemaAlmacen.Business/Services/
    - Integrar paquete NuGet Afip.Net para conexión con Web Services de AFIP/ARCA
    - Implementar emisión de CAE mediante CreateNextVoucherAsync: enviar punto de venta, tipo comprobante, importes (total, neto gravado, IVA, exento), moneda, alícuotas
    - Almacenar CAE, fecha vencimiento CAE y número de comprobante al recibir respuesta exitosa
    - Soportar tipos: Factura A (1), Factura B (6), Factura C (11) según condición IVA del receptor
    - Marcar venta como pendiente de facturación si falla la solicitud de CAE
    - _Requirements: 15.1, 15.3, 15.4, 15.5, 15.7_

  - [x] 16.2 Implementar gestión de comprobantes pendientes y reintento
    - Listado de ventas pendientes de facturación con error almacenado
    - Reintento individual y masivo de emisión
    - Consulta de comprobante emitido mediante GetVoucherInfoAsync
    - _Requirements: 15.8, 15.9_

  - [x] 16.3 Implementar generación de PDF de comprobante
    - Generar PDF con datos fiscales obligatorios: CAE, fecha vencimiento, número comprobante, datos emisor/receptor, detalle productos, importes, IVA, código QR
    - _Requirements: 15.6_

  - [x] 16.4 Implementar configuración de conexión AFIP
    - Sección de configuración accesible solo por Administrador: CUIT, certificado, clave privada, modo producción/desarrollo
    - Modo desarrollo con CUIT de prueba 20409378472 sin certificado
    - Registrar emisiones en auditoría (exitosas y fallidas)
    - _Requirements: 15.2, 15.10, 15.12_

  - [x] 16.5 Crear FacturacionController en SistemaAlmacen.Server/Controllers/
    - Endpoints: POST /api/facturacion/emitir/{ventaId}, POST /api/facturacion/reintentar/{ventaId}, POST /api/facturacion/reintentar-masivo, GET /api/facturacion/pendientes, GET /api/facturacion/comprobante/{ventaId}, GET /api/facturacion/comprobante/{ventaId}/pdf
    - _Requirements: 15.3, 15.8, 15.9_

  - [ ]* 16.6 Escribir tests unitarios para módulo de facturación
    - Tests con mock de AFIP: respuesta exitosa con CAE, rechazo, timeout, error de conexión
    - Tests de selección automática de tipo de comprobante
    - _Requirements: 15.3, 15.5, 15.7_

- [x] 17. Implementar módulo de reportes y exportación
  - [x] 17.1 Implementar IReporteService en SistemaAlmacen.Business/Services/
    - Reporte de ventas: total monetario, cantidad transacciones, desglose diario (rango máx 365 días)
    - Reporte de productos más vendidos: top 50 por unidades vendidas con nombre, categoría y cantidad
    - Reporte de inventario: todos los productos con nombre, categoría, stock, precio, estado
    - Incluir desglose por medio de pago en reportes de ventas
    - Mostrar reporte vacío con mensaje si no hay datos en el período
    - _Requirements: 11.1, 11.2, 11.5, 11.6, 11.7, 17.11_

  - [x] 17.2 Implementar exportación a PDF, Excel y CSV
    - Generar archivo en formato seleccionado (máx 30 segundos)
    - Nombre de archivo con tipo de reporte y rango de fechas
    - Manejo de error si excede timeout: mensaje + opción de reintentar
    - _Requirements: 11.3, 11.4, 11.8_

  - [x] 17.3 Crear ReportesController en SistemaAlmacen.Server/Controllers/
    - Endpoints: GET /api/reportes/ventas, GET /api/reportes/productos-mas-vendidos, GET /api/reportes/inventario, POST /api/reportes/exportar
    - Restringir acceso a Administrador (excepto reporte de ventas propias para Vendedor)
    - _Requirements: 11.1, 6.2, 6.3_

- [x] 18. Checkpoint — Verificar módulos del servidor completos
  - Ensure all tests pass, ask the user if questions arise.

- [x] 19. Implementar middleware global y validaciones cross-cutting
  - [x] 19.1 Implementar GlobalExceptionMiddleware en SistemaAlmacen.Server/Middleware/
    - Interceptar excepciones no controladas: retornar mensaje seguro sin exponer stack traces, cadenas de conexión o SQL
    - Preservar estado de navegación del usuario tras error
    - Manejo diferenciado: ValidationException → 400, UnauthorizedException → 403, DbUpdateException → 500 genérico, Exception → 500 genérico
    - _Requirements: 1.2_

  - [x] 19.2 Implementar validación dual cliente/servidor
    - Configurar EditForm + DataAnnotationsValidator en componentes Blazor del cliente
    - Validar en servidor independientemente de validación de cliente
    - Resaltar campos con error, preservar datos ingresados, mostrar mensajes por campo
    - Implementar timeout de 30 segundos para transacciones de base de datos
    - _Requirements: 12.1, 12.2, 12.3, 12.4, 12.5_

- [x] 20. Implementar interfaz de usuario Blazor — Autenticación y navegación
  - [x] 20.1 Crear layout principal, sistema de navegación y componentes de autenticación en SistemaAlmacen.Client/
    - Implementar MainLayout con menú lateral que oculta opciones según rol
    - Implementar páginas: Login, RecuperarContraseña, RestablecerContraseña
    - Implementar AuthenticationStateProvider personalizado con JWT
    - Redirección automática a login para usuarios no autenticados
    - Redirección según rol tras login exitoso (panel Admin o panel Vendedor)
    - _Requirements: 4.1, 4.5, 5.3, 6.5, 6.7_

  - [ ]* 20.2 Escribir property test para control de acceso por roles
    - **Property 12: Roles restringen acceso correctamente**
    - **Validates: Requirements 6.3, 6.4, 6.5**

- [x] 21. Implementar interfaz de usuario Blazor — Módulos CRUD
  - [x] 21.1 Crear páginas de gestión de usuarios en SistemaAlmacen.Client/Pages/
    - Listado paginado con búsqueda, formulario de creación/edición, confirmación de eliminación
    - Implementar servicios HTTP del cliente para comunicarse con la API
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

  - [x] 21.2 Crear páginas de gestión de categorías y productos
    - Listado de categorías, formulario crear/editar/eliminar categoría
    - Listado de productos con búsqueda y filtro por categoría, formulario crear/editar producto, eliminación lógica
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 8.1, 8.2, 8.3, 8.4, 8.5_

  - [x] 21.3 Crear páginas de registro y consulta de ventas
    - Interfaz de nueva venta: selección de productos, agregar/eliminar detalles, total en tiempo real
    - Selección de medios de pago con montos (pago mixto)
    - Historial de ventas con filtros y detalle
    - _Requirements: 9.1, 9.2, 9.4, 9.8, 10.1, 10.2, 10.3, 17.6, 17.10_

  - [x] 21.4 Crear páginas de gestión de caja
    - Apertura de caja con monto inicial
    - Panel de caja abierta con movimientos, retiros e ingresos
    - Cierre de caja con formulario de monto real y resumen
    - Historial de cierres con filtros
    - _Requirements: 16.1, 16.4, 16.5, 16.6, 16.7, 16.8_

  - [x] 21.5 Crear páginas de medios de pago, auditoría y reportes
    - Gestión de medios de pago (admin): listado, crear, modificar, desactivar
    - Historial de auditoría con filtros (admin)
    - Reportes con visualización en pantalla y botón de exportar
    - Comprobantes: listado de pendientes, reintento, consulta, descarga PDF
    - _Requirements: 17.2, 17.3, 17.4, 13.3, 13.4, 13.5, 13.6, 11.1, 11.6, 15.8_

- [x] 22. Implementar soporte offline y sincronización
  - [x] 22.1 Implementar IConnectivityService e IOfflineStorageService en SistemaAlmacen.Client/Offline/
    - Detectar estado de conexión y mostrar indicador visual permanente (conectado/desconectado)
    - Almacenar ventas pendientes en IndexedDB via JS Interop
    - Cachear catálogo de productos con precios y stock
    - Mostrar contador de operaciones pendientes de sincronización
    - Preservar operaciones pendientes si se cierra el navegador
    - _Requirements: 14.1, 14.2, 14.3, 14.7, 14.8_

  - [x] 22.2 Implementar ISyncEngine para sincronización automática
    - Sincronizar operaciones pendientes en orden cronológico al recuperar conexión
    - Detectar conflictos (stock insuficiente): marcar operación, notificar al Administrador
    - Eliminar copia local tras sincronización exitosa y actualizar datos con respuesta del servidor
    - Emitir comprobantes AFIP pendientes durante sincronización
    - Restringir funcionalidades offline: solo registro de ventas y consulta de catálogo en caché
    - _Requirements: 14.4, 14.5, 14.6, 14.9, 15.11_

  - [x] 22.3 Configurar Service Worker y manifest.json para PWA
    - Cache de assets estáticos para funcionamiento offline
    - Configurar manifest.json con iconos y nombre de aplicación
    - _Requirements: 14.1, 14.2_

  - [ ]* 22.4 Escribir property test para serialización offline
    - **Property 11: Round-trip de serialización offline**
    - **Validates: Requirements 14.2, 14.6**

- [x] 23. Checkpoint — Verificar sistema completo end-to-end
  - Ensure all tests pass, ask the user if questions arise.

- [x] 24. Registrar auditoría en todos los módulos e integración final
  - [x] 24.1 Integrar registro de auditoría en módulos de caja y facturación
    - Registrar en auditoría: apertura/cierre de caja, retiros, ingresos adicionales
    - Registrar en auditoría: emisión de comprobantes (exitosos y fallidos) con CAE, tipo y número
    - _Requirements: 16.11, 15.12_

  - [x] 24.2 Verificar integración completa entre módulos
    - Verificar flujo completo: abrir caja → registrar venta → seleccionar medios de pago → confirmar → emitir factura AFIP → registrar en auditoría → actualizar caja
    - Verificar que desglose por medio de pago aparece en reportes de ventas
    - Verificar acceso por roles en todos los endpoints
    - _Requirements: 9.3, 15.3, 16.3, 17.8, 17.11, 6.5_

- [x] 25. Checkpoint final — Verificar todas las funcionalidades integradas
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Las tareas marcadas con `*` son opcionales y pueden omitirse para un MVP más rápido
- Cada tarea referencia los requisitos específicos que implementa para trazabilidad
- Los checkpoints aseguran validación incremental del progreso
- Los property tests (FsCheck) validan propiedades universales de correctitud
- Los tests unitarios (xUnit) validan ejemplos específicos y edge cases
- El orden de implementación respeta las dependencias: datos → lógica de negocio → API → UI → offline
- La facturación AFIP se diseña como operación no bloqueante: si falla, la venta queda pendiente de facturación

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2"] },
    { "id": 1, "tasks": ["2.1", "3.1", "3.2", "3.3"] },
    { "id": 2, "tasks": ["2.2"] },
    { "id": 3, "tasks": ["2.3", "2.4", "4.1"] },
    { "id": 4, "tasks": ["6.1", "6.2", "7.1", "8.1", "9.1"] },
    { "id": 5, "tasks": ["6.3", "6.4", "6.5", "6.6", "7.2", "7.3", "7.4", "7.5", "8.2", "9.2", "9.3"] },
    { "id": 6, "tasks": ["11.1", "12.1", "13.1", "15.1"] },
    { "id": 7, "tasks": ["11.2", "11.3", "11.4", "11.5", "11.6", "12.2", "12.3", "12.4", "13.2", "13.3", "15.2", "15.3", "15.4"] },
    { "id": 8, "tasks": ["16.1", "17.1"] },
    { "id": 9, "tasks": ["16.2", "16.3", "16.4", "16.5", "16.6", "17.2", "17.3"] },
    { "id": 10, "tasks": ["19.1", "19.2"] },
    { "id": 11, "tasks": ["20.1", "20.2"] },
    { "id": 12, "tasks": ["21.1", "21.2", "21.3", "21.4", "21.5"] },
    { "id": 13, "tasks": ["22.1", "22.2", "22.3", "22.4"] },
    { "id": 14, "tasks": ["24.1", "24.2"] }
  ]
}
```
