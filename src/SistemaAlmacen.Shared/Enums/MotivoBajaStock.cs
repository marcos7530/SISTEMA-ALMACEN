namespace SistemaAlmacen.Shared.Enums;

/// <summary>
/// Motivos por los que se puede dar de baja stock de un producto.
/// </summary>
public enum MotivoBajaStock
{
    Rotura = 1,
    Vencimiento = 2,
    Perdida = 3,
    Robo = 4,
    ErrorDeCarga = 5,
    Otro = 6
}
