using ToJePrivela.Domain.Common;

namespace ToJePrivela.Domain.Tests.Common;

public class GuardTests
{
    [Theory]
    [InlineData("0", true)]
    [InlineData("42", true)]
    [InlineData("-7", true)]
    [InlineData("3.5", true)]
    [InlineData("1,000", false)]
    [InlineData("3,5", false)]
    [InlineData("abc", false)]
    [InlineData("42px", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsNumeric_RecognisesInvariantNumbers(string? value, bool expected)
    {
        Assert.Equal(expected, Guard.IsNumeric(value));
    }

    [Fact]
    public void AgainstNegative_AllowsZero()
    {
        Assert.Equal(0, Guard.AgainstNegative(0, "points"));
    }

    [Fact]
    public void AgainstNegative_RejectsNegativeValue()
    {
        Assert.Throws<DomainException>(() => Guard.AgainstNegative(-1, "points"));
    }

    [Fact]
    public void AgainstOutOfRange_ReturnsValueInsideRange()
    {
        Assert.Equal(3, Guard.AgainstOutOfRange(3, "difficulty", 1, 5));
    }

    [Fact]
    public void AgainstNullOrWhiteSpace_TrimsValue()
    {
        Assert.Equal("value", Guard.AgainstNullOrWhiteSpace("  value  ", "name"));
    }
}
