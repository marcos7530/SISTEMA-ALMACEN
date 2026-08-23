using System.Text.RegularExpressions;
using SistemaAlmacen.Business.Interfaces;
using SistemaAlmacen.Data.Entities;
using SistemaAlmacen.Data.Repositories;
using SistemaAlmacen.Shared.Common;
using SistemaAlmacen.Shared.DTOs;
using SistemaAlmacen.Shared.DTOs.Usuarios;
using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Business.Services;

/// <summary>
/// Implementación del servicio de gestión de usuarios.
/// </summary>
public class UsuarioService : IUsuarioService
{
    private readonly IUnitOfWork _unitOfWork;

    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public UsuarioService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<PaginatedResult<UsuarioDto>> GetUsuariosAsync(UsuarioFilter filter)
    {
        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize < 1 ? 20 : filter.PageSize;

        var result = await _unitOfWork.Usuarios.GetActiveUsuariosAsync(
            filter.SearchTerm, page, pageSize);

        return new PaginatedResult<UsuarioDto>
        {
            Items = result.Items.Select(MapToDto).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
    }

    /// <inheritdoc />
    public async Task<UsuarioDto?> GetByIdAsync(int id)
    {
        var usuario = await _unitOfWork.Usuarios.GetByIdAsync(id);

        if (usuario is null || !usuario.Activo)
            return null;

        return MapToDto(usuario);
    }

    /// <inheritdoc />
    public async Task<Result<UsuarioDto>> CreateAsync(CreateUsuarioRequest request)
    {
        // Validaciones de entrada
        var validationErrors = ValidateCreateRequest(request);
        if (validationErrors.Count > 0)
            return Result<UsuarioDto>.ValidationFailure(validationErrors);

        // Verificar unicidad de email entre usuarios activos
        var existingUser = await _unitOfWork.Usuarios.GetByEmailAsync(request.Email);
        if (existingUser is not null && existingUser.Activo)
        {
            return Result<UsuarioDto>.Failure(
                "El correo electrónico ya se encuentra registrado en un usuario activo.",
                "EMAIL_DUPLICADO");
        }

        // Crear entidad
        var usuario = new Usuario
        {
            Nombre = request.Nombre.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Rol = request.Rol,
            Activo = true,
            IntentosFallidos = 0,
            FechaCreacion = DateTime.UtcNow
        };

        await _unitOfWork.Usuarios.AddAsync(usuario);
        await _unitOfWork.SaveChangesAsync();

        return Result<UsuarioDto>.Success(MapToDto(usuario));
    }

    /// <inheritdoc />
    public async Task<Result<UsuarioDto>> UpdateAsync(int id, UpdateUsuarioRequest request)
    {
        // Buscar usuario activo
        var usuario = await _unitOfWork.Usuarios.GetByIdAsync(id);
        if (usuario is null || !usuario.Activo)
        {
            return Result<UsuarioDto>.Failure(
                "El usuario no fue encontrado.",
                "USUARIO_NO_ENCONTRADO");
        }

        // Validaciones de entrada
        var validationErrors = ValidateUpdateRequest(request);
        if (validationErrors.Count > 0)
            return Result<UsuarioDto>.ValidationFailure(validationErrors);

        // Verificar unicidad de email (excluyendo al usuario actual)
        var existingUser = await _unitOfWork.Usuarios.GetByEmailAsync(request.Email);
        if (existingUser is not null && existingUser.Activo && existingUser.Id != id)
        {
            return Result<UsuarioDto>.Failure(
                "El correo electrónico ya se encuentra registrado en otro usuario activo.",
                "EMAIL_DUPLICADO");
        }

        // Actualizar campos
        usuario.Nombre = request.Nombre.Trim();
        usuario.Email = request.Email.Trim().ToLowerInvariant();
        usuario.Rol = request.Rol;

        _unitOfWork.Usuarios.Update(usuario);
        await _unitOfWork.SaveChangesAsync();

        return Result<UsuarioDto>.Success(MapToDto(usuario));
    }

    /// <inheritdoc />
    public async Task<Result> DeactivateAsync(int id, int currentUserId)
    {
        // No permitir eliminar cuenta propia
        if (id == currentUserId)
        {
            return Result.Failure(
                "No es posible eliminar el usuario con sesión activa.",
                "ELIMINAR_CUENTA_PROPIA");
        }

        // Buscar usuario activo
        var usuario = await _unitOfWork.Usuarios.GetByIdAsync(id);
        if (usuario is null || !usuario.Activo)
        {
            return Result.Failure(
                "El usuario no fue encontrado.",
                "USUARIO_NO_ENCONTRADO");
        }

        // No permitir eliminar último administrador
        if (usuario.Rol == Rol.Administrador)
        {
            var activeAdminCount = await _unitOfWork.Usuarios.CountActiveAdminsAsync();
            if (activeAdminCount <= 1)
            {
                return Result.Failure(
                    "No se puede eliminar el último Administrador activo del sistema.",
                    "ULTIMO_ADMIN");
            }
        }

        // Eliminación lógica
        usuario.Activo = false;
        _unitOfWork.Usuarios.Update(usuario);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    #region Private Methods

    private static UsuarioDto MapToDto(Usuario usuario)
    {
        return new UsuarioDto
        {
            Id = usuario.Id,
            Nombre = usuario.Nombre,
            Email = usuario.Email,
            Rol = usuario.Rol,
            Activo = usuario.Activo,
            FechaCreacion = usuario.FechaCreacion
        };
    }

    private static Dictionary<string, string[]> ValidateCreateRequest(CreateUsuarioRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        // Validar nombre
        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            errors["Nombre"] = new[] { "El nombre es requerido y no puede ser solo espacios en blanco." };
        }
        else if (request.Nombre.Trim().Length > 100)
        {
            errors["Nombre"] = new[] { "El nombre no puede exceder 100 caracteres." };
        }

        // Validar email
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors["Email"] = new[] { "El correo electrónico es requerido." };
        }
        else if (request.Email.Trim().Length > 254)
        {
            errors["Email"] = new[] { "El correo electrónico no puede exceder 254 caracteres." };
        }
        else if (!EmailRegex.IsMatch(request.Email.Trim()))
        {
            errors["Email"] = new[] { "El formato del correo electrónico no es válido." };
        }

        // Validar contraseña
        if (string.IsNullOrEmpty(request.Password))
        {
            errors["Password"] = new[] { "La contraseña es requerida." };
        }
        else if (request.Password.Length < 8)
        {
            errors["Password"] = new[] { "La contraseña debe tener al menos 8 caracteres." };
        }
        else if (request.Password.Length > 50)
        {
            errors["Password"] = new[] { "La contraseña no puede exceder 50 caracteres." };
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateUpdateRequest(UpdateUsuarioRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        // Validar nombre
        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            errors["Nombre"] = new[] { "El nombre es requerido y no puede ser solo espacios en blanco." };
        }
        else if (request.Nombre.Trim().Length > 100)
        {
            errors["Nombre"] = new[] { "El nombre no puede exceder 100 caracteres." };
        }

        // Validar email
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors["Email"] = new[] { "El correo electrónico es requerido." };
        }
        else if (request.Email.Trim().Length > 254)
        {
            errors["Email"] = new[] { "El correo electrónico no puede exceder 254 caracteres." };
        }
        else if (!EmailRegex.IsMatch(request.Email.Trim()))
        {
            errors["Email"] = new[] { "El formato del correo electrónico no es válido." };
        }

        return errors;
    }

    #endregion
}
