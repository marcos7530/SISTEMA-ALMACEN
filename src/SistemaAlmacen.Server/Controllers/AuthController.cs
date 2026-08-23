using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaAlmacen.Business.Interfaces;
using SistemaAlmacen.Shared.DTOs.Auth;

namespace SistemaAlmacen.Server.Controllers;

/// <summary>
/// Controller de autenticación: login, logout y recuperación de contraseña.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IPasswordRecoveryService _passwordRecoveryService;

    public AuthController(IAuthService authService, IPasswordRecoveryService passwordRecoveryService)
    {
        _authService = authService;
        _passwordRecoveryService = passwordRecoveryService;
    }

    /// <summary>
    /// Autentica al usuario con email y contraseña.
    /// Retorna 200 con token JWT si las credenciales son válidas, 401 si son inválidas.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.LoginAsync(request);

        if (result.Success)
            return Ok(result);

        return Unauthorized(result);
    }

    /// <summary>
    /// Cierra la sesión del usuario autenticado.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _authService.LogoutAsync();
        return Ok(new { message = "Sesión cerrada exitosamente." });
    }

    /// <summary>
    /// Solicita recuperación de contraseña. Siempre retorna 200 con mensaje genérico
    /// independientemente de si el email existe (Req 5.2).
    /// </summary>
    [HttpPost("recover-password")]
    [AllowAnonymous]
    public async Task<IActionResult> RecoverPassword([FromBody] RecoverPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { message = "El correo electrónico es requerido." });

        await _passwordRecoveryService.RequestRecoveryAsync(request.Email);

        return Ok(new { message = "Si el correo está registrado, recibirás instrucciones para recuperar tu contraseña." });
    }

    /// <summary>
    /// Restablece la contraseña usando un token de recuperación válido.
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest(new { message = "El token y la nueva contraseña son requeridos." });

        var result = await _passwordRecoveryService.ResetPasswordAsync(request.Token, request.NewPassword);

        if (result.IsSuccess)
            return Ok(new { message = "Contraseña restablecida exitosamente." });

        return BadRequest(new { message = result.ErrorMessage });
    }

    /// <summary>
    /// Valida si un token de recuperación es válido (no expirado, no usado).
    /// </summary>
    [HttpPost("validate-token")]
    [AllowAnonymous]
    public async Task<IActionResult> ValidateToken([FromBody] ValidateTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return BadRequest(new { valid = false, message = "El token es requerido." });

        var isValid = await _passwordRecoveryService.ValidateTokenAsync(request.Token);

        return Ok(new { valid = isValid });
    }
}

/// <summary>
/// Request para solicitar recuperación de contraseña.
/// </summary>
public class RecoverPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Request para restablecer la contraseña con un token de recuperación.
/// </summary>
public class ResetPasswordRequest
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>
/// Request para validar un token de recuperación.
/// </summary>
public class ValidateTokenRequest
{
    public string Token { get; set; } = string.Empty;
}
