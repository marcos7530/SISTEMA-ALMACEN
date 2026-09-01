using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SistemaAlmacen.Data.Entities;
using SistemaAlmacen.Shared.Enums;

namespace SistemaAlmacen.Business.Services;

/// <summary>
/// Genera documentos PDF de comprobantes fiscales usando QuestPDF.
/// </summary>
public class ComprobantePdfGenerator
{
    /// <summary>
    /// Genera un PDF del comprobante fiscal con todos los datos requeridos.
    /// </summary>
    /// <param name="comprobante">Datos del comprobante con CAE.</param>
    /// <param name="venta">Venta con detalles y productos cargados.</param>
    /// <param name="vendedor">Nombre del vendedor que registró la venta.</param>
    /// <returns>Bytes del archivo PDF generado.</returns>
    private static readonly CultureInfo CulturaArgentina = new("es-AR");

    public byte[] Generar(Comprobante comprobante, Venta venta, string vendedor)
    {
        var tipoNombre = ObtenerNombreTipoComprobante(comprobante.TipoComprobante);
        var (netoGravado, iva) = CalcularImportesDesagregados(venta.Total);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(header =>
                {
                    header.Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(left =>
                            {
                                left.Item().Text("SISTEMA ALMACÉN").Bold().FontSize(14);
                                left.Item().Text("Razón Social: Sistema Almacén S.R.L.");
                                left.Item().Text("Domicilio Comercial: Dirección Fiscal");
                                left.Item().Text("Condición frente al IVA: Responsable Inscripto");
                            });

                            row.RelativeItem().AlignRight().Column(right =>
                            {
                                right.Item().Text(tipoNombre).Bold().FontSize(14);
                                right.Item().Text($"Punto de Venta: 0001");
                                right.Item().Text($"Comp. Nro: {comprobante.NumeroComprobante:D8}");
                                right.Item().Text($"Fecha de Emisión: {comprobante.FechaEmision:dd/MM/yyyy}");
                            });
                        });

                        col.Item().PaddingVertical(5).LineHorizontal(1);
                    });
                });

                page.Content().Element(content =>
                {
                    content.Column(col =>
                    {
                        // Datos de la venta
                        col.Item().PaddingBottom(10).Column(info =>
                        {
                            info.Item().Text($"Fecha de Venta: {venta.Fecha:dd/MM/yyyy HH:mm}");
                            info.Item().Text($"Vendedor: {vendedor}");
                            info.Item().Text($"Condición de Venta: Contado");
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

                            // Header de la tabla
                            table.Header(header =>
                            {
                                header.Cell().BorderBottom(1).Padding(3).Text("Producto").Bold();
                                header.Cell().BorderBottom(1).Padding(3).AlignRight().Text("Cant.").Bold();
                                header.Cell().BorderBottom(1).Padding(3).AlignRight().Text("Precio Unit.").Bold();
                                header.Cell().BorderBottom(1).Padding(3).AlignRight().Text("Subtotal").Bold();
                            });

                            // Filas de detalles
                            foreach (var detalle in venta.Detalles)
                            {
                                var nombreProducto = detalle.Producto?.Nombre ?? $"Producto #{detalle.ProductoId}";
                                table.Cell().Padding(3).Text(nombreProducto);
                                table.Cell().Padding(3).AlignRight().Text(detalle.Cantidad.ToString());
                                table.Cell().Padding(3).AlignRight().Text($"${detalle.PrecioUnitario.ToString("N2", CulturaArgentina)}");
                                table.Cell().Padding(3).AlignRight().Text($"${detalle.Subtotal.ToString("N2", CulturaArgentina)}");
                            }
                        });

                        // Totales
                        col.Item().PaddingTop(10).AlignRight().Column(totales =>
                        {
                            totales.Item().Text($"Subtotal (Neto Gravado): ${netoGravado.ToString("N2", CulturaArgentina)}");
                            totales.Item().Text($"IVA (21%): ${iva.ToString("N2", CulturaArgentina)}");
                            totales.Item().PaddingTop(3).Text($"Total: ${venta.Total.ToString("N2", CulturaArgentina)}").Bold().FontSize(12);
                        });

                        // Datos fiscales
                        col.Item().PaddingTop(20).BorderTop(1).PaddingTop(5).Column(fiscal =>
                        {
                            fiscal.Item().Text("DATOS FISCALES").Bold();
                            fiscal.Item().Text($"CAE: {comprobante.CAE}");
                            fiscal.Item().Text($"Fecha Vto. CAE: {comprobante.FechaVencimientoCAE:dd/MM/yyyy}");
                            fiscal.Item().Text($"Tipo Comprobante: {tipoNombre}");
                            fiscal.Item().Text($"Número: {comprobante.NumeroComprobante:D8}");
                        });
                    });
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Comprobante generado electrónicamente - ");
                    text.Span($"CAE: {comprobante.CAE}");
                });
            });
        });

        return document.GeneratePdf();
    }

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
            _ => $"COMPROBANTE (Tipo {tipo})"
        };
    }

    /// <summary>
    /// Calcula neto gravado e IVA a partir del total (IVA 21% incluido).
    /// </summary>
    private static (decimal netoGravado, decimal iva) CalcularImportesDesagregados(decimal total)
    {
        var netoGravado = Math.Round(total / 1.21m, 2);
        var iva = total - netoGravado;
        return (netoGravado, iva);
    }
}
