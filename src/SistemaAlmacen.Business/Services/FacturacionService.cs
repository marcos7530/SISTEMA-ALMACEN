using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QuestPDF.Infrastructure;
using SistemaAlmacen.Business.Interfaces;
using SistemaAlmacen.Business.Models.Afip;
using SistemaAlmacen.Data.Entities;
using SistemaAlmacen.Data.Repositories;
using SistemaAlmacen.Shared.Common;
using SistemaAlmacen.Shared.DTOs.Facturacion;
using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Business.Services;

/// <summary>
/// Implementación del servicio de facturación electrónica AFIP/ARCA.
/// Coordina la emisión de comprobantes, almacena CAE y gestiona pendientes.
/// </summary>
public class FacturacionService : IFacturacionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAfipClientWrapper _afipClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FacturacionService> _logger;
    private readonly ComprobantePdfGenerator _pdfGenerator;

    /// <summary>
    /// Punto de venta AFIP configurado. Default 1 para desarrollo.
    /// </summary>
    private int PuntoDeVentaAfip => _configuration.GetValue<int>("Afip:PuntoDeVenta", 1);

    static FacturacionService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public FacturacionService(
        IUnitOfWork unitOfWork,
        IAfipClientWrapper afipClient,
        IConfiguration configuration,
        ILogger<FacturacionService> logger)
    {
        _unitOfWork = unitOfWork;
        _afipClient = afipClient;
        _configuration = configuration;
        _logger = logger;
        _pdfGenerator = new ComprobantePdfGenerator();
    }

    /// <inheritdoc />
    public async Task<Result<ComprobanteDto>> EmitirComprobanteAsync(int ventaId)
    {
        var venta = await _unitOfWork.Ventas.GetVentaConDetallesAsync(ventaId);
        if (venta is null)
            return Result<ComprobanteDto>.Failure("La venta no existe.", "VENTA_NO_ENCONTRADA");

        if (venta.Estado == EstadoVenta.Borrador)
            return Result<ComprobanteDto>.Failure("La venta debe estar confirmada para emitir comprobante.", "VENTA_NO_CONFIRMADA");

        // Verificar si ya tiene comprobante emitido
        var comprobanteExistente = await _unitOfWork.Comprobantes.GetByVentaIdAsync(ventaId);
        if (comprobanteExistente is not null && comprobanteExistente.Estado == EstadoComprobante.Emitido)
            return Result<ComprobanteDto>.Failure("La venta ya tiene un comprobante emitido.", "COMPROBANTE_EXISTENTE");

        // Determinar tipo de comprobante (default Factura B para consumidor final)
        var tipoComprobante = DeterminarTipoComprobante(venta);

        // Calcular importes para AFIP
        var (netoGravado, iva, exento) = CalcularImportes(venta.Total);

        var request = new AfipVoucherRequest
        {
            PuntoDeVenta = PuntoDeVentaAfip,
            TipoComprobante = tipoComprobante,
            Total = venta.Total,
            NetoGravado = netoGravado,
            Iva = iva,
            Exento = exento,
            Moneda = "PES",
            FechaComprobante = venta.Fecha
        };

        try
        {
            var response = await _afipClient.CreateNextVoucherAsync(request);

            if (response.HasCae)
            {
                var comprobante = await RegistrarComprobanteExitosoAsync(venta, response, tipoComprobante, comprobanteExistente);
                return Result<ComprobanteDto>.Success(MapToDto(comprobante));
            }
            else
            {
                await RegistrarComprobanteRechazadoAsync(venta, response, tipoComprobante, comprobanteExistente);
                return Result<ComprobanteDto>.Failure(
                    $"AFIP rechazó el comprobante: {response.ErrorMessage}",
                    "AFIP_RECHAZO");
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error de conexión con AFIP al emitir comprobante para venta {VentaId}", ventaId);
            await MarcarPendienteFacturacionAsync(venta, ex.Message, tipoComprobante, comprobanteExistente);
            return Result<ComprobanteDto>.Failure(
                "No se pudo conectar con AFIP. La venta queda pendiente de facturación.",
                "AFIP_CONEXION_ERROR");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al emitir comprobante para venta {VentaId}", ventaId);
            await MarcarPendienteFacturacionAsync(venta, ex.Message, tipoComprobante, comprobanteExistente);
            return Result<ComprobanteDto>.Failure(
                "Error inesperado al emitir comprobante. La venta queda pendiente de facturación.",
                "AFIP_ERROR_INESPERADO");
        }
    }

    /// <inheritdoc />
    public async Task<Result<ComprobanteDto>> ReintentarEmisionAsync(int ventaId)
    {
        var venta = await _unitOfWork.Ventas.GetVentaConDetallesAsync(ventaId);
        if (venta is null)
            return Result<ComprobanteDto>.Failure("La venta no existe.", "VENTA_NO_ENCONTRADA");

        if (venta.Estado != EstadoVenta.PendienteFacturacion)
            return Result<ComprobanteDto>.Failure(
                "Solo se puede reintentar la emisión de ventas pendientes de facturación.",
                "VENTA_NO_PENDIENTE");

        return await EmitirComprobanteAsync(ventaId);
    }

    /// <inheritdoc />
    public async Task<List<VentaPendienteFacturacionDto>> GetPendientesAsync()
    {
        var comprobantes = await _unitOfWork.Comprobantes.GetPendientesAsync();

        return comprobantes.Select(c => new VentaPendienteFacturacionDto
        {
            VentaId = c.VentaId,
            FechaVenta = c.Venta.Fecha,
            Total = c.Venta.Total,
            Error = c.ErrorDetalle ?? string.Empty
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<Result> EmitirPendientesMasivamenteAsync()
    {
        var pendientes = await _unitOfWork.Comprobantes.GetPendientesAsync();

        if (pendientes.Count == 0)
            return Result.Success();

        var errores = new List<string>();

        foreach (var comprobante in pendientes)
        {
            var resultado = await EmitirComprobanteAsync(comprobante.VentaId);
            if (!resultado.IsSuccess)
            {
                errores.Add($"Venta {comprobante.VentaId}: {resultado.ErrorMessage}");
            }
        }

        if (errores.Count > 0)
        {
            return Result.Failure(
                $"Se procesaron {pendientes.Count} pendientes. {errores.Count} con errores: {string.Join("; ", errores.Take(5))}",
                "EMISION_MASIVA_PARCIAL");
        }

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<ComprobanteDto?> ConsultarComprobanteAsync(int ventaId)
    {
        var comprobante = await _unitOfWork.Comprobantes.GetByVentaIdAsync(ventaId);
        if (comprobante is null)
            return null;

        return MapToDto(comprobante);
    }

    /// <inheritdoc />
    public async Task<byte[]> GenerarPdfComprobanteAsync(int ventaId)
    {
        var venta = await _unitOfWork.Ventas.GetVentaConDetallesAsync(ventaId);
        if (venta is null)
            return Array.Empty<byte>();

        var comprobante = await _unitOfWork.Comprobantes.GetByVentaIdAsync(ventaId);
        if (comprobante is null || comprobante.Estado != EstadoComprobante.Emitido)
            return Array.Empty<byte>();

        var vendedor = venta.Usuario?.Nombre ?? "Sin asignar";

        return _pdfGenerator.Generar(comprobante, venta, vendedor);
    }

    /// <summary>
    /// Determina el tipo de comprobante según la condición IVA del receptor.
    /// Por defecto retorna Factura B (consumidor final).
    /// </summary>
    private int DeterminarTipoComprobante(Venta venta)
    {
        // Por simplicidad, se usa Factura B (tipo 6) como default para consumidor final.
        // En una implementación completa, se determinaría por la condición IVA del cliente.
        // Factura A (1): Responsable Inscripto → Responsable Inscripto
        // Factura B (6): Responsable Inscripto → Consumidor Final / Monotributista
        // Factura C (11): Monotributista → Cualquier receptor
        return (int)TipoComprobante.FacturaB;
    }

    /// <summary>
    /// Calcula los importes desagregados para AFIP.
    /// Asume IVA 21% incluido en el total para Factura B.
    /// </summary>
    private (decimal netoGravado, decimal iva, decimal exento) CalcularImportes(decimal total)
    {
        // Para Factura B: el IVA está incluido en el precio, no se discrimina
        // Neto gravado = Total / 1.21, IVA = Total - NetoGravado
        var netoGravado = Math.Round(total / 1.21m, 2);
        var iva = total - netoGravado;
        var exento = 0m;

        return (netoGravado, iva, exento);
    }

    /// <summary>
    /// Registra un comprobante exitoso con CAE en la base de datos.
    /// </summary>
    private async Task<Comprobante> RegistrarComprobanteExitosoAsync(
        Venta venta,
        AfipVoucherResponse response,
        int tipoComprobante,
        Comprobante? comprobanteExistente)
    {
        var comprobante = comprobanteExistente ?? new Comprobante
        {
            VentaId = venta.Id,
            FechaEmision = DateTime.UtcNow
        };

        comprobante.TipoComprobante = tipoComprobante;
        comprobante.NumeroComprobante = response.NumeroComprobante!.Value;
        comprobante.CAE = response.Cae;
        comprobante.FechaVencimientoCAE = response.CaeVencimiento;
        comprobante.Estado = EstadoComprobante.Emitido;
        comprobante.ErrorDetalle = null;

        if (comprobanteExistente is null)
        {
            await _unitOfWork.Comprobantes.AddAsync(comprobante);
        }

        // Actualizar estado de la venta a Confirmada (ya no es pendiente)
        if (venta.Estado == EstadoVenta.PendienteFacturacion)
        {
            venta.Estado = EstadoVenta.Confirmada;
        }

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Comprobante emitido exitosamente: Venta {VentaId}, CAE {Cae}, Nro {Numero}",
            venta.Id, response.Cae, response.NumeroComprobante);

        return comprobante;
    }

    /// <summary>
    /// Registra un comprobante rechazado por AFIP y marca la venta como pendiente.
    /// </summary>
    private async Task RegistrarComprobanteRechazadoAsync(
        Venta venta,
        AfipVoucherResponse response,
        int tipoComprobante,
        Comprobante? comprobanteExistente)
    {
        var comprobante = comprobanteExistente ?? new Comprobante
        {
            VentaId = venta.Id,
            FechaEmision = DateTime.UtcNow
        };

        comprobante.TipoComprobante = tipoComprobante;
        comprobante.NumeroComprobante = 0;
        comprobante.CAE = null;
        comprobante.FechaVencimientoCAE = null;
        comprobante.Estado = EstadoComprobante.Rechazado;
        comprobante.ErrorDetalle = response.ErrorMessage ?? string.Join("; ", response.Errors ?? new List<string>());

        if (comprobanteExistente is null)
        {
            await _unitOfWork.Comprobantes.AddAsync(comprobante);
        }

        // Marcar venta como pendiente de facturación
        venta.Estado = EstadoVenta.PendienteFacturacion;

        await _unitOfWork.SaveChangesAsync();

        _logger.LogWarning(
            "Comprobante rechazado por AFIP: Venta {VentaId}, Error: {Error}",
            venta.Id, comprobante.ErrorDetalle);
    }

    /// <summary>
    /// Marca la venta como pendiente de facturación por error de conexión.
    /// </summary>
    private async Task MarcarPendienteFacturacionAsync(
        Venta venta,
        string errorMessage,
        int tipoComprobante,
        Comprobante? comprobanteExistente)
    {
        var comprobante = comprobanteExistente ?? new Comprobante
        {
            VentaId = venta.Id,
            FechaEmision = DateTime.UtcNow
        };

        comprobante.TipoComprobante = tipoComprobante;
        comprobante.NumeroComprobante = 0;
        comprobante.CAE = null;
        comprobante.FechaVencimientoCAE = null;
        comprobante.Estado = EstadoComprobante.Pendiente;
        comprobante.ErrorDetalle = errorMessage.Length > 500
            ? errorMessage[..500]
            : errorMessage;

        if (comprobanteExistente is null)
        {
            await _unitOfWork.Comprobantes.AddAsync(comprobante);
        }

        // Marcar venta como pendiente de facturación
        venta.Estado = EstadoVenta.PendienteFacturacion;

        await _unitOfWork.SaveChangesAsync();

        _logger.LogWarning(
            "Venta {VentaId} marcada como pendiente de facturación: {Error}",
            venta.Id, errorMessage);
    }

    /// <summary>
    /// Mapea una entidad Comprobante a su DTO.
    /// </summary>
    private static ComprobanteDto MapToDto(Comprobante comprobante)
    {
        return new ComprobanteDto
        {
            Id = comprobante.Id,
            VentaId = comprobante.VentaId,
            TipoComprobante = comprobante.TipoComprobante,
            NumeroComprobante = comprobante.NumeroComprobante,
            CAE = comprobante.CAE ?? string.Empty,
            FechaVencimientoCAE = comprobante.FechaVencimientoCAE ?? DateTime.MinValue,
            Estado = comprobante.Estado,
            FechaEmision = comprobante.FechaEmision
        };
    }
}
