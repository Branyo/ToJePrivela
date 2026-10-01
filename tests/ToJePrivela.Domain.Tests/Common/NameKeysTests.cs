using ToJePrivela.Domain.Common;

namespace ToJePrivela.Domain.Tests.Common;

public class NameKeysTests
{
    [Theory]
    [InlineData("Štefan", "štefan")]
    [InlineData("ĽUBOŠ", "ľuboš")]
    [InlineData("  Brano ", "brano")]
    [InlineData("ÁČĎÉÍĹĽŇÓÔŔŠŤÚÝŽ", "áčďéíĺľňóôŕšťúýž")]
    public void Of_FoldsCaseOfEveryLetter(string name, string expected)
    {
        Assert.Equal(expected, NameKeys.Of(name));
    }

    [Fact]
    public void Of_KeepsDiacritics()
    {
        Assert.NotEqual(NameKeys.Of("Štefan"), NameKeys.Of("Stefan"));
    }

    [Fact]
    public void Of_TreatsComposedAndDecomposedLettersAlike()
    {
        Assert.Equal(NameKeys.Of("Š"), NameKeys.Of("Š"));
    }
}
