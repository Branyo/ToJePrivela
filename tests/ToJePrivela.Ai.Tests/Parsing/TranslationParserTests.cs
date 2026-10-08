using ToJePrivela.Ai.Parsing;

namespace ToJePrivela.Ai.Tests.Parsing;

public class TranslationParserTests
{
    private readonly TranslationParser _sut = new();

    [Theory]
    [InlineData("Birds", "Birds")]
    [InlineData("  Martial arts actors \n", "Martial arts actors")]
    [InlineData("\"Toys\"", "Toys")]
    [InlineData("„Hračky“", "Hračky")]
    [InlineData("**Trains**.", "Trains")]
    [InlineData("Translation: Toys", "Toys")]
    [InlineData("Preklad: \"Vtáky\"", "Vtáky")]
    [InlineData("\nVlaky\nThis is the Slovak word for trains.", "Vlaky")]
    public void Parse_ReadsTheTranslation(string reply, string expected)
    {
        Assert.Equal(expected, _sut.Parse(reply));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  \n ")]
    [InlineData("\"\"")]
    public void Parse_ReturnsNullWithoutATranslation(string? reply)
    {
        Assert.Null(_sut.Parse(reply));
    }
}
