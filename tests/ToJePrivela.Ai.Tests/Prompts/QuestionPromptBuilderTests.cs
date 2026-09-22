using ToJePrivela.Ai.Prompts;

namespace ToJePrivela.Ai.Tests.Prompts;

public class QuestionPromptBuilderTests
{
    private readonly QuestionPromptBuilder _sut = new();

    [Fact]
    public void Build_MentionsCategoryCountAndLanguage()
    {
        var prompt = _sut.Build("Sport", 7, "Slovak");

        Assert.Contains("Sport", prompt);
        Assert.Contains("7", prompt);
        Assert.Contains("Slovak", prompt);
    }

    [Fact]
    public void Build_AsksForANumericAnswerInJson()
    {
        var prompt = _sut.Build("Sport", 1, "Slovak");

        Assert.Contains("JSON array", prompt);
        Assert.Contains("numeric", prompt);
        Assert.Contains("\"question\"", prompt);
        Assert.Contains("\"answer\"", prompt);
    }
}
