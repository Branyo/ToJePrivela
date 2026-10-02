using ToJePrivela.Domain.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Domain.Tests.Entities;

public class QuestionCategoryTests
{
    [Fact]
    public void Constructor_KeepsTheTrimmedName()
    {
        Assert.Equal("Sport", new QuestionCategory(" Sport ").Name);
    }

    [Fact]
    public void Constructor_KeepsTheNameKey()
    {
        Assert.Equal("šport", new QuestionCategory(" Šport ").NameKey);
    }

    [Theory]
    [InlineData("")]
    [InlineData("S")]
    public void Constructor_RejectsTooShortName(string name)
    {
        Assert.Throws<DomainException>(() => new QuestionCategory(name));
    }

    [Fact]
    public void Constructor_RejectsTooLongName()
    {
        var name = new string('x', QuestionCategory.NameMaxLength + 1);

        Assert.Throws<DomainException>(() => new QuestionCategory(name));
    }
}
