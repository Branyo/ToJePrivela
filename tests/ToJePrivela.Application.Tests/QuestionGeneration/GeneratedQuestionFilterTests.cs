using ToJePrivela.Application.Abstractions.Ai;
using ToJePrivela.Application.QuestionGeneration;

namespace ToJePrivela.Application.Tests.QuestionGeneration;

public class GeneratedQuestionFilterTests
{
    private const string ValidQuestion = "Which year was ChatGPT publicly released?";

    [Fact]
    public void IsUsable_AcceptsAValidQuestion()
    {
        Assert.True(GeneratedQuestionFilter.IsUsable(new GeneratedQuestion(ValidQuestion, "2022")));
    }

    [Theory]
    [InlineData(ValidQuestion, "two thousand")]
    [InlineData(ValidQuestion, "3,5")]
    [InlineData(ValidQuestion, " ")]
    [InlineData("Short", "5")]
    public void IsUsable_RejectsQuestionsTheDomainWouldReject(string text, string answer)
    {
        Assert.False(GeneratedQuestionFilter.IsUsable(new GeneratedQuestion(text, answer)));
    }

    [Theory]
    [InlineData("1000000000001")]
    [InlineData("-1000000000001")]
    [InlineData("5000000000000")]
    public void IsUsable_RejectsAnswersAboveOneTrillion(string answer)
    {
        Assert.False(GeneratedQuestionFilter.IsUsable(new GeneratedQuestion(ValidQuestion, answer)));
    }

    [Fact]
    public void IsUsable_AcceptsAnAnswerOfExactlyOneTrillion()
    {
        Assert.True(GeneratedQuestionFilter.IsUsable(new GeneratedQuestion(ValidQuestion, "1000000000000")));
    }

    [Theory]
    [InlineData("In 1969, which year did Apollo 11 land on the Moon?", "1969")]
    [InlineData("How many kilometers is the 42 km long marathon?", "42")]
    [InlineData("How many meters is 3,5 meters rounded to one decimal?", "3.5")]
    [InlineData("How many inhabitants does a town of 1 000 000 people have?", "1000000")]
    [InlineData("How many degrees below zero is -40 degrees Celsius?", "-40")]
    public void IsUsable_RejectsQuestionsThatStateTheirAnswer(string text, string answer)
    {
        Assert.False(GeneratedQuestionFilter.IsUsable(new GeneratedQuestion(text, answer)));
    }

    [Theory]
    [InlineData("How many players did a Formula 1 team field in 2020?", "2")]
    [InlineData("How many years after 1945 did the Berlin Wall fall?", "44")]
    [InlineData("How many kilometers long is the river Danube?", "2850")]
    public void IsUsable_KeepsQuestionsWithOtherNumbers(string text, string answer)
    {
        Assert.True(GeneratedQuestionFilter.IsUsable(new GeneratedQuestion(text, answer)));
    }
}
