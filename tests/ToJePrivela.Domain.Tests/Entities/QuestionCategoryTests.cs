using ToJePrivela.Domain.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Domain.Tests.Entities;

public class QuestionCategoryTests
{
    [Fact]
    public void Constructor_KeepsNameAndAuthor()
    {
        var author = new Player("Duri", "🦊");
        typeof(Player).GetProperty(nameof(Player.Id))!.SetValue(author, 7);

        var category = new QuestionCategory(" Sport ", author);

        Assert.Equal("Sport", category.Name);
        Assert.Equal(7, category.AddedByPlayerId);
        Assert.Same(author, category.AddedByPlayer);
    }

    [Fact]
    public void Constructor_KeepsTheNameKey()
    {
        Assert.Equal("šport", new QuestionCategory(" Šport ").NameKey);
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
}
