using ToJePrivela.Ai.Prompts;
using ToJePrivela.Application.Abstractions.Ai;

namespace ToJePrivela.Ai.Tests.Prompts;

public class QuestionPromptBuilderTests
{
    private readonly QuestionPromptBuilder _sut = new();

    [Fact]
    public void BuildQuestions_MentionsCategoryCountAndBothLanguages()
    {
        var prompt = _sut.BuildQuestions(new QuestionGenerationRequest("Sport", 7));

        Assert.Contains("Sport", prompt);
        Assert.Contains("7", prompt);
        Assert.Contains("Slovak and English", prompt);
    }

    [Fact]
    public void BuildQuestions_AsksForANumericAnswerInJson()
    {
        var prompt = _sut.BuildQuestions(new QuestionGenerationRequest("Sport", 1));

        Assert.Contains("JSON array", prompt);
        Assert.Contains("numeric", prompt);
        Assert.Contains("\"questionSk\"", prompt);
        Assert.Contains("\"questionEn\"", prompt);
        Assert.Contains("\"answer\"", prompt);
        Assert.Contains("same meaning, units and answer", prompt);
    }

    [Fact]
    public void BuildQuestions_NarrowsToTheSubtopic()
    {
        var prompt = _sut.BuildQuestions(new QuestionGenerationRequest("Sport", 5, Subtopic: "Ice hockey"));

        Assert.Contains("Subtopic: Ice hockey", prompt);
    }

    [Fact]
    public void BuildQuestions_ListsTheQuestionsNotToRepeat()
    {
        var prompt = _sut.BuildQuestions(
            new QuestionGenerationRequest("Sport", 5, ExcludedQuestions: ["How long is a marathon?", "How many rings are on the Olympic flag?"]));

        Assert.Contains("do not repeat", prompt);
        Assert.Contains("How long is a marathon?", prompt);
        Assert.Contains("How many rings are on the Olympic flag?", prompt);
    }

    [Fact]
    public void BuildQuestions_LeavesOutSubtopicAndExclusionsWhenThereAreNone()
    {
        var prompt = _sut.BuildQuestions(new QuestionGenerationRequest("Sport", 5, null, []));

        Assert.DoesNotContain("Subtopic", prompt);
        Assert.DoesNotContain("do not repeat", prompt);
    }

    [Fact]
    public void BuildQuestions_AsksForVariedHiddenAndBoundedAnswers()
    {
        var prompt = _sut.BuildQuestions(new QuestionGenerationRequest("Sport", 5));

        Assert.Contains("Vary the kind of number", prompt);
        Assert.Contains("kilograms", prompt);
        Assert.Contains("Never reveal the answer in the question", prompt);
        Assert.Contains("1 000 000 000 000", prompt);
        Assert.Contains("How many millions", prompt);
    }

    [Fact]
    public void BuildSubtopics_PrefersBroadNonOverlappingSubtopics()
    {
        var prompt = _sut.BuildSubtopics("Toys", 4);

        Assert.Contains("broad, general subtopics", prompt);
        Assert.Contains("\"Construction sets\" rather than \"Lego\"", prompt);
        Assert.Contains("must not overlap", prompt);
    }

    [Fact]
    public void BuildSubtopics_AsksForTheRequestedNumberOfSubtopicsAsAJsonArray()
    {
        var prompt = _sut.BuildSubtopics("Sport", 4);

        Assert.Contains("\"Sport\"", prompt);
        Assert.Contains("4 distinct", prompt);
        Assert.Contains("Target language: English", prompt);
        Assert.Contains("JSON array of strings", prompt);
    }
}
