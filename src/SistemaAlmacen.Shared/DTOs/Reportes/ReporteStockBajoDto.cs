namespace SistemaAlmacen.Shared.DTOs.Reportes;

/// <summary>
/// DTO con datos de un producto en situación de stock bajo o agotado para reportes.
/// </summary>
public class ReporteStockBajoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public int Stock { get; set; }
    public int StockMinimo { get; set; }
    public int MinimoEfectivo { get; set; }
    public string Estado { get; set; } = string.Empty;
}
