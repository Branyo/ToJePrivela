using ToJePrivela.Domain.Common;

namespace ToJePrivela.Domain.Tests.Common;

public class UtcTimeTests
{
    [Fact]
    public void Normalize_KeepsAUtcTime()
    {
        var time = new DateTime(2026, 9, 30, 8, 0, 0, DateTimeKind.Utc);

        Assert.Equal(time, UtcTime.Normalize(time));
        Assert.Equal(DateTimeKind.Utc, UtcTime.Normalize(time).Kind);
    }

    [Fact]
    public void Normalize_TakesATimeWithoutOffsetAsUtc()
    {
        var normalized = UtcTime.Normalize(new DateTime(2026, 9, 30, 8, 0, 0, DateTimeKind.Unspecified));

        Assert.Equal(new DateTime(2026, 9, 30, 8, 0, 0, DateTimeKind.Utc), normalized);
        Assert.Equal(DateTimeKind.Utc, normalized.Kind);
    }

    [Fact]
    public void Normalize_ConvertsALocalTimeToUtc()
    {
        var local = new DateTimeOffset(2026, 9, 30, 10, 0, 0, TimeSpan.FromHours(2)).LocalDateTime;

        var normalized = UtcTime.Normalize(local);

        Assert.Equal(new DateTime(2026, 9, 30, 8, 0, 0, DateTimeKind.Utc), normalized);
        Assert.Equal(DateTimeKind.Utc, normalized.Kind);
    }

    [Fact]
    public void Normalize_KeepsNull()
    {
        Assert.Null(UtcTime.Normalize((DateTime?)null));
    }
}
