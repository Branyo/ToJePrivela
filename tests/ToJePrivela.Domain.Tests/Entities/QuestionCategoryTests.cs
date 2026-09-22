using ToJePrivela.Domain.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Domain.Tests.Entities;

public class QuestionCategoryTests
{
    [Fact]
    public void Constructor_KeepsNameAndAuthor()
    {
        var category = new QuestionCategory(" Sport ", addedByPlayerId: 7);

        Assert.Equal("Sport", category.Name);
        Assert.Equal(7, category.AddedByPlayerId);
    }

    [Fact]
    public void Constructor_AllowsNoAuthor()
    {
        var category = new QuestionCategory("Sport");

        Assert.Null(category.AddedByPlayerId);
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

    [Fact]
    public void Rename_ReplacesTheName()
    {
        var category = new QuestionCategory("Sport");

        category.Rename("History");

        Assert.Equal("History", category.Name);
    }
}
