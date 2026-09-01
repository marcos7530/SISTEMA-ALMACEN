using SistemaAlmacen.Shared.Common;
using SistemaAlmacen.Shared.DTOs;
using SistemaAlmacen.Shared.DTOs.Ventas;

namespace SistemaAlmacen.Business.Interfaces;

/// <summary>
/// Servicio de lógica de negocio para la gestión de ventas.
/// </summary>
public interface IVentaService
{
    /// <summary>
    /// Inicializa una nueva venta en estado Borrador con la fecha actual y el vendedor asociado.
    /// </summary>
    /// <param name="vendedorId">ID del usuario vendedor que inicia la venta.</param>
    /// <returns>DTO de la venta creada.</returns>
    Task<VentaDto> InitializeVentaAsync(int vendedorId);

    /// <summary>
    /// Agrega un detalle (producto con cantidad) a una venta en estado Borrador.
    /// Valida cantidad, existencia del producto, stock disponible y recalcula el total.
    /// </summary>
    /// <param name="ventaId">ID de la venta.</param>
    /// <param name="request">Datos del detalle a agregar.</param>
    /// <returns>Resultado con el detalle agregado o error de validación.</returns>
    Task<Result<DetalleVentaDto>> AddDetalleAsync(int ventaId, AddDetalleRequest request);

    /// <summary>
    /// Elimina un detalle de una venta en estado Borrador y recalcula el total.
    /// </summary>
    /// <param name="ventaId">ID de la venta.</param>
    /// <param name="detalleId">ID del detalle a eliminar.</param>
    /// <returns>Resultado exitoso o error si no se encuentra la venta o el detalle.</returns>
    Task<Result> RemoveDetalleAsync(int ventaId, int detalleId);

    /// <summary>
    /// Confirma una venta: verifica stock, descuenta stock atómicamente,
    /// y cambia el estado a Confirmada. Realiza rollback ante errores.
    /// </summary>
    /// <param name="ventaId">ID de la venta a confirmar.</param>
    /// <param name="request">Datos de confirmación (pagos).</param>
    /// <returns>Resultado con la venta confirmada o error.</returns>
    Task<Result<VentaDto>> ConfirmVentaAsync(int ventaId, ConfirmVentaRequest request);

    /// <summary>
    /// Obtiene el historial de ventas paginado y filtrado.
    /// </summary>
    /// <param name="filter">Filtros de búsqueda y paginación.</param>
    /// <returns>Resultado paginado con resúmenes de ventas.</returns>
    Task<PaginatedResult<VentaResumenDto>> GetHistorialAsync(VentaFilter filter);

    /// <summary>
    /// Obtiene el detalle completo de una venta incluyendo detalles y pagos.
    /// </summary>
    /// <param name="ventaId">ID de la venta.</param>
    /// <returns>DTO con detalle completo o null si no existe.</returns>
    Task<VentaDetalleCompletoDto?> GetDetalleCompletoAsync(int ventaId);

    /// <summary>
    /// Anula una venta confirmada: repone el stock, revierte el ingreso de caja en efectivo
    /// (si corresponde), revierte el cargo en cuenta corriente (si corresponde), cambia el
    /// estado a Anulada y registra la operación en auditoría. Todo de forma transaccional.
    /// </summary>
    /// <param name="ventaId">ID de la venta a anular.</param>
    /// <param name="request">Motivo de la anulación.</param>
    /// <param name="usuarioId">Usuario que realiza la anulación.</param>
    /// <returns>Resultado con la venta anulada o error.</returns>
    Task<Result<VentaDto>> AnularVentaAsync(int ventaId, AnularVentaRequest request, int usuarioId);
}
