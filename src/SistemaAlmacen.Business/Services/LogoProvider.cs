using System.Reflection;

namespace SistemaAlmacen.Business.Services;

/// <summary>
/// Provee el logo de la aplicación (la "tiendita" del Sistema POS) como bytes PNG.
/// El logo está embebido como recurso en el ensamblado (Assets/logo.png), por lo que
/// está disponible en tiempo de ejecución sin depender de rutas del sistema de archivos.
/// Se carga una sola vez y se cachea para reutilizarlo en todos los PDF generados.
/// </summary>
public static class LogoProvider
{
    private static readonly Lazy<byte[]?> _logoPng = new(CargarLogo);

    /// <summary>
    /// Bytes PNG del logo, o <c>null</c> si el recurso no se pudo cargar.
    /// </summary>
    public static byte[]? LogoPng => _logoPng.Value;

    private static byte[]? CargarLogo()
    {
        var assembly = typeof(LogoProvider).Assembly;

        // El nombre del recurso embebido se compone como
        // "<DefaultNamespace>.<ruta con puntos>". Lo resolvemos de forma robusta
        // buscando el recurso que termina en "Assets.logo.png".
        var resourceName = Array.Find(
            assembly.GetManifestResourceNames(),
            name => name.EndsWith("Assets.logo.png", StringComparison.OrdinalIgnoreCase));

        if (resourceName is null)
        {
            return null;
        }

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            return null;
        }

        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}
