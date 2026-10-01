namespace ToJePrivela.Domain.Common;

/// <summary>
/// Every time the domain keeps is UTC. A time that carries an offset (<see cref="DateTimeKind.Local"/>, as
/// System.Text.Json reads <c>"…+02:00"</c>) is converted; one without any (<see cref="DateTimeKind.Unspecified"/>)
/// is taken to be UTC already.
/// </summary>
public static class UtcTime
{
    public static DateTime Normalize(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    public static DateTime? Normalize(DateTime? value) => value is DateTime time ? Normalize(time) : null;
}
