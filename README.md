# Sistema POS - Punto de Venta

Sistema integral de Punto de Venta desarrollado con **Blazor WebAssembly (PWA)** y **ASP.NET Core Web API**, diseñado para gestionar ventas, inventario, caja, facturación electrónica AFIP y auditoría.

---

## Requisitos Previos

| Herramienta | Versión mínima | Descarga |
|-------------|---------------|----------|
| .NET SDK | 10.0 | https://dotnet.microsoft.com/download |
| SQL Server | 2019+ o LocalDB | Incluido con Visual Studio |
| Node.js (opcional) | 18+ | Solo si se modifican assets del Service Worker |

> **Nota:** El proyecto usa `(localdb)\mssqllocaldb` por defecto, que viene instalado con Visual Studio. Si no lo tenés, podés usar SQL Server Express o cambiar la cadena de conexión.

### Verificar instalación

```powershell
dotnet --version    # Debe mostrar 10.0.x
sqllocaldb info     # Debe listar MSSQLLocalDB
```

---

## Configuración Rápida

### 1. Clonar el repositorio

```powershell
git clone <url-del-repositorio>
cd "SISTEMA ALMACEN"
```

### 2. Restaurar paquetes NuGet

```powershell
dotnet restore SistemaAlmacen.slnx
```

### 3. Configurar la base de datos

La cadena de conexión por defecto en `src/SistemaAlmacen.Server/appsettings.json` es:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=SistemaAlmacen;Trusted_Connection=True;MultipleActiveResultSets=true"
}
```

**Si usás otra instancia de SQL Server**, editá este valor. Ejemplos:

```
-- SQL Server Express
Server=.\SQLEXPRESS;Database=SistemaAlmacen;Trusted_Connection=True;MultipleActiveResultSets=true

-- SQL Server con usuario/contraseña
Server=mi-servidor;Database=SistemaAlmacen;User Id=sa;Password=MiPassword123;TrustServerCertificate=True
```

### 4. Aplicar migraciones y datos iniciales

La aplicación aplica las migraciones automáticamente al iniciar (via `DataSeeder.SeedAsync`). No necesitás ejecutar `dotnet ef database update` manualmente.

Si preferís aplicar la migración por separado:

```powershell
dotnet ef database update --project src/SistemaAlmacen.Data --startup-project src/SistemaAlmacen.Server
```

### 5. Ejecutar la aplicación

```powershell
dotnet run --project src/SistemaAlmacen.Server
```

La aplicación se levanta en:
- **HTTPS:** https://localhost:5001 (o el puerto asignado)
- **HTTP:** http://localhost:5000

> El servidor hospeda tanto la API como el cliente Blazor WASM. Solo necesitás ejecutar el proyecto Server.

### 6. Acceder al sistema

Abrí el navegador en la URL que muestra la consola (ej: `https://localhost:5001`).

**Credenciales del administrador inicial:**

| Campo | Valor |
|-------|-------|
| Email | `admin@sistema.local` |
| Contraseña | `Admin123!` |

---

## Estructura del Proyecto

```
SistemaAlmacen.slnx
├── src/
│   ├── SistemaAlmacen.Server/        # ASP.NET Core Web API (proyecto de inicio)
│   ├── SistemaAlmacen.Client/        # Blazor WebAssembly (PWA)
│   ├── SistemaAlmacen.Business/      # Lógica de Negocio (servicios)
│   ├── SistemaAlmacen.Data/          # Acceso a Datos (EF Core, repositorios)
│   └── SistemaAlmacen.Shared/        # DTOs, enums, validadores compartidos
└── tests/
    ├── SistemaAlmacen.Business.Tests/ # Tests unitarios y property tests
    ├── SistemaAlmacen.Data.Tests/     # Tests de repositorios (SQLite in-memory)
    └── SistemaAlmacen.Integration.Tests/ # Tests de integración (WebApplicationFactory)
```

### Flujo de dependencias

```
Client → Shared
Server → Business → Data → Shared
Server → Shared
Server → Client (hosting)
```

---

## Comandos Útiles

### Compilar toda la solución

```powershell
dotnet build SistemaAlmacen.slnx
```

### Ejecutar tests

```powershell
dotnet test SistemaAlmacen.slnx
```

### Ejecutar en modo desarrollo (con hot reload)

```powershell
dotnet watch run --project src/SistemaAlmacen.Server
```

### Agregar una nueva migración

```powershell
dotnet ef migrations add NombreDeLaMigracion --project src/SistemaAlmacen.Data --startup-project src/SistemaAlmacen.Server
```

### Revertir última migración

```powershell
dotnet ef migrations remove --project src/SistemaAlmacen.Data --startup-project src/SistemaAlmacen.Server
```

### Publicar para producción

```powershell
dotnet publish src/SistemaAlmacen.Server -c Release -o ./publish
```

---

## Configuración

### appsettings.json (Server)

| Sección | Descripción |
|---------|-------------|
| `ConnectionStrings:DefaultConnection` | Cadena de conexión a SQL Server |
| `Jwt:Key` | Clave secreta para firmar tokens JWT (mínimo 32 caracteres) |
| `Jwt:Issuer` | Emisor del token |
| `Jwt:Audience` | Audiencia del token |
| `Jwt:ExpirationMinutes` | Tiempo de expiración del token (default: 30 min) |
| `Afip:UseMock` | `true` = sin conexión real a AFIP, `false` = producción |
| `Afip:Cuit` | CUIT del contribuyente |
| `Afip:PuntoDeVenta` | Punto de venta habilitado en AFIP |
| `Afip:ModoProduccion` | `false` = homologación, `true` = producción |

### Variables de entorno (producción)

Para producción, sobrescribí las configuraciones sensibles con variables de entorno:

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=prod-server;Database=SistemaAlmacenProd;..."
$env:Jwt__Key = "ClaveSuperSecretaDeProduccion64CaracteresMinimo!!"
```

---

## Módulos del Sistema

| Módulo | Descripción | Roles |
|--------|-------------|-------|
| Autenticación | Login, logout, recuperación de contraseña, bloqueo por intentos | Todos |
| Usuarios | ABM de usuarios con roles | Administrador |
| Categorías | ABM de categorías de productos | Admin (CRUD), Vendedor (lectura) |
| Productos | ABM con búsqueda, filtros, stock | Admin (CRUD), Vendedor (lectura) |
| Ventas | Registro de ventas, detalle, historial | Vendedor y Admin |
| Medios de Pago | Gestión de métodos de pago, pago mixto | Administrador |
| Caja | Apertura, cierre, retiros, ingresos | Vendedor (abrir/cerrar), Admin (retiros/ingresos) |
| Facturación | Emisión de comprobantes AFIP, reintentos | Administrador |
| Reportes | Ventas, productos más vendidos, inventario | Administrador |
| Auditoría | Historial de todas las operaciones | Administrador |
| Offline/PWA | Ventas sin conexión, sincronización automática | Todos |

---

## API Endpoints

La API está documentada con OpenAPI/Swagger. En modo desarrollo, accedé a:

```
https://localhost:5001/openapi/v1.json
```

### Endpoints principales

| Método | Ruta | Descripción |
|--------|------|-------------|
| POST | `/api/auth/login` | Iniciar sesión |
| POST | `/api/auth/recover-password` | Solicitar recuperación |
| POST | `/api/auth/reset-password` | Restablecer contraseña |
| GET | `/api/usuarios` | Listar usuarios (paginado) |
| GET | `/api/categorias` | Listar categorías |
| GET | `/api/productos` | Listar productos (paginado + filtros) |
| POST | `/api/ventas` | Iniciar nueva venta |
| POST | `/api/ventas/{id}/detalles` | Agregar producto a venta |
| POST | `/api/ventas/{id}/confirmar` | Confirmar venta con pagos |
| GET | `/api/ventas` | Historial de ventas |
| POST | `/api/caja/abrir` | Abrir caja |
| POST | `/api/caja/cerrar` | Cerrar caja |
| GET | `/api/reportes/ventas` | Reporte de ventas |
| POST | `/api/reportes/exportar` | Exportar a PDF/Excel/CSV |
| POST | `/api/facturacion/emitir/{id}` | Emitir comprobante AFIP |
| GET | `/api/auditoria` | Historial de auditoría |

---

## Tecnologías Utilizadas

| Categoría | Tecnología |
|-----------|-----------|
| Frontend | Blazor WebAssembly (.NET 10), PWA |
| Backend | ASP.NET Core 10 Web API |
| Base de Datos | SQL Server + Entity Framework Core 10 |
| Autenticación | JWT Bearer |
| Hashing | BCrypt.Net |
| PDF | QuestPDF |
| Excel | ClosedXML |
| Facturación | Wrapper AFIP (mock para desarrollo) |
| Tests | xUnit, FsCheck, FluentAssertions, Moq |

---

## Datos Iniciales (Seed)

Al ejecutar por primera vez, el sistema crea automáticamente:

**Usuario administrador:**
- Email: `admin@sistema.local`
- Contraseña: `Admin123!`
- Rol: Administrador

**Medios de pago:**
1. Efectivo (sistema, no eliminable)
2. Tarjeta de Débito
3. Tarjeta de Crédito
4. Transferencia Bancaria

---

## Despliegue en Producción

### Checklist pre-deploy

- [ ] Cambiar `Jwt:Key` por una clave segura de al menos 64 caracteres
- [ ] Configurar cadena de conexión apuntando al servidor de producción
- [ ] Cambiar contraseña del usuario admin
- [ ] Configurar `Afip:UseMock = false` si se va a facturar
- [ ] Configurar certificado digital AFIP
- [ ] Configurar HTTPS con certificado válido
- [ ] Configurar logs a un servicio de monitoreo

### Despliegue con IIS

1. Publicar: `dotnet publish src/SistemaAlmacen.Server -c Release -o ./publish`
2. Crear sitio en IIS apuntando a la carpeta `publish`
3. Instalar el ASP.NET Core Hosting Bundle
4. Configurar pool de aplicaciones en "Sin código administrado"

### Despliegue con Docker (ejemplo)

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/SistemaAlmacen.Server/SistemaAlmacen.Server.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SistemaAlmacen.Server.dll"]
```

---

## Solución de Problemas

### "Cannot connect to SQL Server"

- Verificar que SQL Server/LocalDB está corriendo: `sqllocaldb start MSSQLLocalDB`
- Verificar la cadena de conexión en `appsettings.json`

### "Port already in use"

- Cambiar el puerto en `Properties/launchSettings.json` del proyecto Server
- O usar: `dotnet run --project src/SistemaAlmacen.Server --urls "https://localhost:7001"`

### La migración falla

- Asegurarse de que la base de datos existe o que el usuario tiene permisos para crearla
- Probar con: `dotnet ef database update --project src/SistemaAlmacen.Data --startup-project src/SistemaAlmacen.Server --verbose`

### El cliente Blazor no carga

- Limpiar caché del navegador (Ctrl+Shift+Delete)
- Desregistrar Service Workers en DevTools → Application → Service Workers
- Rebuild: `dotnet clean && dotnet build`

---

## Contribución

1. Crear una rama desde `main`: `git checkout -b feature/mi-feature`
2. Implementar cambios
3. Verificar: `dotnet build SistemaAlmacen.slnx && dotnet test SistemaAlmacen.slnx`
4. Commit y push
5. Crear Pull Request

---

## Licencia

Proyecto privado. Todos los derechos reservados.
