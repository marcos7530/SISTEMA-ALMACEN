using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SistemaAlmacen.Data.Entities;
using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Business.Services;

/// <summary>
/// Genera documentos PDF de comprobantes fiscales usando QuestPDF.
/// Incluye los datos reales del emisor (desde configuración), el tratamiento correcto
/// según el tipo de comprobante (Factura C de monotributo no discrimina IVA) y el
/// código QR obligatorio de AFIP.
/// </summary>
public class ComprobantePdfGenerator
{
    private static readonly CultureInfo CulturaArgentina = new("es-AR");
    private readonly IConfiguration _configuration;

    public ComprobantePdfGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // --- Datos del emisor (desde configuración) ---
    private string EmisorCuit => _configuration["Afip:Cuit"] ?? string.Empty;
    private int PuntoDeVenta => _configuration.GetValue<int>("Afip:PuntoDeVenta", 1);
    private bool EmisorMonotributo => _configuration.GetValue<bool>("Afip:EmisorMonotributo", false);
    private string EmisorRazonSocial => _configuration["Afip:Emisor:RazonSocial"] ?? "Razón Social no configurada";
    private string EmisorNombreFantasia => _configuration["Afip:Emisor:NombreFantasia"] ?? string.Empty;
    private string EmisorDomicilio => _configuration["Afip:Emisor:Domicilio"] ?? string.Empty;
    private string EmisorCondicionIva => _configuration["Afip:Emisor:CondicionIva"] ?? "Responsable Monotributo";
    private string EmisorIngresosBrutos => _configuration["Afip:Emisor:IngresosBrutos"] ?? string.Empty;
    private string EmisorInicioActividades => _configuration["Afip:Emisor:InicioActividades"] ?? string.Empty;

    /// <summary>
    /// Genera un PDF del comprobante fiscal con todos los datos requeridos por AFIP.
    /// </summary>
    /// <param name="comprobante">Datos del comprobante con CAE.</param>
    /// <param name="venta">Venta con detalles, productos y cliente cargados.</param>
    /// <param name="vendedor">Nombre del vendedor que registró la venta.</param>
    /// <returns>Bytes del archivo PDF generado.</returns>
    public byte[] Generar(Comprobante comprobante, Venta venta, string vendedor)
    {
        var esComprobanteC = comprobante.TipoComprobante is 11 or 13;
        var tipoNombre = ObtenerNombreTipoComprobante(comprobante.TipoComprobante);
        var letra = ObtenerLetraComprobante(comprobante.TipoComprobante);

        // QR oficial de AFIP (PNG en bytes) para embeber en el PDF.
        var qrPng = GenerarQrAfip(comprobante, venta);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(header => ComponerEncabezado(header, comprobante, tipoNombre, letra));
                page.Content().Element(content => ComponerContenido(content, comprobante, venta, vendedor, esComprobanteC, qrPng));
                page.Footer().AlignCenter().Text(text =>
                {
                    text.DefaultTextStyle(x => x.FontSize(7).FontColor(Colors.Grey.Medium));
                    text.Span("Comprobante Autorizado por AFIP/ARCA");
                });
            });
        });

        return document.GeneratePdf();
    }

    /// <summary>
    /// Encabezado: datos del emisor a la izquierda, tipo/letra de comprobante y numeración a la derecha.
    /// </summary>
    private void ComponerEncabezado(IContainer header, Comprobante comprobante, string tipoNombre, string letra)
    {
        header.Column(col =>
        {
            col.Item().Row(row =>
            {
                // Emisor
                row.RelativeItem().Column(left =>
                {
                    if (!string.IsNullOrWhiteSpace(EmisorNombreFantasia))
                        left.Item().Text(EmisorNombreFantasia).Bold().FontSize(13);
                    left.Item().Text(EmisorRazonSocial).Bold().FontSize(11);
                    left.Item().Text($"CUIT: {EmisorCuit}");
                    if (!string.IsNullOrWhiteSpace(EmisorDomicilio))
                        left.Item().Text($"Domicilio: {EmisorDomicilio}");
                    left.Item().Text($"Condición frente al IVA: {EmisorCondicionIva}");
                    if (!string.IsNullOrWhiteSpace(EmisorIngresosBrutos))
                        left.Item().Text($"Ingresos Brutos: {EmisorIngresosBrutos}");
                    if (!string.IsNullOrWhiteSpace(EmisorInicioActividades))
                        left.Item().Text($"Inicio de Actividades: {EmisorInicioActividades}");
                });

                // Letra del comprobante (recuadro central)
                row.ConstantItem(60).AlignCenter().Column(mid =>
                {
                    mid.Item().Border(1).AlignCenter().Text(letra).Bold().FontSize(24);
                    mid.Item().AlignCenter().Text($"Cód. {comprobante.TipoComprobante:D2}").FontSize(7);
                });

                // Numeración y tipo
                row.RelativeItem().AlignRight().Column(right =>
                {
                    right.Item().Text(tipoNombre).Bold().FontSize(12);
                    right.Item().Text($"Punto de Venta: {PuntoDeVenta:D5}");
                    right.Item().Text($"Comp. Nro: {comprobante.NumeroComprobante:D8}");
                    right.Item().Text($"Fecha de Emisión: {comprobante.FechaEmision:dd/MM/yyyy}");
                });
            });

            col.Item().PaddingVertical(5).LineHorizontal(1);
        });
    }

    /// <summary>
    /// Contenido: datos del receptor, detalle de productos, totales, datos fiscales (CAE) y QR.
    /// </summary>
    private void ComponerContenido(
        IContainer content,
        Comprobante comprobante,
        Venta venta,
        string vendedor,
        bool esComprobanteC,
        byte[] qrPng)
    {
        content.Column(col =>
        {
            // Datos del receptor (cliente)
            col.Item().PaddingBottom(8).Column(receptor =>
            {
                var cliente = venta.Cliente;
                var nombreReceptor = cliente?.Nombre ?? "Consumidor Final";
                var docReceptor = string.IsNullOrWhiteSpace(cliente?.Documento) ? "-" : cliente!.Documento!;
                var condReceptor = cliente is null
                    ? "Consumidor Final"
                    : ObtenerNombreCondicionIva(cliente.CondicionIva);

                receptor.Item().Text($"Cliente: {nombreReceptor}");
                receptor.Item().Text($"CUIT/DNI: {docReceptor}");
                receptor.Item().Text($"Condición frente al IVA: {condReceptor}");
                if (!string.IsNullOrWhiteSpace(cliente?.Direccion))
                    receptor.Item().Text($"Domicilio: {cliente!.Direccion}");
                receptor.Item().Text($"Fecha de Venta: {venta.Fecha:dd/MM/yyyy HH:mm}");
                receptor.Item().Text($"Vendedor: {vendedor}");
                receptor.Item().Text("Condición de Venta: Contado");
            });

            // Tabla de detalles
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(4); // Producto
                    columns.RelativeColumn(1); // Cantidad
                    columns.RelativeColumn(2); // Precio Unit.
                    columns.RelativeColumn(2); // Subtotal
                });

                table.Header(headerRow =>
                {
                    headerRow.Cell().BorderBottom(1).Padding(3).Text("Producto").Bold();
                    headerRow.Cell().BorderBottom(1).Padding(3).AlignRight().Text("Cant.").Bold();
                    headerRow.Cell().BorderBottom(1).Padding(3).AlignRight().Text("P. Unit.").Bold();
                    headerRow.Cell().BorderBottom(1).Padding(3).AlignRight().Text("Subtotal").Bold();
                });

                foreach (var detalle in venta.Detalles)
                {
                    var nombreProducto = detalle.Producto?.Nombre ?? $"Producto #{detalle.ProductoId}";
                    table.Cell().Padding(3).Text(nombreProducto);
                    table.Cell().Padding(3).AlignRight().Text(detalle.Cantidad.ToString());
                    table.Cell().Padding(3).AlignRight().Text(FormatoMoneda(detalle.PrecioUnitario));
                    table.Cell().Padding(3).AlignRight().Text(FormatoMoneda(detalle.Subtotal));
                }
            });

            // Totales
            col.Item().PaddingTop(10).AlignRight().Column(totales =>
            {
                if (esComprobanteC)
                {
                    // Factura C (monotributo): NO se discrimina IVA. Solo el total.
                    totales.Item().Text($"Total: {FormatoMoneda(venta.Total)}").Bold().FontSize(12);
                }
                else
                {
                    // Factura A/B (responsable inscripto): se discrimina el IVA.
                    var (netoGravado, iva) = CalcularImportesDesagregados(venta.Total);
                    totales.Item().Text($"Neto Gravado: {FormatoMoneda(netoGravado)}");
                    totales.Item().Text($"IVA (21%): {FormatoMoneda(iva)}");
                    totales.Item().PaddingTop(3).Text($"Total: {FormatoMoneda(venta.Total)}").Bold().FontSize(12);
                }
            });

            // Datos fiscales + QR
            col.Item().PaddingTop(15).BorderTop(1).PaddingTop(8).Row(row =>
            {
                row.ConstantItem(110).Image(qrPng).FitArea();

                row.RelativeItem().PaddingLeft(10).AlignMiddle().Column(fiscal =>
                {
                    fiscal.Item().Text("CAE N°: " + comprobante.CAE).Bold();
                    fiscal.Item().Text($"Fecha de Vto. de CAE: {comprobante.FechaVencimientoCAE:dd/MM/yyyy}");
                });
            });
        });
    }

    /// <summary>
    /// Genera el código QR oficial de AFIP en formato PNG.
    /// Formato: https://www.afip.gob.ar/fe/qr/?p=BASE64(JSON) según la especificación de AFIP.
    /// </summary>
    private byte[] GenerarQrAfip(Comprobante comprobante, Venta venta)
    {
        long.TryParse(EmisorCuit, out var cuitEmisor);

        // Documento del receptor (si es CUIT/DNI válido).
        int tipoDocRec = 99; // Consumidor Final por defecto
        long nroDocRec = 0;
        var docCliente = venta.Cliente?.Documento;
        if (!string.IsNullOrWhiteSpace(docCliente) && long.TryParse(docCliente, out var docParsed))
        {
            nroDocRec = docParsed;
            // 11 dígitos => CUIT (80); si no, DNI (96).
            tipoDocRec = docCliente.Length == 11 ? 80 : 96;
        }

        long.TryParse(comprobante.CAE, out var caeNum);

        // El JSON del QR usa claves y tipos definidos por AFIP.
        var datos = new Dictionary<string, object?>
        {
            ["ver"] = 1,
            ["fecha"] = comprobante.FechaEmision.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["cuit"] = cuitEmisor,
            ["ptoVta"] = PuntoDeVenta,
            ["tipoCmp"] = comprobante.TipoComprobante,
            ["nroCmp"] = comprobante.NumeroComprobante,
            ["importe"] = venta.Total,
            ["moneda"] = "PES",
            ["ctz"] = 1,
            ["tipoDocRec"] = tipoDocRec,
            ["nroDocRec"] = nroDocRec,
            ["tipoCodAut"] = "E", // E = CAE
            ["codAut"] = caeNum
        };

        var json = JsonSerializer.Serialize(datos);
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        var url = $"https://www.afip.gob.ar/fe/qr/?p={base64}";

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.M);
        var pngQr = new PngByteQRCode(data);
        return pngQr.GetGraphic(20);
    }

    private static string FormatoMoneda(decimal valor) => $"${valor.ToString("N2", CulturaArgentina)}";

    /// <summary>
    /// Obtiene el nombre legible del tipo de comprobante AFIP.
    /// </summary>
    private static string ObtenerNombreTipoComprobante(int tipo)
    {
        return tipo switch
        {
            (int)TipoComprobante.FacturaA => "FACTURA A",
            (int)TipoComprobante.FacturaB => "FACTURA B",
            (int)TipoComprobante.FacturaC => "FACTURA C",
            (int)TipoComprobante.NotaCreditoA => "NOTA DE CRÉDITO A",
            (int)TipoComprobante.NotaCreditoB => "NOTA DE CRÉDITO B",
            (int)TipoComprobante.NotaCreditoC => "NOTA DE CRÉDITO C",
            _ => $"COMPROBANTE (Tipo {tipo})"
        };
    }

    /// <summary>
    /// Obtiene la letra del comprobante para el recuadro central.
    /// </summary>
    private static string ObtenerLetraComprobante(int tipo)
    {
        return tipo switch
        {
            (int)TipoComprobante.FacturaA or (int)TipoComprobante.NotaCreditoA => "A",
            (int)TipoComprobante.FacturaB or (int)TipoComprobante.NotaCreditoB => "B",
            (int)TipoComprobante.FacturaC or (int)TipoComprobante.NotaCreditoC => "C",
            _ => "X"
        };
    }

    /// <summary>
    /// Nombre legible de la condición frente al IVA del receptor.
    /// </summary>
    private static string ObtenerNombreCondicionIva(CondicionIva condicion)
    {
        return condicion switch
        {
            CondicionIva.ResponsableInscripto => "Responsable Inscripto",
            CondicionIva.Monotributista => "Responsable Monotributo",
            CondicionIva.Exento => "IVA Sujeto Exento",
            _ => "Consumidor Final"
        };
    }

    /// <summary>
    /// Calcula neto gravado e IVA a partir del total (IVA 21% incluido). Solo para A/B.
    /// </summary>
    private static (decimal netoGravado, decimal iva) CalcularImportesDesagregados(decimal total)
    {
        var netoGravado = Math.Round(total / 1.21m, 2);
        var iva = total - netoGravado;
        return (netoGravado, iva);
    }
}
