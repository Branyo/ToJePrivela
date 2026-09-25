using ToJePrivela.Application.QuestionGeneration;

namespace ToJePrivela.Application.Tests.QuestionGeneration;

public class QuestionTextNormalizerTests
{
    [Theory]
    [InlineData("Koľko hráčov má futbalový tím?", "koľko hráčov má futbalový tím")]
    [InlineData("  KOĽKO   hráčov\tmá futbalový tím ?! ", "koľko hráčov má futbalový tím")]
    [InlineData("V ktorom roku...", "v ktorom roku")]
    public void Normalize_IgnoresCaseSpacingAndTrailingPunctuation(string text, string expected)
    {
        Assert.Equal(expected, QuestionTextNormalizer.Normalize(text));
    }

    [Fact]
    public void Normalize_KeepsDiacritics()
    {
        Assert.NotEqual(
            QuestionTextNormalizer.Normalize("Koľko je hodín?"),
            QuestionTextNormalizer.Normalize("Kolko je hodin?"));
    }

    [Fact]
    public void Normalize_KeepsPunctuationInsideTheText()
    {
        Assert.Equal("koľko je 2.5 + 1", QuestionTextNormalizer.Normalize("Koľko je 2.5 + 1?"));
    }
}
