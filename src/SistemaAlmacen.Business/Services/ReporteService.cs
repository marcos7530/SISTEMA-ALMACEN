using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SistemaAlmacen.Business.Interfaces;
using SistemaAlmacen.Data.Context;
using SistemaAlmacen.Shared.DTOs.Reportes;
using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Business.Services;

/// <summary>
/// Implementación del servicio de reportes.
/// Utiliza ApplicationDbContext directamente para consultas complejas con agrupación y agregación.
/// </summary>
public class ReporteService : IReporteService
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Rango máximo permitido para consultas de reportes de ventas (365 días).
    /// </summary>
    private const int MaxRangoDias = 365;

    public ReporteService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<ReporteVentasDto> GenerarReporteVentasAsync(DateTime fechaInicio, DateTime fechaFin)
    {
        ValidarRangoFechas(fechaInicio, fechaFin);

        // Normalizar fechas para incluir el día completo
        var inicio = fechaInicio.Date;
        var fin = fechaFin.Date.AddDays(1).AddTicks(-1);

        // Consultar ventas confirmadas (incluye PendienteFacturacion como confirmadas a nivel comercial)
        var ventasQuery = _context.Ventas
            .AsNoTracking()
            .Where(v => v.Fecha >= inicio && v.Fecha <= fin &&
                        (v.Estado == EstadoVenta.Confirmada || v.Estado == EstadoVenta.PendienteFacturacion));

        var ventas = await ventasQuery.ToListAsync();

        if (ventas.Count == 0)
        {
            return new ReporteVentasDto
            {
                TotalMonetario = 0,
                CantidadTransacciones = 0,
                DesgloseDiario = new List<DesgloseDiarioDto>(),
                DesgloseMediosPago = new List<DesgloseMedioPagoDto>()
            };
        }

        var totalMonetario = ventas.Sum(v => v.Total);
        var cantidadTransacciones = ventas.Count;

        // Desglose diario
        var desgloseDiario = ventas
            .GroupBy(v => v.Fecha.Date)
            .Select(g => new DesgloseDiarioDto
            {
                Fecha = g.Key,
                Monto = g.Sum(v => v.Total),
                Transacciones = g.Count()
            })
            .OrderBy(d => d.Fecha)
            .ToList();

        // Desglose por medio de pago
        var ventaIds = ventas.Select(v => v.Id).ToList();

        var desgloseMediosPago = await _context.VentaPagos
            .AsNoTracking()
            .Where(vp => ventaIds.Contains(vp.VentaId))
            .Include(vp => vp.MedioPago)
            .GroupBy(vp => vp.MedioPago.Nombre)
            .Select(g => new DesgloseMedioPagoDto
            {
                MedioPago = g.Key,
                Total = g.Sum(vp => vp.Monto),
                Cantidad = g.Count()
            })
            .OrderByDescending(d => d.Total)
            .ToListAsync();

        return new ReporteVentasDto
        {
            TotalMonetario = totalMonetario,
            CantidadTransacciones = cantidadTransacciones,
            DesgloseDiario = desgloseDiario,
            DesgloseMediosPago = desgloseMediosPago
        };
    }

    /// <inheritdoc />
    public async Task<List<ProductoMasVendidoDto>> GenerarReporteProductosAsync(DateTime fechaInicio, DateTime fechaFin)
    {
        // Normalizar fechas
        var inicio = fechaInicio.Date;
        var fin = fechaFin.Date.AddDays(1).AddTicks(-1);

        var productosVendidos = await _context.DetallesVenta
            .AsNoTracking()
            .Where(d => d.Venta.Fecha >= inicio && d.Venta.Fecha <= fin &&
                        (d.Venta.Estado == EstadoVenta.Confirmada || d.Venta.Estado == EstadoVenta.PendienteFacturacion))
            .GroupBy(d => new { d.ProductoId, d.Producto.Nombre, Categoria = d.Producto.Categoria.Nombre })
            .Select(g => new ProductoMasVendidoDto
            {
                Nombre = g.Key.Nombre,
                Categoria = g.Key.Categoria,
                CantidadVendida = g.Sum(d => d.Cantidad)
            })
            .OrderByDescending(p => p.CantidadVendida)
            .Take(50)
            .ToListAsync();

        return productosVendidos;
    }

    /// <inheritdoc />
    public async Task<List<ReporteInventarioDto>> GenerarReporteInventarioAsync()
    {
        var productos = await _context.Productos
            .AsNoTracking()
            .Include(p => p.Categoria)
            .OrderBy(p => p.Nombre)
            .Select(p => new ReporteInventarioDto
            {
                Id = p.Id,
                Nombre = p.Nombre,
                Categoria = p.Categoria.Nombre,
                Stock = p.Stock,
                Precio = p.Precio,
                Activo = p.Activo
            })
            .ToListAsync();

        return productos;
    }

    /// <inheritdoc />
    public async Task<byte[]> ExportarAsync(ExportRequest request)
    {
        // Obtener los datos según el tipo de reporte
        var (headers, rows) = await ObtenerDatosReporteAsync(request.TipoReporte, request.FechaInicio, request.FechaFin);

        // Generar el archivo en el formato solicitado
        return request.Formato switch
        {
            FormatoExportacion.CSV => GenerarCsv(headers, rows),
            FormatoExportacion.Excel => GenerarExcel(headers, rows, request.TipoReporte),
            FormatoExportacion.PDF => GenerarPdf(headers, rows, request.TipoReporte, request.FechaInicio, request.FechaFin),
            _ => throw new ArgumentException($"Formato de exportación no soportado: {request.Formato}")
        };
    }

    /// <summary>
    /// Obtiene los datos tabulares del reporte según el tipo solicitado.
    /// </summary>
    private async Task<(string[] Headers, List<string[]> Rows)> ObtenerDatosReporteAsync(
        string tipoReporte, DateTime fechaInicio, DateTime fechaFin)
    {
        return tipoReporte.ToLowerInvariant() switch
        {
            "ventas" => await ObtenerDatosVentasAsync(fechaInicio, fechaFin),
            "productos" => await ObtenerDatosProductosAsync(fechaInicio, fechaFin),
            "inventario" => await ObtenerDatosInventarioAsync(),
            _ => throw new ArgumentException($"Tipo de reporte no soportado: {tipoReporte}")
        };
    }

    private async Task<(string[] Headers, List<string[]> Rows)> ObtenerDatosVentasAsync(DateTime fechaInicio, DateTime fechaFin)
    {
        var reporte = await GenerarReporteVentasAsync(fechaInicio, fechaFin);

        var headers = new[] { "Fecha", "Monto", "Transacciones" };
        var rows = reporte.DesgloseDiario.Select(d => new[]
        {
            d.Fecha.ToString("yyyy-MM-dd"),
            d.Monto.ToString("F2", CultureInfo.InvariantCulture),
            d.Transacciones.ToString()
        }).ToList();

        return (headers, rows);
    }

    private async Task<(string[] Headers, List<string[]> Rows)> ObtenerDatosProductosAsync(DateTime fechaInicio, DateTime fechaFin)
    {
        var productos = await GenerarReporteProductosAsync(fechaInicio, fechaFin);

        var headers = new[] { "Producto", "Categoría", "Cantidad Vendida" };
        var rows = productos.Select(p => new[]
        {
            p.Nombre,
            p.Categoria,
            p.CantidadVendida.ToString()
        }).ToList();

        return (headers, rows);
    }

    private async Task<(string[] Headers, List<string[]> Rows)> ObtenerDatosInventarioAsync()
    {
        var inventario = await GenerarReporteInventarioAsync();

        var headers = new[] { "Producto", "Categoría", "Stock", "Precio", "Estado" };
        var rows = inventario.Select(p => new[]
        {
            p.Nombre,
            p.Categoria,
            p.Stock.ToString(),
            p.Precio.ToString("F2", CultureInfo.InvariantCulture),
            p.Activo ? "Activo" : "Inactivo"
        }).ToList();

        return (headers, rows);
    }

    /// <summary>
    /// Genera un archivo CSV con BOM UTF-8.
    /// </summary>
    private static byte[] GenerarCsv(string[] headers, List<string[]> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", headers.Select(EscaparCsv)));

        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(",", row.Select(EscaparCsv)));
        }

        // UTF-8 BOM para compatibilidad con Excel
        var bom = Encoding.UTF8.GetPreamble();
        var content = Encoding.UTF8.GetBytes(sb.ToString());
        var result = new byte[bom.Length + content.Length];
        bom.CopyTo(result, 0);
        content.CopyTo(result, bom.Length);
        return result;
    }

    /// <summary>
    /// Escapa un valor para CSV (comillas dobles si contiene comas, comillas o saltos de línea).
    /// </summary>
    private static string EscaparCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    /// <summary>
    /// Genera un archivo Excel (XLSX) usando ClosedXML.
    /// </summary>
    private static byte[] GenerarExcel(string[] headers, List<string[]> rows, string tipoReporte)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(tipoReporte.Length > 31
            ? tipoReporte[..31]
            : tipoReporte);

        // Encabezados
        for (var col = 0; col < headers.Length; col++)
        {
            var cell = worksheet.Cell(1, col + 1);
            cell.Value = headers[col];
            cell.Style.Font.Bold = true;
        }

        // Datos
        for (var row = 0; row < rows.Count; row++)
        {
            for (var col = 0; col < rows[row].Length; col++)
            {
                worksheet.Cell(row + 2, col + 1).Value = rows[row][col];
            }
        }

        // Autoajustar columnas
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Genera un archivo PDF usando QuestPDF con tabla de datos.
    /// </summary>
    private static byte[] GenerarPdf(string[] headers, List<string[]> rows,
        string tipoReporte, DateTime fechaInicio, DateTime fechaFin)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);

                page.Header().Text(text =>
                {
                    text.Span($"Reporte de {tipoReporte} — ").Bold().FontSize(14);
                    text.Span($"{fechaInicio:dd/MM/yyyy} a {fechaFin:dd/MM/yyyy}").FontSize(12);
                });

                page.Content().PaddingVertical(10).Table(table =>
                {
                    // Definir columnas
                    table.ColumnsDefinition(columns =>
                    {
                        for (var i = 0; i < headers.Length; i++)
                        {
                            columns.RelativeColumn();
                        }
                    });

                    // Encabezados
                    table.Header(header =>
                    {
                        foreach (var h in headers)
                        {
                            header.Cell().Background(Colors.Grey.Lighten2)
                                .Padding(5)
                                .Text(h).Bold().FontSize(10);
                        }
                    });

                    // Filas de datos
                    foreach (var row in rows)
                    {
                        foreach (var cell in row)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1)
                                .Padding(4)
                                .Text(cell ?? string.Empty).FontSize(9);
                        }
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Página ").FontSize(8);
                    text.CurrentPageNumber().FontSize(8);
                    text.Span(" de ").FontSize(8);
                    text.TotalPages().FontSize(8);
                });
            });
        });

        using var stream = new MemoryStream();
        document.GeneratePdf(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Valida que el rango de fechas no exceda el máximo permitido (365 días)
    /// y que la fecha de inicio no sea posterior a la fecha de fin.
    /// </summary>
    private static void ValidarRangoFechas(DateTime fechaInicio, DateTime fechaFin)
    {
        if (fechaInicio.Date > fechaFin.Date)
        {
            throw new ArgumentException("La fecha de inicio no puede ser posterior a la fecha de fin.");
        }

        var dias = (fechaFin.Date - fechaInicio.Date).Days;
        if (dias > MaxRangoDias)
        {
            throw new ArgumentException($"El rango de fechas no puede exceder {MaxRangoDias} días.");
        }
    }
}
