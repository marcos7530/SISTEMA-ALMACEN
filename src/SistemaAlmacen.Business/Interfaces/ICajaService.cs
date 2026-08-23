using SistemaAlmacen.Shared.Common;
using SistemaAlmacen.Shared.DTOs;
using SistemaAlmacen.Shared.DTOs.Caja;

namespace SistemaAlmacen.Business.Interfaces;

/// <summary>
/// Servicio para gestión de apertura, movimientos y cierre de caja registradora.
/// </summary>
public interface ICajaService
{
    /// <summary>
    /// Abre una nueva caja validando que no exista una abierta para el punto de venta.
    /// </summary>
    Task<Result<CajaDto>> AbrirCajaAsync(AbrirCajaRequest request, int usuarioId);

    /// <summary>
    /// Cierra la caja abierta calculando saldo esperado, diferencia y generando resumen.
    /// </summary>
    Task<Result<CierreResumenDto>> CerrarCajaAsync(CerrarCajaRequest request, int usuarioId);

    /// <summary>
    /// Registra un retiro de efectivo de la caja (solo Administrador).
    /// </summary>
    Task<Result<MovimientoDto>> RegistrarRetiroAsync(MovimientoRequest request, int usuarioId);

    /// <summary>
    /// Registra un ingreso adicional de efectivo a la caja (solo Administrador).
    /// </summary>
    Task<Result<MovimientoDto>> RegistrarIngresoAsync(MovimientoRequest request, int usuarioId);

    /// <summary>
    /// Obtiene la caja actualmente abierta para un punto de venta, o null si no hay ninguna.
    /// </summary>
    Task<CajaDto?> GetCajaAbiertaAsync(int puntoDeVentaId);

    /// <summary>
    /// Obtiene el historial paginado de cierres de caja con filtros opcionales.
    /// </summary>
    Task<PaginatedResult<CierreResumenDto>> GetHistorialCierresAsync(CierreFilter filter);

    /// <summary>
    /// Registra automáticamente una venta en efectivo como ingreso de caja.
    /// </summary>
    Task<Result<MovimientoDto>> RegistrarVentaEfectivoAsync(decimal monto, int usuarioId, int puntoDeVentaId);

    /// <summary>
    /// Verifica si existe una caja abierta para el punto de venta.
    /// </summary>
    Task<bool> HayCajaAbiertaAsync(int puntoDeVentaId);
}
