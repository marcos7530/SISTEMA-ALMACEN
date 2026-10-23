# Guía de Defensa — Sistema POS (Punto de Venta)

> Proyecto Final — Tecnicatura Superior en Programación
> Documento de estudio para la defensa oral + demo en vivo (~30 min)

---

## Cómo usar este documento

1. **Primero leé la sección "Resumen en 60 segundos"** — es lo que tenés que poder decir de memoria.
2. **Estudiá la sección técnica** (Arquitectura, Tecnologías, Patrones, Base de Datos) hasta poder explicarla con tus palabras, no de memoria.
3. **Practicá la demo** siguiendo el guion, al menos 2 veces completas.
4. **Repasá el banco de preguntas** — ahí está el 80% de lo que te pueden preguntar.
5. La regla de oro: si no sabés algo, **no inventes**. Decí "esa parte la resolví de tal forma, aunque reconozco que se podría mejorar con X". El tribunal valora honestidad y criterio más que saberlo todo.

---

## 1. Resumen en 60 segundos (tu "elevator pitch")

> "Desarrollé un sistema de Punto de Venta (POS) para un comercio tipo almacén. Permite gestionar productos y stock, registrar ventas con múltiples medios de pago, controlar la caja diaria, emitir facturación electrónica de AFIP, llevar cuentas corrientes de clientes y auditar todas las operaciones. Lo construí con una **arquitectura en capas** usando **.NET 10**: el frontend es una aplicación **Blazor WebAssembly** que funciona como **PWA** (puede operar sin conexión a internet), y el backend es una **API REST en ASP.NET Core** con **SQL Server** y **Entity Framework Core**. La seguridad se maneja con **tokens JWT** y contraseñas hasheadas con **BCrypt**. Lo hice completamente solo."

Memorizá la **idea**, no las palabras exactas. Si decís esto con soltura, ya arrancaste bien.

---

## 2. ¿Qué problema resuelve el sistema?

Un comercio necesita, día a día:
- Saber qué productos tiene y cuánto stock le queda.
- Cobrar ventas rápido, aceptando efectivo, tarjeta, transferencia o "fiado" (cuenta corriente).
- Controlar cuánta plata entró y salió de la caja.
- Cumplir con la obligación legal de facturar (AFIP en Argentina).
- Tener un registro de quién hizo qué (auditoría), para control interno.

El sistema reemplaza el cuaderno/planilla manual por una herramienta integrada, con control de acceso por roles y trazabilidad completa.

---

## 3. Módulos del sistema (qué mostrar en la demo)

| Módulo | Qué hace | Rol que lo usa |
|--------|----------|----------------|
| **Autenticación** | Login, recuperación de contraseña por email, bloqueo tras intentos fallidos | Todos |
| **Usuarios** | Alta/baja/modificación de usuarios y asignación de roles | Administrador |
| **Categorías** | ABM de categorías de productos | Admin (edita), Vendedor (lee) |
| **Productos / Stock** | ABM de productos, control de stock, movimientos de stock, reposición por código de barras | Admin (edita), Vendedor (lee) |
| **Ventas** | Registro de venta paso a paso (borrador → confirmada), pago mixto, anulación con reposición de stock | Vendedor y Admin |
| **Medios de Pago** | Gestión de formas de pago (efectivo, tarjetas, transferencia, cuenta corriente) | Administrador |
| **Caja** | Apertura y cierre de caja, retiros, ingresos, arqueo | Vendedor (abre/cierra), Admin (retiros) |
| **Cuenta Corriente** | Ventas "fiadas" a clientes, registro de cargos y pagos | Admin / Vendedor |
| **Facturación AFIP** | Emisión de comprobantes electrónicos, reintentos ante fallo | Administrador |
| **Reportes** | Ventas, productos más vendidos, inventario; exportación a PDF/Excel/CSV | Administrador |
| **Auditoría** | Historial automático de todas las operaciones | Administrador |
| **Offline / PWA** | Registrar ventas sin internet y sincronizarlas al recuperar conexión | Todos |

---

## 4. Arquitectura del sistema

### 4.1 Visión general: arquitectura en capas (N-Tier / Layered Architecture)

El proyecto está dividido en **5 proyectos** (capas), cada uno con una responsabilidad clara. Esto se llama **separación de responsabilidades** (Separation of Concerns).

```
┌─────────────────────────────────────────────────────────┐
│  SistemaAlmacen.Client  (Blazor WebAssembly - PWA)        │  ← Interfaz de usuario (corre en el navegador)
└───────────────────────────┬─────────────────────────────┘
                            │ HTTP (JSON) sobre la API REST
┌───────────────────────────▼─────────────────────────────┐
│  SistemaAlmacen.Server  (ASP.NET Core Web API)            │  ← Controllers, seguridad, endpoints
└───────────────────────────┬─────────────────────────────┘
                            │ llama a servicios
┌───────────────────────────▼─────────────────────────────┐
│  SistemaAlmacen.Business  (Lógica de negocio / Services)  │  ← Reglas del negocio (validaciones, cálculos)
└───────────────────────────┬─────────────────────────────┘
                            │ usa repositorios
┌───────────────────────────▼─────────────────────────────┐
│  SistemaAlmacen.Data  (EF Core, Repositorios, Entidades)  │  ← Acceso a la base de datos
└───────────────────────────┬─────────────────────────────┘
                            ▼
                      SQL Server (Base de datos)

  SistemaAlmacen.Shared  (DTOs, Enums, Validadores)  ← Compartido entre Client y Server
```

### 4.2 Qué hace cada capa (explicación para la defensa)

- **Client (Presentación):** Lo que ve el usuario. Es Blazor WebAssembly, o sea que el código C# se compila a WebAssembly y **corre dentro del navegador** (no en el servidor). Se comunica con el backend mediante llamadas HTTP a la API.

- **Server (API / Presentación del backend):** Expone los **endpoints REST** (`/api/ventas`, `/api/productos`, etc.). Se encarga de la **autenticación y autorización** (validar el token JWT, chequear roles), y de traducir las peticiones HTTP en llamadas a la capa de negocio. No tiene lógica de negocio: solo coordina.

- **Business (Lógica de Negocio):** El **corazón del sistema**. Acá viven las reglas: "no se puede vender sin stock", "la suma de los pagos tiene que coincidir con el total", "no se puede cobrar en efectivo sin una caja abierta". Está organizado en **Services** (un servicio por módulo: `VentaService`, `StockService`, etc.).

- **Data (Acceso a Datos):** Habla con la base de datos a través de **Entity Framework Core**. Contiene las **entidades** (las clases que mapean a las tablas), los **repositorios** (que encapsulan las consultas) y el **Unit of Work** (que coordina las transacciones).

- **Shared (Compartido):** Contiene los **DTOs** (Data Transfer Objects), enums y validadores que usan tanto el cliente como el servidor. Así ambos "hablan el mismo idioma" sin duplicar código.

### 4.3 Regla de oro de la arquitectura: dependencias en una sola dirección

```
Client   → Shared
Server   → Business → Data → Shared
```

Las capas de arriba conocen a las de abajo, **nunca al revés**. La capa de Data no sabe que existe la Business, y la Business no sabe que existe un controller HTTP. Esto hace que el sistema sea **mantenible y testeable**: puedo cambiar la base de datos o la interfaz sin tocar las reglas de negocio.

> **Por qué esto es importante en la defensa:** demuestra que entendés el principio de **bajo acoplamiento** (low coupling) y **alta cohesión** (high cohesion).

---

## 5. Tecnologías utilizadas y por qué las elegí

Esta es **la pregunta estrella** de cualquier defensa. Para cada tecnología tenés que poder decir: *qué es* y *por qué la elegí* (ventaja concreta).

| Tecnología | Qué es | Por qué la elegí |
|-----------|--------|------------------|
| **.NET 10 / C#** | Plataforma y lenguaje de Microsoft | Lenguaje fuertemente tipado, moderno, con gran ecosistema y soporte. Permite compartir código C# entre front y back. |
| **Blazor WebAssembly** | Framework para hacer SPAs con C# que corren en el navegador | Me permitió usar **un solo lenguaje (C#)** en todo el proyecto, en vez de aprender JavaScript/React aparte. Reutilizo DTOs y validaciones. |
| **PWA (Progressive Web App)** | App web instalable que funciona offline | Un comercio no puede parar de vender si se cae internet. La PWA permite **seguir registrando ventas sin conexión** y sincronizar después. |
| **ASP.NET Core Web API** | Framework para construir APIs REST | Estándar de la industria, alto rendimiento, integración nativa con el resto de .NET. |
| **Entity Framework Core 10** | ORM (Object-Relational Mapper) | Me evita escribir SQL a mano: trabajo con objetos C# y EF genera las consultas. Maneja migraciones, relaciones y transacciones. |
| **SQL Server** | Base de datos relacional | Datos estructurados y relacionados (ventas, productos, clientes) con necesidad de **integridad referencial y transacciones**. Un POS maneja dinero: necesito consistencia fuerte (ACID). |
| **JWT (JSON Web Token)** | Estándar de tokens para autenticación | Es **stateless**: el servidor no guarda sesiones, el token lleva la identidad y el rol firmados. Ideal para una API que atiende a un cliente SPA. |
| **BCrypt** | Algoritmo de hashing de contraseñas | Nunca se guardan contraseñas en texto plano. BCrypt es lento a propósito (resistente a fuerza bruta) e incluye *salt* automático. |
| **QuestPDF** | Librería para generar PDFs | Para los comprobantes de venta y reportes en PDF. API moderna y en C#. |
| **ClosedXML** | Librería para generar archivos Excel | Para exportar reportes a Excel sin depender de que haya Office instalado. |
| **xUnit, FsCheck, FluentAssertions, Moq** | Herramientas de testing | Para pruebas unitarias, property-based testing y mocking de dependencias. |

### Preguntas típicas sobre tecnología (con respuesta)

**"¿Por qué Blazor y no Angular/React?"**
Porque me permitía trabajar **con un solo lenguaje (C#)** en todo el stack, reutilizar las clases de DTOs y validaciones entre cliente y servidor, y no tener que mantener dos ecosistemas distintos (npm + nuget). Para un proyecto hecho por una sola persona, reduce muchísimo la complejidad.

**"¿Por qué SQL Server y no MongoDB / una base NoSQL?"**
Porque los datos de un POS están **fuertemente relacionados** (una venta tiene detalles, que referencian productos, que pertenecen a categorías) y necesito **transacciones ACID**: cuando confirmo una venta, se tiene que descontar el stock, registrar los pagos y mover la caja **todo junto o nada**. Una base relacional con transacciones es la elección natural. NoSQL brilla en otros escenarios (datos no estructurados, escala horizontal masiva) que no son mi caso.

**"¿Por qué una API REST y no que Blazor acceda directo a la base?"**
Por **seguridad y separación de responsabilidades**. El cliente corre en el navegador del usuario, no es confiable. Toda la lógica de negocio y el acceso a datos quedan del lado del servidor, detrás de autenticación. El cliente solo pide y muestra datos.

---

## 6. Patrones de diseño utilizados

Esta sección es clave porque pediste énfasis en patrones. Para cada uno: *qué es*, *dónde lo usé*, *qué problema resuelve*.

### 6.1 Repository Pattern (Patrón Repositorio)
- **Qué es:** una capa que encapsula el acceso a datos. En vez de que los servicios escriban consultas de EF por todos lados, hablan con un repositorio (`IProductoRepository`, `IVentaRepository`).
- **Dónde:** carpeta `Data/Repositories`.
- **Qué resuelve:** centraliza las consultas, facilita el testing (puedo reemplazar el repositorio por uno falso/mock) y desacopla la lógica de negocio de EF Core.

### 6.2 Unit of Work (Unidad de Trabajo)
- **Qué es:** un objeto que agrupa varios repositorios y coordina una **transacción única**. Expone `BeginTransactionAsync`, `CommitTransactionAsync`, `RollbackTransactionAsync` y `SaveChangesAsync`.
- **Dónde:** `Data/Repositories/UnitOfWork.cs`.
- **Qué resuelve:** cuando confirmo una venta, tengo que tocar varias tablas (stock, pagos, caja, cuenta corriente). El Unit of Work garantiza que **todo se guarde junto o se revierta junto**. Si algo falla, hace `Rollback` y la base queda intacta. Esto es fundamental porque el sistema maneja dinero.

> **Ejemplo concreto para la demo/defensa:** en `VentaService.ConfirmVentaAsync` arranco con `BeginTransactionAsync()`, voy validando y modificando (stock, pagos, caja), y solo al final hago `CommitTransactionAsync()`. Si cualquier validación falla, hago `RollbackTransactionAsync()` y devuelvo un error claro.

### 6.3 Dependency Injection (Inyección de Dependencias)
- **Qué es:** en vez de que una clase cree sus dependencias, las recibe "desde afuera" (por el constructor). El framework de .NET las provee automáticamente.
- **Dónde:** `Server/Program.cs`, con todos los `builder.Services.AddScoped<IInterfaz, Implementacion>()`.
- **Qué resuelve:** bajo acoplamiento y testeabilidad. Los servicios dependen de **interfaces** (`IStockService`, `IUnitOfWork`), no de clases concretas. Puedo cambiar la implementación sin tocar quien la usa.

### 6.4 DTO (Data Transfer Object)
- **Qué es:** objetos simples que transportan datos entre capas (`VentaDto`, `ProductoDto`, `LoginRequest`).
- **Dónde:** proyecto `Shared`.
- **Qué resuelve:** **no exponer las entidades de la base de datos** directamente al cliente. El DTO lleva solo lo que el cliente necesita (ej: el `VentaDto` no expone campos internos). Mejora seguridad y desacopla el modelo de datos del contrato de la API.

### 6.5 Result Pattern (Patrón Resultado)
- **Qué es:** en vez de lanzar excepciones para errores de negocio, los métodos devuelven un objeto `Result` que dice si salió bien (`Success`) o mal (`Failure` con mensaje y código de error).
- **Dónde:** `Shared/Common`, usado en todos los servicios (`Result<VentaDto>`).
- **Qué resuelve:** manejo de errores **explícito y limpio**. El que llama sabe que puede fallar y lo maneja. Las excepciones quedan para errores verdaderamente inesperados. Además devuelve mensajes claros para mostrar al usuario.

### 6.6 Interceptor (patrón de EF Core)
- **Qué es:** un "gancho" que se ejecuta automáticamente antes de guardar cambios en la base.
- **Dónde:** `Data/Interceptors/AuditoriaInterceptor.cs`.
- **Qué resuelve:** la **auditoría automática**. Cada vez que se crea, modifica o elimina una entidad auditable, el interceptor genera automáticamente un registro en la tabla de auditoría, **dentro de la misma transacción**. Así es imposible que exista un cambio sin su registro de auditoría. No tengo que acordarme de auditar en cada servicio: pasa solo.

### 6.7 Options Pattern
- **Qué es:** forma de .NET para leer configuración fuertemente tipada (`EmailOptions`, config de JWT y AFIP desde `appsettings.json`).
- **Qué resuelve:** la configuración (claves, URLs, credenciales) no está hardcodeada, se lee de archivos de configuración o variables de entorno.

---

## 7. Base de datos

Pediste énfasis acá, así que estudiá bien esta parte.

### 7.1 Enfoque: Code First con EF Core

Usé el enfoque **Code First**: defino las tablas como **clases C# (entidades)** y EF Core genera la base de datos y las **migraciones** automáticamente. Las migraciones se aplican solas al iniciar la app (vía `DataSeeder`).

- **Entidades:** carpeta `Data/Entities`.
- **Configuraciones (Fluent API):** carpeta `Data/Configurations`, donde defino tipos, longitudes, relaciones, índices.
- **Migraciones:** carpeta `Data/Migrations`.

### 7.2 Principales entidades (tablas)

| Entidad | Representa |
|---------|-----------|
| `Usuario` | Usuarios del sistema (con rol, hash de contraseña, control de bloqueo) |
| `Categoria` | Categorías de productos |
| `Producto` | Productos con precio, stock, código de barras |
| `Venta` | Cabecera de una venta (fecha, total, estado, vendedor, cliente) |
| `DetalleVenta` | Cada línea de una venta (producto, cantidad, precio, subtotal) |
| `VentaPago` | Cada pago de una venta (permite pago mixto) |
| `MedioPago` | Formas de pago (efectivo, tarjetas, transferencia, cuenta corriente) |
| `Caja` | Apertura/cierre de caja |
| `CajaMovimiento` | Movimientos de caja (ingresos, retiros, ventas en efectivo) |
| `MovimientoStock` | Historial de cambios de stock (ingresos, bajas) |
| `Cliente` | Clientes (para cuenta corriente y facturación) |
| `MovimientoCuentaCorriente` | Cargos y pagos de cuenta corriente |
| `Comprobante` | Comprobantes de facturación AFIP |
| `Proveedor` / `Compra` / `DetalleCompra` | Gestión de compras a proveedores |
| `AuditoriaLog` | Registro de todas las operaciones |
| `TokenRecuperacion` | Tokens para recuperar contraseña |

### 7.3 Relaciones y normalización

La base está **normalizada** (hasta 3FN en general): cada dato se guarda una sola vez y las relaciones se expresan con claves foráneas.

Ejemplos de relaciones:
- `Venta` **1 → N** `DetalleVenta` (una venta tiene muchos detalles).
- `Venta` **1 → N** `VentaPago` (una venta puede tener varios pagos → **pago mixto**).
- `DetalleVenta` **N → 1** `Producto`.
- `Producto` **N → 1** `Categoria`.
- `Venta` **N → 1** `Cliente` (opcional, solo si es cuenta corriente).

> **Decisión de diseño defendible — separar `VentaPago` de `Venta`:** no puse un solo campo "medio de pago" en la venta, sino una tabla aparte. ¿Por qué? Porque una venta puede pagarse **de varias formas a la vez** (ej: $500 en efectivo + $500 con tarjeta). Esto es **pago mixto**, y la única forma limpia de modelarlo es con una relación 1 a N.

### 7.4 Integridad y transacciones

- **Integridad referencial:** las claves foráneas impiden datos huérfanos (no puede haber un detalle de venta apuntando a un producto que no existe).
- **Transacciones ACID:** las operaciones críticas (confirmar venta, anular venta) corren dentro de una transacción. O se completa todo, o no se toca nada.
- **Soft delete / campo `Activo`:** en vez de borrar registros físicamente (productos, usuarios), los marco como inactivos (`Activo = false`). Así no pierdo el historial ni rompo referencias de ventas pasadas.

---

## 8. Seguridad

### 8.1 Autenticación con JWT
Cuando un usuario hace login correctamente, el servidor genera un **token JWT firmado** que contiene su ID, email y rol. El cliente guarda ese token y lo envía en cada petición (header `Authorization: Bearer ...`). El servidor valida la firma en cada request. Es **stateless**: no se guardan sesiones en el servidor.

El token:
- Está **firmado** con una clave secreta (HMAC-SHA256) → no se puede falsificar.
- **Expira** a los 30 minutos.
- Se valida emisor (`Issuer`), audiencia (`Audience`), vigencia y firma.

### 8.2 Contraseñas con BCrypt
Las contraseñas **nunca** se guardan en texto plano. Se guardan hasheadas con **BCrypt**, que:
- Es un algoritmo **lento a propósito** → resistente a ataques de fuerza bruta.
- Incluye **salt** automático → dos usuarios con la misma contraseña tienen hashes distintos.
- Para verificar, se compara con `BCrypt.Verify`, nunca se "desencripta" (es un hash, es irreversible).

### 8.3 Autorización por roles
Hay dos roles: **Administrador** y **Vendedor**. Con políticas de autorización (`RequireAdmin`, `RequireVendedorOrAdmin`) restrinjo qué puede hacer cada uno. El rol viaja dentro del token JWT.

### 8.4 Protección contra fuerza bruta
Tras varios intentos de login fallidos, la cuenta se **bloquea temporalmente** (15 minutos). Además, los mensajes de error **no revelan** si el email existe o no ("las credenciales son incorrectas", nunca "ese email no existe"), para no dar pistas a un atacante.

### 8.5 Otras medidas
- **CORS** configurado para aceptar solo el origen del cliente.
- **DTOs** para no exponer entidades internas.
- **Middleware global de excepciones** que captura errores y no filtra detalles internos al cliente.
- Configuración sensible (claves, conexión) vía **variables de entorno** en producción.

---

## 9. Funcionalidad offline (PWA) — tu diferencial

Esta es una feature que **impresiona** y conviene mostrarla. Explicación:

- El cliente es una **PWA**: tiene un *Service Worker* que cachea la app, así puede abrirse sin internet.
- Hay un módulo `Offline` en el cliente con:
  - `ConnectivityService`: detecta si hay o no conexión.
  - `OfflineStorageService`: guarda ventas pendientes localmente (en el navegador).
  - `SyncEngine`: cuando vuelve la conexión, **sincroniza automáticamente** las ventas pendientes con el servidor.
- **Caso de uso:** se corta internet, el vendedor sigue cobrando, las ventas quedan guardadas en el dispositivo, y al volver la conexión se suben solas.

> Si te preguntan por conflictos: las ventas offline se encolan y se procesan al reconectar; el servidor sigue siendo la fuente de verdad y revalida stock al sincronizar.

---

## 10. Testing

El proyecto tiene tres niveles de pruebas (carpeta `tests/`):
- **Unit tests** (`Business.Tests`): prueban la lógica de negocio de forma aislada, usando **Moq** para simular dependencias.
- **Property-based testing** (`FsCheck`): en vez de probar casos puntuales, genera cientos de casos aleatorios para verificar propiedades (ej: "el total de una venta siempre es la suma de sus subtotales").
- **Tests de repositorios** (`Data.Tests`): usan **SQLite en memoria** para probar las consultas sin necesitar un SQL Server real.
- **Tests de integración** (`Integration.Tests`): usan `WebApplicationFactory` para probar la API de punta a punta (request HTTP real → respuesta).

> Si te preguntan "¿probaste tu código?": sí, tengo tests unitarios, de integración y property-based, cubriendo la lógica crítica como ventas y stock.

---

## 11. Metodología de desarrollo

Pediste énfasis acá. Puntos para mencionar (ajustá a lo que realmente hiciste):

- **Desarrollo incremental por módulos:** fui construyendo módulo por módulo (primero autenticación y usuarios, después productos/stock, después ventas, etc.), cada uno funcional antes de pasar al siguiente.
- **Control de versiones con Git:** llevé el historial del proyecto con commits incrementales.
- **Desarrollo guiado por requisitos:** cada módulo nació de requisitos concretos (por ejemplo, el manejo de pago mixto, el bloqueo de cuenta, la auditoría automática).
- **Diseño por capas desde el inicio:** definí la arquitectura antes de codear, lo que me permitió trabajar ordenado y testear cada capa por separado.
- **Pruebas automatizadas** para validar la lógica crítica.

> Si seguiste algo tipo Scrum/Kanban personal, iteraciones, o un tablero de tareas, mencionalo. Si no, "desarrollo incremental e iterativo" es una descripción honesta y correcta.

---

## 12. Guion de presentación (para los ~30 min)

### Minuto 0–3 · Apertura
- Presentate y decí el **elevator pitch** (sección 1).
- "Hoy les voy a mostrar el sistema funcionando y después explico las decisiones técnicas."

### Minuto 3–15 · Demo en vivo (ver sección 13)
- Mostrá el sistema funcionando. Es lo que más engancha al tribunal.

### Minuto 15–25 · Explicación técnica
- Mostrá el **diagrama de capas** (sección 4.1) y explicá la arquitectura.
- Contá las **tecnologías y por qué** (sección 5).
- Mencioná **2 o 3 patrones** que más te gusten (Repository + Unit of Work + Inyección de dependencias son los más vistosos).
- Mostrá el **modelo de datos** y contá la decisión de `VentaPago` (pago mixto).
- Cerrá con **seguridad** (JWT + BCrypt + roles).

### Minuto 25–30 · Cierre y preguntas
- Resumí: "En síntesis, es un sistema en capas, seguro, testeado, que resuelve un problema real de un comercio."
- Mencioná **mejoras futuras** (muestra madurez): reportes más avանzados, app móvil nativa, multi-sucursal, integración de pagos con QR.
- Abrí el espacio a preguntas.

---

## 13. Guion de la DEMO en vivo (paso a paso)

> **Antes de empezar:** tené la app corriendo (`dotnet run --project src/SistemaAlmacen.Server`), la base con datos de prueba, y el navegador abierto. **Probá la demo entera al menos 2 veces antes.**

**Secuencia recomendada (cuenta una historia: "un día en el comercio"):**

1. **Login** como administrador (`admin@sistema.local` / `Admin123!`). Comentá: "la autenticación usa JWT".
2. **Mostrar productos y stock.** Crear una categoría y un producto nuevo. Comentá el control de stock.
3. **Abrir la caja** (como paso previo a vender). Comentá: "no se puede cobrar en efectivo sin caja abierta, es una regla de negocio".
4. **Registrar una venta:**
   - Agregar productos al carrito.
   - Mostrar que valida stock.
   - Confirmar con **pago mixto** (parte efectivo, parte tarjeta) → este es el momento "wow".
5. **Mostrar el historial de ventas** y el detalle de la venta recién hecha.
6. **Anular una venta** y mostrar que el **stock se repone** automáticamente.
7. **Mostrar un reporte** y exportarlo a PDF o Excel.
8. **Mostrar la auditoría:** señalá que todas las operaciones anteriores quedaron registradas automáticamente.
9. **(Opcional, si tenés tiempo y anda bien) Demo offline:** cortá la conexión (DevTools → Network → Offline), registrá una venta, volvé a conectar y mostrá cómo sincroniza.
10. **Cerrar la caja** y mostrar el arqueo.

> **Plan B si algo falla en la demo:** tené **capturas de pantalla** de cada paso. Si la demo en vivo se rompe, mostrás las capturas y seguís. Nunca te quedes trabado peleando con un error en vivo: "tengo capturas por si la conexión fallaba", y adelante.

---

## 14. Banco de preguntas del tribunal (con respuestas)

### Arquitectura
**P: ¿Qué arquitectura usaste y por qué?**
R: Arquitectura en capas (N-capas). Separé presentación, lógica de negocio y acceso a datos en proyectos distintos. Lo hice para tener separación de responsabilidades, bajo acoplamiento y poder testear cada capa por separado. Las dependencias van en una sola dirección: las capas superiores conocen a las inferiores, nunca al revés.

**P: ¿Qué pasa si querés cambiar de SQL Server a otra base?**
R: Como el acceso a datos está aislado en la capa Data detrás de repositorios y EF Core, el impacto es mínimo. EF Core soporta varios motores; cambiaría el proveedor y la cadena de conexión, sin tocar la lógica de negocio.

**P: ¿La capa de negocio conoce a HTTP?**
R: No. La capa Business no sabe nada de HTTP ni de controllers. Trabaja con DTOs y objetos de dominio. Eso me permite reutilizarla desde otro tipo de cliente si hiciera falta.

### Base de datos
**P: ¿Code First o Database First?**
R: Code First. Defino las entidades en C# y EF Core genera la base y las migraciones. Me da control del esquema desde el código y versionado de cambios con migraciones.

**P: ¿La base está normalizada?**
R: Sí, en general hasta la 3ª forma normal. Evito redundancia y uso claves foráneas para la integridad. Un ejemplo es la separación de `Venta`, `DetalleVenta` y `VentaPago`.

**P: ¿Cómo manejás que una venta tenga varios pagos?**
R: Con una tabla aparte `VentaPago` en relación 1 a N con `Venta`. Así soporto pago mixto (efectivo + tarjeta, por ejemplo) sin limitar a un solo medio por venta.

**P: ¿Borrás registros físicamente?**
R: No, uso soft delete con un campo `Activo`. Así conservo el historial y no rompo referencias de ventas antiguas.

### Transacciones y consistencia
**P: ¿Qué pasa si falla algo en el medio de confirmar una venta?**
R: Toda la confirmación corre dentro de una transacción coordinada por el Unit of Work. Si cualquier paso falla (stock, pago, caja), hago rollback y la base queda exactamente como estaba. Nunca queda una venta a medias.

**P: ¿Qué es ACID?**
R: Son las propiedades de una transacción: Atomicidad (todo o nada), Consistencia (la base queda en estado válido), Aislamiento (las transacciones no se pisan) y Durabilidad (una vez confirmado, persiste). Mi sistema las aprovecha para operaciones con dinero.

### Seguridad
**P: ¿Cómo guardás las contraseñas?**
R: Hasheadas con BCrypt, nunca en texto plano. BCrypt agrega salt automático y es lento a propósito para resistir fuerza bruta. Es irreversible: para validar comparo hashes, no "desencripto".

**P: ¿Qué es un JWT y por qué lo usás?**
R: Es un token firmado que lleva la identidad y el rol del usuario. Lo uso porque es stateless: el servidor no guarda sesión, valida la firma del token en cada request. Encaja perfecto con una API que atiende a un cliente SPA.

**P: ¿Qué pasa si alguien roba un token?**
R: Es un riesgo real de JWT. Lo mitigo con expiración corta (30 min) y HTTPS para que no viaje en claro. Como mejora futura se podría implementar refresh tokens y revocación.

**P: ¿Cómo evitás ataques de fuerza bruta al login?**
R: Bloqueo temporal de la cuenta tras varios intentos fallidos, y mensajes de error genéricos que no revelan si el email existe.

### Patrones
**P: ¿Qué patrones de diseño aplicaste?**
R: Repository y Unit of Work para el acceso a datos, Inyección de Dependencias en todo el sistema, DTO para transferir datos sin exponer entidades, Result Pattern para manejo de errores explícito, e Interceptor de EF Core para la auditoría automática.

**P: ¿Qué es inyección de dependencias y qué ganás?**
R: Es recibir las dependencias desde afuera (por constructor) en vez de crearlas adentro. Gano bajo acoplamiento y testeabilidad: mis clases dependen de interfaces, así que puedo reemplazar implementaciones o usar mocks en los tests.

### Testing
**P: ¿Cómo probaste el sistema?**
R: Con tests unitarios de la lógica de negocio (usando Moq para aislar dependencias), property-based testing con FsCheck, tests de repositorios con SQLite en memoria, y tests de integración de la API con WebApplicationFactory.

### Generales / trampa
**P: Si tuvieras que hacerlo de nuevo, ¿qué cambiarías?**
R: (Respuesta honesta que muestra madurez) Por ejemplo, implementar refresh tokens, mejorar la búsqueda de tokens de recuperación con un repositorio dedicado en vez de recorrer usuarios, y agregar más cobertura de tests de integración. Mostrar que sos consciente de las limitaciones suma puntos.

**P: ¿Lo hiciste solo?**
R: Sí, desarrollé todo el sistema de forma individual: la arquitectura, el backend, el frontend, la base de datos y las pruebas.

**P: ¿Qué fue lo más difícil?**
R: (Elegí algo real) Por ejemplo: coordinar la transacción de confirmación de venta, que toca stock, pagos, caja y cuenta corriente, garantizando que todo sea atómico y consistente. O la sincronización offline.

---

## 15. Glosario rápido (términos que podrías tener que definir)

- **API REST:** forma de comunicar sistemas por HTTP usando recursos y verbos (GET, POST, etc.).
- **ORM:** herramienta que mapea objetos del código a tablas de la base (EF Core).
- **DTO:** objeto simple para transportar datos entre capas.
- **JWT:** token firmado que transporta la identidad del usuario.
- **Hash:** transformación irreversible de un dato (usado para contraseñas).
- **Salt:** dato aleatorio que se agrega antes de hashear para que hashes iguales no se repitan.
- **Transacción ACID:** operación "todo o nada" sobre la base.
- **Migración:** script versionado que modifica el esquema de la base.
- **PWA:** app web instalable que puede funcionar offline.
- **WebAssembly:** formato que permite correr código (C# compilado) dentro del navegador.
- **Inyección de dependencias:** proveer a una clase sus dependencias desde afuera.
- **Middleware:** componente que procesa cada request HTTP en una cadena.
- **Interceptor:** gancho que se ejecuta automáticamente en cierto punto (antes de guardar en la base).
- **Stateless:** el servidor no guarda estado de sesión entre peticiones.
- **Normalización:** organizar la base para minimizar redundancia.
- **Soft delete:** marcar un registro como inactivo en vez de borrarlo.

---

## 16. Checklist final antes de la defensa

- [ ] Puedo decir el elevator pitch de memoria y con soltura.
- [ ] La app corre sin errores desde cero (`dotnet run`).
- [ ] Probé la demo completa al menos 2 veces.
- [ ] Tengo capturas de pantalla de respaldo por si falla la demo.
- [ ] Sé explicar las 5 capas y por qué están separadas.
- [ ] Puedo justificar cada tecnología elegida (sección 5).
- [ ] Puedo explicar al menos 3 patrones de diseño.
- [ ] Sé explicar el modelo de datos y la decisión de pago mixto.
- [ ] Sé explicar JWT, BCrypt y roles sin trabarme.
- [ ] Tengo pensadas 2–3 mejoras futuras para el cierre.
- [ ] Preparé respuestas para "¿qué fue lo más difícil?" y "¿qué cambiarías?".

---

> **Consejo final:** el tribunal no espera perfección, espera que **entiendas lo que hiciste y por qué**. Hablá con seguridad de lo que sabés, sé honesto con lo que mejorarías, y dejá que la demo hable por vos. ¡Éxitos!
