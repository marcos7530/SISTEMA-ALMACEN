namespace SistemaAlmacen.Shared.Enums;

/// <summary>
/// Condición frente al IVA de un cliente, según categorías de AFIP.
/// Determina el tipo de comprobante que corresponde emitir.
/// </summary>
public enum CondicionIva
{
    ConsumidorFinal = 1,
    ResponsableInscripto = 2,
    Monotributista = 3,
    Exento = 4
}
