using SistemaAlmacen.Data.Entities;

namespace SistemaAlmacen.Business.Services;

/// <summary>
/// Utilidades para el cálculo del margen efectivo y el precio de venta sugerido.
/// Jerarquía de margen: producto > categoría > margen por defecto del sistema.
/// </summary>
public static class MargenCalculator
{
    /// <summary>
    /// Determina el margen efectivo (en porcentaje) y su origen para un producto.
    /// </summary>
    /// <param name="margenProducto">Margen propio del producto (nullable).</param>
    /// <param name="margenCategoria">Margen de la categoría (nullable).</param>
    /// <param name="margenDefecto">Margen por defecto del sistema.</param>
    public static (decimal margen, string origen) ResolverMargen(
        decimal? margenProducto,
        decimal? margenCategoria,
        decimal margenDefecto)
    {
        if (margenProducto.HasValue)
            return (margenProducto.Value, "Producto");

        if (margenCategoria.HasValue)
            return (margenCategoria.Value, "Categoria");

        return (margenDefecto, "Default");
    }

    /// <summary>
    /// Calcula el precio de venta sugerido a partir de un costo y un margen (porcentaje).
    /// Redondea a 2 decimales.
    /// </summary>
    public static decimal CalcularPrecioSugerido(decimal costo, decimal margenPorcentaje)
    {
        if (costo <= 0)
            return 0m;

        var precio = costo * (1 + margenPorcentaje / 100m);
        return Math.Round(precio, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Determina el margen efectivo de un producto considerando su categoría cargada.
    /// </summary>
    public static (decimal margen, string origen) ResolverMargenProducto(Producto producto, decimal margenDefecto)
    {
        return ResolverMargen(producto.MargenGanancia, producto.Categoria?.MargenGanancia, margenDefecto);
    }
}
