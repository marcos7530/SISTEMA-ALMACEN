using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SistemaAlmacen.Business.Interfaces;
using SistemaAlmacen.Data.Context;
using SistemaAlmacen.Shared.Common;
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
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Rango máximo permitido para consultas de reportes de ventas (365 días).
    /// </summary>
    private const int MaxRangoDias = 365;

    public ReporteService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    /// <inheritdoc />
    public async Task<ReporteVentasDto> GenerarReporteVentasAsync(DateTime fechaInicio, DateTime fechaFin)
    {
        ValidarRangoFechas(fechaInicio, fechaFin);

        // Las fechas llegan como día local de Argentina; se convierten al rango UTC
        // equivalente para comparar contra v.Fecha (almacenada en UTC). Ver ArgentinaTimeZone.
        var (inicio, fin) = ArgentinaTimeZone.ToUtcRange(fechaInicio, fechaFin);

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
            .GroupBy(v => ArgentinaTimeZone.ToLocal(v.Fecha).Date)
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
        // Las fechas llegan como día local de Argentina; se convierten al rango UTC
        // equivalente para comparar contra Venta.Fecha (almacenada en UTC). Ver ArgentinaTimeZone.
        var (inicio, fin) = ArgentinaTimeZone.ToUtcRange(fechaInicio, fechaFin);

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
    public async Task<List<ReporteStockBajoDto>> GenerarReporteStockBajoAsync()
    {
        var umbralStockBajo = _configuration.GetValue<int>("Dashboard:StockBajoUmbral", 5);

        // Solo productos activos; se traen a memoria para aplicar la misma lógica
        // de umbral que el dashboard (depende de una variable de configuración).
        var productos = await _context.Productos
            .AsNoTracking()
            .Include(p => p.Categoria)
            .Where(p => p.Activo)
            .Select(p => new
            {
                p.Id,
                p.Nombre,
                Categoria = p.Categoria.Nombre,
                p.Stock,
                p.StockMinimo
            })
            .ToListAsync();

        var resultado = productos
            .Select(p =>
            {
                var minimo = p.StockMinimo > 0 ? p.StockMinimo : umbralStockBajo;
                return new
                {
                    p.Id,
                    p.Nombre,
                    p.Categoria,
                    p.Stock,
                    p.StockMinimo,
                    MinimoEfectivo = minimo,
                    EsAgotado = p.Stock <= 0,
                    EsStockBajo = p.Stock > 0 && p.Stock <= minimo
                };
            })
            .Where(p => p.EsAgotado || p.EsStockBajo)
            .OrderBy(p => p.Stock)
            .ThenBy(p => p.Nombre)
            .Select(p => new ReporteStockBajoDto
            {
                Id = p.Id,
                Nombre = p.Nombre,
                Categoria = p.Categoria,
                Stock = p.Stock,
                StockMinimo = p.StockMinimo,
                MinimoEfectivo = p.MinimoEfectivo,
                Estado = p.EsAgotado ? "Agotado" : "Stock Bajo"
            })
            .ToList();

        return resultado;
    }

    /// <inheritdoc />
    public async Task<DashboardDto> GenerarDashboardAsync()
    {
        var umbralStockBajo = _configuration.GetValue<int>("Dashboard:StockBajoUmbral", 5);

        // "Hoy" y "este mes" se calculan sobre la hora local de Argentina, pero las comparaciones
        // se hacen contra v.Fecha (UTC), por lo que cada límite se convierte al rango UTC equivalente.
        var ahora = ArgentinaTimeZone.ToLocal(DateTime.UtcNow);
        var inicioMesLocal = new DateTime(ahora.Year, ahora.Month, 1);
        var finMesLocal = inicioMesLocal.AddMonths(1).AddDays(-1);

        var (inicioHoy, finHoy) = ArgentinaTimeZone.ToUtcRange(ahora.Date, ahora.Date);
        var (inicioMes, finMes) = ArgentinaTimeZone.ToUtcRange(inicioMesLocal, finMesLocal);

        // Estados considerados como venta concretada (comercialmente).
        // Ventas de hoy
        var ventasHoy = await _context.Ventas
            .AsNoTracking()
            .Where(v => v.Fecha >= inicioHoy && v.Fecha <= finHoy &&
                        (v.Estado == EstadoVenta.Confirmada || v.Estado == EstadoVenta.PendienteFacturacion))
            .Select(v => v.Total)
            .ToListAsync();

        var ventasHoyMonto = ventasHoy.Sum();
        var ventasHoyTransacciones = ventasHoy.Count;
        var ticketPromedioHoy = ventasHoyTransacciones > 0
            ? Math.Round(ventasHoyMonto / ventasHoyTransacciones, 2)
            : 0m;

        // Ventas del mes
        var ventasMes = await _context.Ventas
            .AsNoTracking()
            .Where(v => v.Fecha >= inicioMes && v.Fecha <= finMes &&
                        (v.Estado == EstadoVenta.Confirmada || v.Estado == EstadoVenta.PendienteFacturacion))
            .Select(v => v.Total)
            .ToListAsync();

        var ventasMesMonto = ventasMes.Sum();
        var ventasMesTransacciones = ventasMes.Count;

        // Inventario (solo productos activos)
        var productosActivos = await _context.Productos
            .AsNoTracking()
            .Where(p => p.Activo)
            .Select(p => new { p.Stock, p.StockMinimo, p.Precio, p.PrecioCosto })
            .ToListAsync();

        var productosAgotados = productosActivos.Count(p => p.Stock <= 0);
        // Stock bajo: usa el mínimo propio del producto; si es 0 (no configurado), cae al umbral global.
        var productosStockBajo = productosActivos.Count(p =>
        {
            var minimo = p.StockMinimo > 0 ? p.StockMinimo : umbralStockBajo;
            return p.Stock > 0 && p.Stock <= minimo;
        });
        var valorInventarioCosto = productosActivos.Sum(p => p.PrecioCosto * p.Stock);
        var valorInventarioVenta = productosActivos.Sum(p => p.Precio * p.Stock);

        // Comprobantes pendientes o rechazados (no lograron facturarse)
        var comprobantesPendientes = await _context.Comprobantes
            .AsNoTracking()
            .CountAsync(c => c.Estado == EstadoComprobante.Pendiente || c.Estado == EstadoComprobante.Rechazado);

        // Top 5 productos del mes
        var topProductos = await _context.DetallesVenta
            .AsNoTracking()
            .Where(d => d.Venta.Fecha >= inicioMes && d.Venta.Fecha <= finMes &&
                        (d.Venta.Estado == EstadoVenta.Confirmada || d.Venta.Estado == EstadoVenta.PendienteFacturacion))
            .GroupBy(d => new { d.ProductoId, d.Producto.Nombre, Categoria = d.Producto.Categoria.Nombre })
            .Select(g => new ProductoMasVendidoDto
            {
                Nombre = g.Key.Nombre,
                Categoria = g.Key.Categoria,
                CantidadVendida = g.Sum(d => d.Cantidad)
            })
            .OrderByDescending(p => p.CantidadVendida)
            .Take(5)
            .ToListAsync();

        return new DashboardDto
        {
            VentasHoyMonto = ventasHoyMonto,
            VentasHoyTransacciones = ventasHoyTransacciones,
            TicketPromedioHoy = ticketPromedioHoy,
            VentasMesMonto = ventasMesMonto,
            VentasMesTransacciones = ventasMesTransacciones,
            ProductosStockBajo = productosStockBajo,
            ProductosAgotados = productosAgotados,
            ValorInventarioCosto = valorInventarioCosto,
            ValorInventarioVenta = valorInventarioVenta,
            ComprobantesPendientes = comprobantesPendientes,
            TopProductosMes = topProductos,
            StockBajoUmbral = umbralStockBajo
        };
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
            "stock-bajo" => await ObtenerDatosStockBajoAsync(),
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

    private async Task<(string[] Headers, List<string[]> Rows)> ObtenerDatosStockBajoAsync()
    {
        var stockBajo = await GenerarReporteStockBajoAsync();

        var headers = new[] { "Producto", "Categoría", "Stock", "Stock Mínimo", "Estado" };
        var rows = stockBajo.Select(p => new[]
        {
            p.Nombre,
            p.Categoria,
            p.Stock.ToString(),
            p.MinimoEfectivo.ToString(),
            p.Estado
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

    // --- Estilo del Excel (coherente con la identidad visual de la app) ---
    private const string ColorPrimario = "#1A237E";   // Azul oscuro (encabezado / título)
    private const string ColorFilaPar = "#EEF1F8";     // Azul muy claro (zebra)
    private const string ColorTotales = "#C5CAE9";     // Azul claro (fila de totales)
    private const string ColorBorde = "#B0B6D0";       // Borde suave

    /// <summary>
    /// Tipo de dato inferido para una columna, usado para aplicar el formato
    /// correcto en el Excel (fecha, moneda, entero o texto).
    /// </summary>
    private enum TipoColumna { Texto, Fecha, Moneda, Entero }

    /// <summary>
    /// Genera un archivo Excel (XLSX) con un diseño profesional usando ClosedXML:
    /// título, encabezado con color, filas alternadas, tipos de dato reales
    /// (fecha / moneda / número), bordes, fila de totales y panel inmovilizado.
    /// </summary>
    private static byte[] GenerarExcel(string[] headers, List<string[]> rows, string tipoReporte)
    {
        using var workbook = new XLWorkbook();
        var nombreHoja = ObtenerTituloLegible(tipoReporte);
        var worksheet = workbook.Worksheets.Add(nombreHoja.Length > 31 ? nombreHoja[..31] : nombreHoja);

        var numCols = headers.Length;
        var tiposColumna = InferirTiposColumna(headers, rows);

        // --- 1. Título del reporte (fila 1) ---
        var celdaTitulo = worksheet.Cell(1, 1);
        celdaTitulo.Value = $"Reporte de {nombreHoja}";
        var rangoTitulo = worksheet.Range(1, 1, 1, numCols);
        rangoTitulo.Merge();
        celdaTitulo.Style.Font.Bold = true;
        celdaTitulo.Style.Font.FontSize = 16;
        celdaTitulo.Style.Font.FontColor = XLColor.FromHtml(ColorPrimario);
        celdaTitulo.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        worksheet.Row(1).Height = 24;

        // Subtítulo: fecha de generación (fila 2)
        var celdaSubtitulo = worksheet.Cell(2, 1);
        celdaSubtitulo.Value = $"Generado el {ArgentinaTimeZone.ToLocal(DateTime.UtcNow):dd/MM/yyyy HH:mm}";
        worksheet.Range(2, 1, 2, numCols).Merge();
        celdaSubtitulo.Style.Font.Italic = true;
        celdaSubtitulo.Style.Font.FontSize = 9;
        celdaSubtitulo.Style.Font.FontColor = XLColor.FromHtml("#6B7280");

        // --- 2. Encabezados de la tabla (fila 4) ---
        const int filaEncabezado = 4;
        for (var col = 0; col < numCols; col++)
        {
            var cell = worksheet.Cell(filaEncabezado, col + 1);
            cell.Value = headers[col];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml(ColorPrimario);
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }
        worksheet.Row(filaEncabezado).Height = 20;

        // --- 3. Filas de datos (con tipos reales y zebra) ---
        var primeraFilaDatos = filaEncabezado + 1;
        for (var r = 0; r < rows.Count; r++)
        {
            var filaExcel = primeraFilaDatos + r;
            for (var c = 0; c < rows[r].Length && c < numCols; c++)
            {
                var cell = worksheet.Cell(filaExcel, c + 1);
                AsignarValorTipado(cell, rows[r][c], tiposColumna[c]);
            }

            // Zebra: fondo suave en filas pares
            if (r % 2 == 1)
            {
                worksheet.Range(filaExcel, 1, filaExcel, numCols)
                    .Style.Fill.BackgroundColor = XLColor.FromHtml(ColorFilaPar);
            }
        }

        var ultimaFilaDatos = primeraFilaDatos + rows.Count - 1;

        // --- 4. Fila de totales (suma de columnas de moneda / entero) ---
        var filaTotales = 0;
        if (rows.Count > 0)
        {
            filaTotales = ultimaFilaDatos + 1;
            var rangoTotales = worksheet.Range(filaTotales, 1, filaTotales, numCols);
            rangoTotales.Style.Font.Bold = true;
            rangoTotales.Style.Fill.BackgroundColor = XLColor.FromHtml(ColorTotales);

            worksheet.Cell(filaTotales, 1).Value = "TOTAL";

            for (var c = 0; c < numCols; c++)
            {
                if (tiposColumna[c] is TipoColumna.Moneda or TipoColumna.Entero)
                {
                    var colLetra = worksheet.Cell(primeraFilaDatos, c + 1).Address.ColumnLetter;
                    var formula = $"SUM({colLetra}{primeraFilaDatos}:{colLetra}{ultimaFilaDatos})";
                    var cellTotal = worksheet.Cell(filaTotales, c + 1);
                    cellTotal.FormulaA1 = formula;
                    cellTotal.Style.NumberFormat.Format =
                        tiposColumna[c] == TipoColumna.Moneda ? "$ #,##0.00" : "#,##0";
                    cellTotal.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                }
            }
        }

        // --- 5. Bordes alrededor de encabezado + datos + totales ---
        var ultimaFilaTabla = filaTotales > 0 ? filaTotales : ultimaFilaDatos;
        if (rows.Count > 0 || numCols > 0)
        {
            var rangoTabla = worksheet.Range(filaEncabezado, 1, Math.Max(ultimaFilaTabla, filaEncabezado), numCols);
            rangoTabla.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            rangoTabla.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            rangoTabla.Style.Border.OutsideBorderColor = XLColor.FromHtml(ColorBorde);
            rangoTabla.Style.Border.InsideBorderColor = XLColor.FromHtml(ColorBorde);
        }

        // --- 6. Inmovilizar título + encabezado ---
        worksheet.SheetView.FreezeRows(filaEncabezado);

        // --- 7. Autoajustar columnas (con un mínimo cómodo) ---
        worksheet.Columns().AdjustToContents();
        for (var col = 1; col <= numCols; col++)
        {
            if (worksheet.Column(col).Width < 12)
                worksheet.Column(col).Width = 12;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Infiere el tipo de dato de cada columna analizando todos sus valores.
    /// Una columna es Fecha/Moneda/Entero solo si TODOS sus valores no vacíos
    /// encajan en ese tipo; de lo contrario se trata como Texto.
    /// Los importes se generan con "F2" invariante, por eso se detectan como moneda
    /// las columnas con decimales; los enteros puros (p. ej. Transacciones, Stock) como entero.
    /// </summary>
    private static TipoColumna[] InferirTiposColumna(string[] headers, List<string[]> rows)
    {
        var tipos = new TipoColumna[headers.Length];

        for (var c = 0; c < headers.Length; c++)
        {
            bool tieneValores = false;
            bool todosFecha = true, todosMoneda = true, todosEntero = true;

            foreach (var fila in rows)
            {
                if (c >= fila.Length) continue;
                var valor = fila[c];
                if (string.IsNullOrWhiteSpace(valor)) continue;

                tieneValores = true;

                if (!DateTime.TryParseExact(valor, "yyyy-MM-dd",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                    todosFecha = false;

                var esDecimal = decimal.TryParse(valor, NumberStyles.Number,
                    CultureInfo.InvariantCulture, out var numero);
                if (!esDecimal)
                {
                    todosMoneda = false;
                    todosEntero = false;
                }
                else if (numero != Math.Truncate(numero) || valor.Contains('.'))
                {
                    // Tiene parte decimal => se considera moneda, no entero.
                    todosEntero = false;
                }
            }

            tipos[c] = !tieneValores ? TipoColumna.Texto
                : todosFecha ? TipoColumna.Fecha
                : todosEntero ? TipoColumna.Entero
                : todosMoneda ? TipoColumna.Moneda
                : TipoColumna.Texto;
        }

        return tipos;
    }

    /// <summary>
    /// Asigna el valor a la celda con el tipo y formato adecuados según la columna.
    /// </summary>
    private static void AsignarValorTipado(IXLCell cell, string valor, TipoColumna tipo)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            cell.Value = string.Empty;
            return;
        }

        switch (tipo)
        {
            case TipoColumna.Fecha when DateTime.TryParseExact(valor, "yyyy-MM-dd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha):
                cell.Value = fecha;
                cell.Style.NumberFormat.Format = "dd/mm/yyyy";
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                break;

            case TipoColumna.Moneda when decimal.TryParse(valor, NumberStyles.Number,
                    CultureInfo.InvariantCulture, out var monto):
                cell.Value = monto;
                cell.Style.NumberFormat.Format = "$ #,##0.00";
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                break;

            case TipoColumna.Entero when decimal.TryParse(valor, NumberStyles.Number,
                    CultureInfo.InvariantCulture, out var entero):
                cell.Value = entero;
                cell.Style.NumberFormat.Format = "#,##0";
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                break;

            default:
                cell.Value = valor;
                break;
        }
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

                page.Header().Row(headerRow =>
                {
                    // Logo de la aplicación (si está disponible)
                    var logo = LogoProvider.LogoPng;
                    if (logo is not null)
                    {
                        headerRow.ConstantItem(40).AlignMiddle().Image(logo).FitWidth();
                        headerRow.ConstantItem(10); // separación entre logo y título
                    }

                    headerRow.RelativeItem().AlignMiddle().Text(text =>
                    {
                        text.Span($"Reporte de {ObtenerTituloLegible(tipoReporte)} — ").Bold().FontSize(14);

                        // El inventario y el stock bajo reflejan el estado al momento de generarse,
                        // no un rango de fechas. Los demás reportes usan el período solicitado.
                        if (string.Equals(tipoReporte, "inventario", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(tipoReporte, "stock-bajo", StringComparison.OrdinalIgnoreCase))
                        {
                            text.Span($"al {ArgentinaTimeZone.ToLocal(DateTime.UtcNow):dd/MM/yyyy}").FontSize(12);
                        }
                        else
                        {
                            text.Span($"{fechaInicio:dd/MM/yyyy} a {fechaFin:dd/MM/yyyy}").FontSize(12);
                        }
                    });
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
    /// Devuelve el título legible de un tipo de reporte para encabezados de PDF y nombres de hoja de Excel,
    /// evitando mostrar el identificador crudo (p. ej. "stock-bajo") al usuario.
    /// </summary>
    private static string ObtenerTituloLegible(string tipoReporte) => tipoReporte.ToLowerInvariant() switch
    {
        "ventas" => "Ventas",
        "productos" => "Productos Más Vendidos",
        "inventario" => "Inventario",
        "stock-bajo" => "Stock Bajo",
        _ => tipoReporte
    };

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
