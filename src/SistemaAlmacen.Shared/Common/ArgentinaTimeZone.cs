namespace SistemaAlmacen.Shared.Common;

/// <summary>
/// Única fuente de verdad para la zona horaria de negocio (Argentina, America/Argentina/Buenos_Aires)
/// y conversiones centralizadas entre hora local y UTC.
/// Los datos de fecha se persisten en UTC (DateTime.UtcNow) pero EF los devuelve con Kind=Unspecified,
/// por lo que todas las conversiones normalizan el Kind explícitamente.
/// </summary>
public static class ArgentinaTimeZone
{
    private static readonly Lazy<TimeZoneInfo> _instance = new(ResolveTimeZone);

    /// <summary>
    /// Zona horaria de negocio (Argentina). Se resuelve una sola vez de forma perezosa.
    /// </summary>
    public static TimeZoneInfo Instance => _instance.Value;

    private static TimeZoneInfo ResolveTimeZone()
    {
        // Se intentan dos identificadores para garantizar resolución multiplataforma:
        // - "America/Argentina/Buenos_Aires" es el id IANA usado en Linux/macOS y en Windows moderno con ICU,
        //   además de Blazor WASM que trae los datos IANA embebidos.
        // - "Argentina Standard Time" es el id clásico de Windows, usado como respaldo cuando ICU no está disponible.
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Argentina Standard Time");
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Argentina Standard Time");
        }
    }

    /// <summary>
    /// Convierte un instante UTC a la hora local de Argentina.
    /// Asume que el valor de entrada representa UTC (aunque su Kind sea Unspecified, como ocurre con EF).
    /// </summary>
    /// <param name="utc">Instante en UTC a convertir.</param>
    /// <returns>El mismo instante expresado en hora local de Argentina.</returns>
    public static DateTime ToLocal(DateTime utc)
    {
        var utcNormalized = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utcNormalized, Instance);
    }

    /// <summary>
    /// Convierte un rango de fechas local de Argentina (solo día) al rango UTC equivalente
    /// para filtrar columnas almacenadas en UTC.
    /// El límite inferior es la medianoche local del día de inicio; el superior es el final del día (23:59:59.9999999) local del día de fin.
    /// Cuando un límite es null se usa UTC MinValue/MaxValue para no acotar ese extremo.
    /// </summary>
    /// <param name="fechaInicioLocal">Fecha de inicio en hora local (se usa solo la parte de fecha). Null = sin límite inferior.</param>
    /// <param name="fechaFinLocal">Fecha de fin en hora local (se usa solo la parte de fecha). Null = sin límite superior.</param>
    /// <returns>Tupla con el inicio y fin del rango expresados en UTC.</returns>
    public static (DateTime inicioUtc, DateTime finUtc) ToUtcRange(DateTime? fechaInicioLocal, DateTime? fechaFinLocal)
    {
        DateTime inicioUtc;
        if (fechaInicioLocal is null)
        {
            inicioUtc = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
        }
        else
        {
            var inicioLocal = DateTime.SpecifyKind(fechaInicioLocal.Value.Date, DateTimeKind.Unspecified);
            inicioUtc = TimeZoneInfo.ConvertTimeToUtc(inicioLocal, Instance);
        }

        DateTime finUtc;
        if (fechaFinLocal is null)
        {
            // Se retorna MaxValue directamente para evitar overflow al convertir a UTC.
            finUtc = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);
        }
        else
        {
            var finLocal = DateTime.SpecifyKind(fechaFinLocal.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Unspecified);
            finUtc = TimeZoneInfo.ConvertTimeToUtc(finLocal, Instance);
        }

        return (inicioUtc, finUtc);
    }
}
