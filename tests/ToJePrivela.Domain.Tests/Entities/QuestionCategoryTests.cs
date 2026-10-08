using ToJePrivela.Domain.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Domain.Tests.Entities;

public class QuestionCategoryTests
{
    [Fact]
    public void Constructor_KeepsBothTrimmedNames()
    {
        var category = new QuestionCategory(" Šport ", " Sport ");

        Assert.Equal("Šport", category.NameSk);
        Assert.Equal("Sport", category.NameEn);
    }

    [Fact]
    public void Constructor_KeepsANameKeyPerLanguage()
    {
        var category = new QuestionCategory(" Šport ", " SPORT ");

        Assert.Equal("šport", category.NameSkKey);
        Assert.Equal("sport", category.NameEnKey);
    }

    [Fact]
    public void NameIn_GivesTheNameInThatLanguage()
    {
        var category = new QuestionCategory("Vtáky", "Birds");

        Assert.Equal("Vtáky", category.NameIn(Language.Sk));
        Assert.Equal("Birds", category.NameIn(Language.En));
    }

    [Theory]
    [InlineData("")]
    [InlineData("S")]
    public void Constructor_RejectsTooShortName(string name)
    {
        Assert.Throws<DomainException>(() => new QuestionCategory(name, "Sport"));
        Assert.Throws<DomainException>(() => new QuestionCategory("Šport", name));
    }

    [Fact]
    public void Constructor_RejectsTooLongName()
    {
        var name = new string('x', QuestionCategory.NameMaxLength + 1);

        Assert.Throws<DomainException>(() => new QuestionCategory(name, "Sport"));
        Assert.Throws<DomainException>(() => new QuestionCategory("Šport", name));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(" S ", false)]
    [InlineData("Šport", true)]
    [InlineData("  Šport  ", true)]
    public void IsValidName_MatchesTheConstructor(string? name, bool expected)
    {
        Assert.Equal(expected, QuestionCategory.IsValidName(name));
    }
}
