using ToJePrivela.Ai.Prompts;
using ToJePrivela.Application.Abstractions.Ai;

namespace ToJePrivela.Ai.Tests.Prompts;

public class QuestionPromptBuilderTests
{
    private readonly QuestionPromptBuilder _sut = new();

    [Fact]
    public void BuildQuestions_MentionsCategoryCountAndLanguage()
    {
        var prompt = _sut.BuildQuestions(new QuestionGenerationRequest("Sport", 7), "Slovak");

        Assert.Contains("Sport", prompt);
        Assert.Contains("7", prompt);
        Assert.Contains("Slovak", prompt);
    }

    [Fact]
    public void BuildQuestions_AsksForANumericAnswerInJson()
    {
        var prompt = _sut.BuildQuestions(new QuestionGenerationRequest("Sport", 1), "Slovak");

        Assert.Contains("JSON array", prompt);
        Assert.Contains("numeric", prompt);
        Assert.Contains("\"question\"", prompt);
        Assert.Contains("\"answer\"", prompt);
    }

    [Fact]
    public void BuildQuestions_NarrowsToTheSubtopic()
    {
        var prompt = _sut.BuildQuestions(new QuestionGenerationRequest("Sport", 5, Subtopic: "Ice hockey"), "Slovak");

        Assert.Contains("Subtopic: Ice hockey", prompt);
    }

    [Fact]
    public void BuildQuestions_ListsTheQuestionsNotToRepeat()
    {
        var prompt = _sut.BuildQuestions(
            new QuestionGenerationRequest("Sport", 5, ExcludedQuestions: ["How long is a marathon?", "How many rings are on the Olympic flag?"]),
            "Slovak");

        Assert.Contains("do not repeat", prompt);
        Assert.Contains("How long is a marathon?", prompt);
        Assert.Contains("How many rings are on the Olympic flag?", prompt);
    }

    [Fact]
    public void BuildQuestions_LeavesOutSubtopicAndExclusionsWhenThereAreNone()
    {
        var prompt = _sut.BuildQuestions(new QuestionGenerationRequest("Sport", 5, null, []), "Slovak");

        Assert.DoesNotContain("Subtopic", prompt);
        Assert.DoesNotContain("do not repeat", prompt);
    }

    [Fact]
    public void BuildSubtopics_AsksForTheRequestedNumberOfSubtopicsAsAJsonArray()
    {
        var prompt = _sut.BuildSubtopics("Sport", 4, "Slovak");

        Assert.Contains("\"Sport\"", prompt);
        Assert.Contains("4 distinct", prompt);
        Assert.Contains("Slovak", prompt);
        Assert.Contains("JSON array of strings", prompt);
    }
}
