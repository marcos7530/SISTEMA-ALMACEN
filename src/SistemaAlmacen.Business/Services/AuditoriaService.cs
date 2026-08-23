using SistemaAlmacen.Business.Interfaces;
using SistemaAlmacen.Data.Entities;
using SistemaAlmacen.Data.Repositories;
using SistemaAlmacen.Shared.DTOs;
using SistemaAlmacen.Shared.DTOs.Auditoria;
using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Business.Services;

/// <summary>
/// Implementación del servicio de auditoría.
/// Garantiza inmutabilidad: solo expone operaciones de registro y consulta.
/// No se proporcionan métodos de actualización ni eliminación de registros de auditoría.
/// </summary>
public class AuditoriaService : IAuditoriaService
{
    private readonly IUnitOfWork _unitOfWork;

    public AuditoriaService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task RegistrarOperacionAsync(int usuarioId, TipoOperacion tipo, string entidad, string registroId, string descripcion)
    {
        var log = new AuditoriaLog
        {
            UsuarioId = usuarioId,
            TipoOperacion = tipo,
            EntidadAfectada = entidad,
            RegistroAfectadoId = registroId,
            Descripcion = descripcion,
            Fecha = DateTime.UtcNow
        };

        await _unitOfWork.Auditoria.AddAsync(log);
        await _unitOfWork.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<PaginatedResult<AuditoriaDto>> GetHistorialAsync(AuditoriaFilter filter)
    {
        var result = await _unitOfWork.Auditoria.GetHistorialAsync(filter);

        return new PaginatedResult<AuditoriaDto>
        {
            Items = result.Items.Select(MapToDto).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
    }

    /// <summary>
    /// Mapea una entidad AuditoriaLog a AuditoriaDto.
    /// </summary>
    private static AuditoriaDto MapToDto(AuditoriaLog log)
    {
        return new AuditoriaDto
        {
            Id = log.Id,
            Usuario = log.Usuario?.Nombre ?? "Sistema",
            TipoOperacion = log.TipoOperacion,
            EntidadAfectada = log.EntidadAfectada,
            RegistroAfectadoId = log.RegistroAfectadoId,
            Descripcion = log.Descripcion,
            Fecha = log.Fecha
        };
    }
}
