namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Utils;

/// <summary>
/// Calendario del ministerio en America/Bogota (UTC-5, sin horario de verano). Los momentos se guardan en UTC;
/// las fechas sin hora (cumpleaños, fechas programadas) se guardan como fecha y nunca se convierten.
/// </summary>
public static class LocalCalendar
{
    public static readonly TimeZoneInfo Bogota = FindBogota();

    public static DateOnly Today(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        return DateOnly.FromDateTime(ToLocal(timeProvider.GetUtcNow().UtcDateTime));
    }

    public static DateTime ToLocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Bogota);

    /// <summary>Inicio del día local <paramref name="date"/> expresado en UTC.</summary>
    public static DateTime StartOfDayUtc(DateOnly date) =>
        TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified), Bogota);

    public static DateOnly LocalDateOf(DateTime utc) => DateOnly.FromDateTime(ToLocal(utc));

    /// <summary>Próxima fecha (hoy o después) en que se cumple años quien nació en <paramref name="birthDate"/>.</summary>
    public static DateOnly NextBirthday(DateOnly birthDate, DateOnly today)
    {
        var thisYear = BirthdayIn(birthDate, today.Year);
        return thisYear >= today ? thisYear : BirthdayIn(birthDate, today.Year + 1);
    }

    /// <summary>Cumpleaños en el año dado; un 29 de febrero se celebra el 28 en años no bisiestos.</summary>
    public static DateOnly BirthdayIn(DateOnly birthDate, int year) =>
        new(year, birthDate.Month, Math.Min(birthDate.Day, DateTime.DaysInMonth(year, birthDate.Month)));

    /// <summary>
    ///     Obtiene la zona horaria de Bogotá.
    /// </summary>
    /// <remarks>
    ///     Imágenes de contenedor sin base de zonas horarias: Colombia no tiene horario de verano.
    /// </remarks>
    private static TimeZoneInfo FindBogota()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.CreateCustomTimeZone("America/Bogota", TimeSpan.FromHours(-5), "America/Bogota", "America/Bogota");
        }
    }
}
