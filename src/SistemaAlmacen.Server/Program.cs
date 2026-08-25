using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SistemaAlmacen.Data.Context;

var builder = WebApplication.CreateBuilder(args);

// --- Authentication (JWT Bearer) ---
var jwtSection = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSection["Key"] ?? "SuperSecretKeyForDevelopment12345678!");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSection["Issuer"] ?? "SistemaAlmacen",
        ValidAudience = jwtSection["Audience"] ?? "SistemaAlmacen",
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy =>
        policy.RequireRole("Administrador"));
    options.AddPolicy("RequireVendedorOrAdmin", policy =>
        policy.RequireRole("Administrador", "Vendedor"));
});

// --- CORS ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient", policy =>
    {
        policy.WithOrigins(
                builder.Configuration["ClientUrl"] ?? "https://localhost:5001")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// --- Controllers ---
builder.Services.AddControllers();

// --- OpenAPI / Swagger ---
builder.Services.AddOpenApi();

// --- Business & Data Services ---
builder.Services.AddScoped<SistemaAlmacen.Data.Repositories.IUnitOfWork, SistemaAlmacen.Data.Repositories.UnitOfWork>();
builder.Services.AddScoped<SistemaAlmacen.Business.Interfaces.IAuthService, SistemaAlmacen.Business.Services.AuthService>();
builder.Services.AddScoped<SistemaAlmacen.Business.Interfaces.ITokenService, SistemaAlmacen.Business.Services.TokenService>();
builder.Services.AddScoped<SistemaAlmacen.Business.Interfaces.ISessionService, SistemaAlmacen.Business.Services.SessionService>();
builder.Services.AddScoped<SistemaAlmacen.Business.Interfaces.IUsuarioService, SistemaAlmacen.Business.Services.UsuarioService>();
builder.Services.AddScoped<SistemaAlmacen.Business.Interfaces.ICategoriaService, SistemaAlmacen.Business.Services.CategoriaService>();
builder.Services.AddScoped<SistemaAlmacen.Business.Interfaces.IProductoService, SistemaAlmacen.Business.Services.ProductoService>();
builder.Services.AddScoped<SistemaAlmacen.Business.Interfaces.IPasswordRecoveryService, SistemaAlmacen.Business.Services.PasswordRecoveryService>();
builder.Services.AddScoped<SistemaAlmacen.Business.Interfaces.ICajaService, SistemaAlmacen.Business.Services.CajaService>();
builder.Services.AddScoped<SistemaAlmacen.Business.Interfaces.IAuditoriaService, SistemaAlmacen.Business.Services.AuditoriaService>();
builder.Services.AddScoped<SistemaAlmacen.Business.Interfaces.IMedioPagoService, SistemaAlmacen.Business.Services.MedioPagoService>();
builder.Services.AddScoped<SistemaAlmacen.Business.Interfaces.IVentaService, SistemaAlmacen.Business.Services.VentaService>();
builder.Services.AddScoped<SistemaAlmacen.Business.Interfaces.IStockService, SistemaAlmacen.Business.Services.StockService>();
builder.Services.AddScoped<SistemaAlmacen.Business.Interfaces.IAfipClientWrapper, SistemaAlmacen.Business.Services.AfipClientWrapper>();
builder.Services.AddScoped<SistemaAlmacen.Business.Interfaces.IFacturacionService, SistemaAlmacen.Business.Services.FacturacionService>();
builder.Services.AddScoped<SistemaAlmacen.Business.Interfaces.IReporteService, SistemaAlmacen.Business.Services.ReporteService>();

// --- HttpContextAccessor (necesario para AuditoriaInterceptor) ---
builder.Services.AddHttpContextAccessor();

// --- Interceptor de Auditoría ---
builder.Services.AddScoped<SistemaAlmacen.Data.Interceptors.AuditoriaInterceptor>();

// --- Database (Entity Framework Core) ---
builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
    options
        .UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sql => sql.CommandTimeout(30))
        .AddInterceptors(serviceProvider.GetRequiredService<SistemaAlmacen.Data.Interceptors.AuditoriaInterceptor>()));


var app = builder.Build();

// --- Seed de datos iniciales ---
await SistemaAlmacen.Data.Context.DataSeeder.SeedAsync(app.Services);

// --- HTTP Request Pipeline ---

// Global Exception Middleware
app.UseMiddleware<SistemaAlmacen.Server.Middleware.GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseWebAssemblyDebugging();
}
else
{
    // Solo redirigir a HTTPS en produccion para evitar que las API calls
    // pierdan el header Authorization al ser redirigidas.
    app.UseHttpsRedirection();
}

// Serve Blazor WASM static files
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseCors("AllowBlazorClient");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Fallback to Blazor WASM index.html for SPA routing
app.MapFallbackToFile("index.html");

app.Run();
