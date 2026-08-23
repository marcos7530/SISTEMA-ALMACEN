using SistemaAlmacen.Shared.DTOs;
using SistemaAlmacen.Shared.DTOs.Auditoria;
using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Business.Interfaces;

/// <summary>
/// Servicio de auditoría para registrar y consultar el historial de operaciones del sistema.
/// Los registros de auditoría son inmutables: no se exponen operaciones de modificación ni eliminación.
/// </summary>
public interface IAuditoriaService
{
    /// <summary>
    /// Registra una operación en el historial de auditoría.
    /// </summary>
    /// <param name="usuarioId">Identificador del usuario que realizó la operación.</param>
    /// <param name="tipo">Tipo de operación realizada.</param>
    /// <param name="entidad">Nombre de la entidad afectada (ej: "Producto", "Usuario").</param>
    /// <param name="registroId">Identificador del registro afectado.</param>
    /// <param name="descripcion">Descripción resumida del cambio realizado.</param>
    Task RegistrarOperacionAsync(int usuarioId, TipoOperacion tipo, string entidad, string registroId, string descripcion);

    /// <summary>
    /// Obtiene el historial de auditoría paginado y filtrado.
    /// Solo accesible por Administradores.
    /// </summary>
    /// <param name="filter">Filtros de búsqueda: usuario, fechas, tipo operación, entidad.</param>
    /// <returns>Resultado paginado con registros de auditoría.</returns>
    Task<PaginatedResult<AuditoriaDto>> GetHistorialAsync(AuditoriaFilter filter);
}
