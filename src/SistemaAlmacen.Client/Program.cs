using System.Globalization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SistemaAlmacen.Client;
using SistemaAlmacen.Client.Offline;
using SistemaAlmacen.Client.Services;

// --- Culture: Peso argentino ($) as currency symbol, es-AR number formatting ---
var cultureArgentina = new CultureInfo("es-AR");
CultureInfo.DefaultThreadCurrentCulture = cultureArgentina;
CultureInfo.DefaultThreadCurrentUICulture = cultureArgentina;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// --- HttpClient configured to point to server base address ---
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

// --- Authentication ---
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, JwtAuthenticationStateProvider>();

// --- Offline Services ---
builder.Services.AddScoped<IConnectivityService, ConnectivityService>();
builder.Services.AddScoped<IOfflineStorageService, OfflineStorageService>();
builder.Services.AddScoped<ISyncEngine, SyncEngine>();

await builder.Build().RunAsync();
