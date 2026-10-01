using ToJePrivela.Application.Common.Validation;

namespace ToJePrivela.Application.Tests.Common.Validation;

public class ValidationAttributeTests
{
    [Theory]
    [InlineData("Brano", true)]
    [InlineData("  Brano  ", true)]
    [InlineData("   a    ", false)]
    [InlineData("toolong", false)]
    [InlineData(null, true)]
    public void TrimmedLength_MeasuresTheTrimmedText(string? value, bool valid)
    {
        Assert.Equal(valid, new TrimmedLengthAttribute(2, 6).IsValid(value));
    }

    [Fact]
    public void TrimmedLength_PutsTheLimitsIntoTheMessage()
    {
        var attribute = new TrimmedLengthAttribute(2, 50) { ErrorMessage = "{0} should have from {1} to {2} characters." };

        Assert.Equal("Name should have from 2 to 50 characters.", attribute.FormatErrorMessage("Name"));
    }

    [Theory]
    [InlineData("42", true)]
    [InlineData("-3.5", true)]
    [InlineData(" 7 ", true)]
    [InlineData("eight", false)]
    [InlineData("3,5", false)]
    [InlineData("1 000", false)]
    [InlineData("   ", false)]
    [InlineData(null, true)]
    public void Numeric_AcceptsOnlyAnswersTheDomainAccepts(string? value, bool valid)
    {
        Assert.Equal(valid, new NumericAttribute().IsValid(value));
    }

    [Theory]
    [InlineData(new[] { 1, 2 }, true)]
    [InlineData(new[] { 1, 1 }, false)]
    [InlineData(new[] { 1, 2, 3, 4 }, false)]
    public void DistinctCount_CountsDifferentItems(int[] value, bool valid)
    {
        Assert.Equal(valid, new DistinctCountAttribute(2, 3).IsValid(value));
    }

    [Fact]
    public void DistinctCount_PutsTheLimitsIntoTheMessage()
    {
        var attribute = new DistinctCountAttribute(2, 12) { ErrorMessage = "A game needs from {1} to {2} different players." };

        Assert.Equal("A game needs from 2 to 12 different players.", attribute.FormatErrorMessage("PlayerIds"));
    }
}
