namespace SistemaAlmacen.Shared.DTOs.Compras;

/// <summary>
/// Resultado del cálculo de precio de venta sugerido a partir de un costo y el margen efectivo.
/// </summary>
public class PrecioSugeridoDto
{
    public int ProductoId { get; set; }
    public decimal CostoUnitario { get; set; }

    /// <summary>Margen efectivo aplicado (producto > categoría > default), en porcentaje.</summary>
    public decimal MargenEfectivo { get; set; }

    /// <summary>Origen del margen: "Producto", "Categoria" o "Default".</summary>
    public string OrigenMargen { get; set; } = string.Empty;

    /// <summary>Precio de venta sugerido = costo * (1 + margen/100), redondeado a 2 decimales.</summary>
    public decimal PrecioVentaSugerido { get; set; }
}
