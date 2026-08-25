using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace SistemaAlmacen.Client.Services;

/// <summary>
/// Custom AuthenticationStateProvider that reads a JWT token from localStorage
/// and provides the authentication state to Blazor components.
/// Sets the Authorization header on the shared HttpClient immediately upon authentication.
/// </summary>
public class JwtAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IJSRuntime _jsRuntime;
    private readonly HttpClient _httpClient;

    private const string TokenKey = "authToken";

    /// <summary>
    /// Cache en memoria del token para evitar race conditions.
    /// Se setea en MarkUserAsAuthenticated y en GetAuthenticationStateAsync.
    /// </summary>
    private string? _cachedToken;

    public JwtAuthenticationStateProvider(IJSRuntime jsRuntime, HttpClient httpClient)
    {
        _jsRuntime = jsRuntime;
        _httpClient = httpClient;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // Si ya tenemos el token en cache, usarlo directamente sin JS interop
        var token = _cachedToken ?? await GetTokenAsync();

        if (string.IsNullOrWhiteSpace(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        // Cachear y setear header inmediatamente
        _cachedToken = token;
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var claims = ParseClaimsFromJwt(token);
        var identity = new ClaimsIdentity(claims, "jwt");
        var user = new ClaimsPrincipal(identity);

        return new AuthenticationState(user);
    }

    public async Task MarkUserAsAuthenticated(string token)
    {
        // Cachear y setear header INMEDIATAMENTE (antes del await a JS)
        _cachedToken = token;
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        await SetTokenAsync(token);

        var claims = ParseClaimsFromJwt(token);
        var identity = new ClaimsIdentity(claims, "jwt");
        var user = new ClaimsPrincipal(identity);

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    public async Task MarkUserAsLoggedOut()
    {
        _cachedToken = null;
        _httpClient.DefaultRequestHeaders.Authorization = null;

        await RemoveTokenAsync();

        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(anonymous)));
    }

    private async Task<string?> GetTokenAsync()
    {
        try
        {
            var token = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", TokenKey);

            // Cachear y setear header al leerlo de localStorage
            if (!string.IsNullOrWhiteSpace(token))
            {
                _cachedToken = token;
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            return token;
        }
        catch
        {
            // During prerendering, JS interop is not available
            return _cachedToken; // retornar cache si existe
        }
    }

    private async Task SetTokenAsync(string token)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenKey, token);
    }

    private async Task RemoveTokenAsync()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey);
    }

    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(jwt);
        return token.Claims;
    }
}
