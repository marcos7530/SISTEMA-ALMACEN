namespace SistemaAlmacen.Business.Models.Afip;

/// <summary>
/// Resultado de una prueba de conexión con los Web Services de AFIP/ARCA.
/// Distingue una conexión exitosa de los distintos motivos de fallo, para poder
/// mostrar al usuario un mensaje preciso (por ejemplo, diferenciar una caída de AFIP
/// de un problema de configuración local).
/// </summary>
public class AfipConnectionResult
{
    /// <summary>True si AFIP respondió y sus servidores están operativos.</summary>
    public bool Connected { get; set; }

    /// <summary>
    /// True si el fallo se debe a que los servidores de AFIP/ARCA están caídos o
    /// congestionados (problema del lado de AFIP, no del sistema ni de la configuración).
    /// </summary>
    public bool AfipUnavailable { get; set; }

    /// <summary>Mensaje descriptivo apto para mostrar al usuario.</summary>
    public string Message { get; set; } = string.Empty;
}
