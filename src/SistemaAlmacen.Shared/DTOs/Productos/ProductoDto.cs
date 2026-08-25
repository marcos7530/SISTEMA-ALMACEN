namespace SistemaAlmacen.Shared.DTOs.Productos;

/// <summary>
/// DTO de respuesta con datos de producto.
/// </summary>
public class ProductoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? CodigoBarras { get; set; }
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public int Stock { get; set; }
    public string CategoriaNombre { get; set; } = string.Empty;
    public int CategoriaId { get; set; }
    public bool Activo { get; set; }
}
