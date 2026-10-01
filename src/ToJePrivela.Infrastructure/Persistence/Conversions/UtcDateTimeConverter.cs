using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ToJePrivela.Domain.Common;

namespace ToJePrivela.Infrastructure.Persistence.Conversions;

/// <summary>
/// SQLite stores a time as text without its kind, so it would come back <see cref="DateTimeKind.Unspecified"/>
/// and be serialized without the "Z". Everything is written as UTC and read back marked as UTC.
/// </summary>
public sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(
            value => UtcTime.Normalize(value),
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
    {
    }
}
