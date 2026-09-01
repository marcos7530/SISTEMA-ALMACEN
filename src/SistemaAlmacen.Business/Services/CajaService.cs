using SistemaAlmacen.Business.Interfaces;
using SistemaAlmacen.Data.Entities;
using SistemaAlmacen.Data.Repositories;
using SistemaAlmacen.Shared.Common;
using SistemaAlmacen.Shared.DTOs;
using SistemaAlmacen.Shared.DTOs.Caja;
using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Business.Services;

/// <summary>
/// Implementación del servicio de gestión de caja registradora.
/// Maneja apertura, movimientos (retiros/ingresos), registro automático de ventas en efectivo y cierre.
/// </summary>
public class CajaService : ICajaService
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Umbral configurable para marcar diferencias significativas en el cierre de caja.
    /// </summary>
    private const decimal UmbralDiferenciaSignificativa = 500m;

    public CajaService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result<CajaDto>> AbrirCajaAsync(AbrirCajaRequest request, int usuarioId)
    {
        // Validar monto inicial
        if (request.MontoInicial < 0)
            return Result<CajaDto>.Failure("El monto inicial no puede ser negativo.", "MONTO_INVALIDO");

        // Verificar que no exista caja abierta para el punto de venta
        var cajaExistente = await _unitOfWork.Cajas.GetCajaAbiertaAsync(request.PuntoDeVentaId);
        if (cajaExistente is not null)
            return Result<CajaDto>.Failure(
                "Ya existe una caja abierta para este punto de venta. Debe cerrar la caja actual antes de abrir una nueva.",
                "CAJA_YA_ABIERTA");

        var ahora = DateTime.UtcNow;

        var caja = new Caja
        {
            PuntoDeVentaId = request.PuntoDeVentaId,
            UsuarioAperturaId = usuarioId,
            MontoInicial = request.MontoInicial,
            Estado = EstadoCaja.Abierta,
            FechaApertura = ahora
        };

        await _unitOfWork.Cajas.AddAsync(caja);
        await _unitOfWork.SaveChangesAsync();

        // Registrar movimiento de apertura
        var movimientoApertura = new CajaMovimiento
        {
            CajaId = caja.Id,
            UsuarioId = usuarioId,
            Tipo = TipoMovimiento.Apertura,
            Monto = request.MontoInicial,
            Motivo = "Apertura de caja",
            Fecha = ahora
        };

        await _unitOfWork.Cajas.AddMovimientoAsync(caja.Id, movimientoApertura);
        await _unitOfWork.SaveChangesAsync();

        // Cargar usuario para el DTO
        var usuario = await _unitOfWork.Usuarios.GetByIdAsync(usuarioId);

        return Result<CajaDto>.Success(new CajaDto
        {
            Id = caja.Id,
            PuntoDeVentaId = caja.PuntoDeVentaId,
            MontoInicial = caja.MontoInicial,
            Estado = caja.Estado,
            FechaApertura = caja.FechaApertura,
            UsuarioApertura = usuario?.Nombre ?? string.Empty
        });
    }

    /// <inheritdoc />
    public async Task<Result<CierreResumenDto>> CerrarCajaAsync(CerrarCajaRequest request, int usuarioId)
    {
        // Validar monto real de cierre
        if (request.MontoRealCierre < 0)
            return Result<CierreResumenDto>.Failure("El monto real de cierre no puede ser negativo.", "MONTO_INVALIDO");

        // Buscar caja abierta - usamos PuntoDeVentaId=1 por defecto; en una implementación más
        // avanzada se podría pasar como parámetro. Por ahora la búsqueda se hace sobre la caja
        // abierta del usuario o se agrega PuntoDeVentaId al request.
        // Buscar la caja abierta que tenga el usuario como apertura, o cualquier caja abierta
        var cajasAbiertas = await GetCajaAbiertaParaUsuarioAsync(usuarioId);
        if (cajasAbiertas is null)
            return Result<CierreResumenDto>.Failure(
                "No se encontró una caja abierta para cerrar.", "NO_HAY_CAJA_ABIERTA");

        var caja = cajasAbiertas;

        // Calcular saldo esperado: MontoInicial + Ingresos - Retiros
        var movimientos = caja.Movimientos.ToList();

        var totalIngresos = movimientos
            .Where(m => m.Tipo == TipoMovimiento.VentaEfectivo || m.Tipo == TipoMovimiento.IngresoAdicional)
            .Sum(m => m.Monto);

        var totalRetiros = movimientos
            .Where(m => m.Tipo == TipoMovimiento.Retiro)
            .Sum(m => m.Monto);

        var saldoEsperado = caja.MontoInicial + totalIngresos - totalRetiros;
        var diferencia = request.MontoRealCierre - saldoEsperado;

        // Actualizar caja
        caja.Estado = EstadoCaja.Cerrada;
        caja.UsuarioCierreId = usuarioId;
        caja.FechaCierre = DateTime.UtcNow;
        caja.SaldoEsperado = saldoEsperado;
        caja.MontoRealCierre = request.MontoRealCierre;
        caja.Diferencia = diferencia;

        _unitOfWork.Cajas.Update(caja);
        await _unitOfWork.SaveChangesAsync();

        // Determinar si la diferencia es significativa
        var diferenciaSignificativa = Math.Abs(diferencia) > UmbralDiferenciaSignificativa;

        // Generar resumen
        var resumen = new CierreResumenDto
        {
            Id = caja.Id,
            FechaApertura = caja.FechaApertura,
            FechaCierre = caja.FechaCierre!.Value,
            MontoInicial = caja.MontoInicial,
            SaldoEsperado = saldoEsperado,
            MontoRealCierre = request.MontoRealCierre,
            Diferencia = diferencia,
            DiferenciaSignificativa = diferenciaSignificativa,
            Movimientos = movimientos.Select(MapMovimientoToDto).ToList()
        };

        return Result<CierreResumenDto>.Success(resumen);
    }

    /// <inheritdoc />
    public async Task<Result<MovimientoDto>> RegistrarRetiroAsync(MovimientoRequest request, int usuarioId)
    {
        return await RegistrarMovimientoAsync(request, usuarioId, TipoMovimiento.Retiro);
    }

    /// <inheritdoc />
    public async Task<Result<MovimientoDto>> RegistrarIngresoAsync(MovimientoRequest request, int usuarioId)
    {
        return await RegistrarMovimientoAsync(request, usuarioId, TipoMovimiento.IngresoAdicional);
    }

    /// <inheritdoc />
    public async Task<CajaDto?> GetCajaAbiertaAsync(int puntoDeVentaId)
    {
        var caja = await _unitOfWork.Cajas.GetCajaAbiertaAsync(puntoDeVentaId);
        if (caja is null)
            return null;

        var usuario = await _unitOfWork.Usuarios.GetByIdAsync(caja.UsuarioAperturaId);

        return new CajaDto
        {
            Id = caja.Id,
            PuntoDeVentaId = caja.PuntoDeVentaId,
            MontoInicial = caja.MontoInicial,
            Estado = caja.Estado,
            FechaApertura = caja.FechaApertura,
            UsuarioApertura = usuario?.Nombre ?? string.Empty
        };
    }

    /// <inheritdoc />
    public async Task<PaginatedResult<CierreResumenDto>> GetHistorialCierresAsync(CierreFilter filter)
    {
        var result = await _unitOfWork.Cajas.GetHistorialCierresAsync(filter);

        return new PaginatedResult<CierreResumenDto>
        {
            Items = result.Items.Select(caja => new CierreResumenDto
            {
                Id = caja.Id,
                FechaApertura = caja.FechaApertura,
                FechaCierre = caja.FechaCierre ?? DateTime.MinValue,
                MontoInicial = caja.MontoInicial,
                SaldoEsperado = caja.SaldoEsperado ?? 0,
                MontoRealCierre = caja.MontoRealCierre ?? 0,
                Diferencia = caja.Diferencia ?? 0,
                DiferenciaSignificativa = Math.Abs(caja.Diferencia ?? 0) > UmbralDiferenciaSignificativa,
                Movimientos = caja.Movimientos.Select(MapMovimientoToDto).ToList()
            }).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
    }

    /// <inheritdoc />
    public async Task<Result<MovimientoDto>> RegistrarVentaEfectivoAsync(decimal monto, int usuarioId, int puntoDeVentaId)
    {
        if (monto <= 0)
            return Result<MovimientoDto>.Failure("El monto de la venta en efectivo debe ser mayor a cero.", "MONTO_INVALIDO");

        var caja = await _unitOfWork.Cajas.GetCajaAbiertaAsync(puntoDeVentaId);
        if (caja is null)
            return Result<MovimientoDto>.Failure(
                "No hay caja abierta. Debe abrir la caja antes de registrar ventas en efectivo.",
                "NO_HAY_CAJA_ABIERTA");

        var movimiento = new CajaMovimiento
        {
            CajaId = caja.Id,
            UsuarioId = usuarioId,
            Tipo = TipoMovimiento.VentaEfectivo,
            Monto = monto,
            Motivo = "Venta en efectivo",
            Fecha = DateTime.UtcNow
        };

        await _unitOfWork.Cajas.AddMovimientoAsync(caja.Id, movimiento);
        await _unitOfWork.SaveChangesAsync();

        var usuario = await _unitOfWork.Usuarios.GetByIdAsync(usuarioId);

        return Result<MovimientoDto>.Success(new MovimientoDto
        {
            Id = movimiento.Id,
            Tipo = movimiento.Tipo,
            Monto = movimiento.Monto,
            Motivo = movimiento.Motivo ?? string.Empty,
            Fecha = movimiento.Fecha,
            Usuario = usuario?.Nombre ?? string.Empty
        });
    }

    /// <inheritdoc />
    public async Task<Result> RevertirVentaEfectivoAsync(decimal monto, int usuarioId, int ventaId, int puntoDeVentaId)
    {
        if (monto <= 0)
            return Result.Success();

        var caja = await _unitOfWork.Cajas.GetCajaAbiertaAsync(puntoDeVentaId);
        if (caja is null)
            return Result.Failure(
                "No hay una caja abierta para revertir el ingreso en efectivo de la venta.",
                "NO_HAY_CAJA_ABIERTA");

        var movimiento = new CajaMovimiento
        {
            CajaId = caja.Id,
            UsuarioId = usuarioId,
            Tipo = TipoMovimiento.Retiro,
            Monto = monto,
            Motivo = $"Reverso por anulación de venta #{ventaId}",
            Fecha = DateTime.UtcNow
        };

        await _unitOfWork.Cajas.AddMovimientoAsync(caja.Id, movimiento);
        // No se llama SaveChanges: participa de la transacción de anulación.

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<bool> HayCajaAbiertaAsync(int puntoDeVentaId)
    {
        var caja = await _unitOfWork.Cajas.GetCajaAbiertaAsync(puntoDeVentaId);
        return caja is not null;
    }

    /// <summary>
    /// Registra un movimiento genérico (retiro o ingreso adicional) en la caja abierta.
    /// </summary>
    private async Task<Result<MovimientoDto>> RegistrarMovimientoAsync(
        MovimientoRequest request, int usuarioId, TipoMovimiento tipo)
    {
        // Validar monto
        if (request.Monto <= 0)
            return Result<MovimientoDto>.Failure("El monto debe ser mayor a cero.", "MONTO_INVALIDO");

        // Validar motivo
        if (string.IsNullOrWhiteSpace(request.Motivo))
            return Result<MovimientoDto>.Failure("El motivo es requerido.", "MOTIVO_REQUERIDO");

        if (request.Motivo.Trim().Length > 200)
            return Result<MovimientoDto>.Failure("El motivo no puede exceder 200 caracteres.", "MOTIVO_LARGO");

        // Buscar caja abierta del usuario
        var caja = await GetCajaAbiertaParaUsuarioAsync(usuarioId);
        if (caja is null)
            return Result<MovimientoDto>.Failure(
                "No hay caja abierta. Debe abrir la caja antes de registrar movimientos.",
                "NO_HAY_CAJA_ABIERTA");

        var movimiento = new CajaMovimiento
        {
            CajaId = caja.Id,
            UsuarioId = usuarioId,
            Tipo = tipo,
            Monto = request.Monto,
            Motivo = request.Motivo.Trim(),
            Fecha = DateTime.UtcNow
        };

        await _unitOfWork.Cajas.AddMovimientoAsync(caja.Id, movimiento);
        await _unitOfWork.SaveChangesAsync();

        var usuario = await _unitOfWork.Usuarios.GetByIdAsync(usuarioId);

        return Result<MovimientoDto>.Success(new MovimientoDto
        {
            Id = movimiento.Id,
            Tipo = movimiento.Tipo,
            Monto = movimiento.Monto,
            Motivo = movimiento.Motivo,
            Fecha = movimiento.Fecha,
            Usuario = usuario?.Nombre ?? string.Empty
        });
    }

    /// <summary>
    /// Busca una caja abierta asociada al punto de venta del usuario.
    /// Busca la primera caja abierta en el sistema (simplificación para punto de venta único).
    /// </summary>
    private async Task<Caja?> GetCajaAbiertaParaUsuarioAsync(int usuarioId)
    {
        // Buscar la caja abierta - para el flujo actual asumimos punto de venta 1
        // En un escenario multi-punto de venta, esto se resolvería con el contexto del usuario
        var caja = await _unitOfWork.Cajas.GetCajaAbiertaAsync(1);
        return caja;
    }

    /// <summary>
    /// Mapea una entidad CajaMovimiento a MovimientoDto.
    /// </summary>
    private static MovimientoDto MapMovimientoToDto(CajaMovimiento movimiento)
    {
        return new MovimientoDto
        {
            Id = movimiento.Id,
            Tipo = movimiento.Tipo,
            Monto = movimiento.Monto,
            Motivo = movimiento.Motivo ?? string.Empty,
            Fecha = movimiento.Fecha,
            Usuario = movimiento.Usuario?.Nombre ?? string.Empty
        };
    }
}
