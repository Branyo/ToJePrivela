using ToJePrivela.Ai.Parsing;

namespace ToJePrivela.Ai.Tests.Parsing;

public class GeneratedQuestionParserTests
{
    private readonly GeneratedQuestionParser _sut = new();

    [Fact]
    public void Parse_ReadsAPlainJsonArray()
    {
        const string reply = """[{"question": "Which year was ChatGPT released?", "answer": 2022}]""";

        var questions = _sut.Parse(reply);

        var question = Assert.Single(questions);
        Assert.Equal("Which year was ChatGPT released?", question.Question);
        Assert.Equal("2022", question.Answer);
    }

    [Fact]
    public void Parse_ReadsQuotedAnswers()
    {
        const string reply = """[{"question": "Which year was ChatGPT released?", "answer": "2022"}]""";

        Assert.Equal("2022", Assert.Single(_sut.Parse(reply)).Answer);
    }

    [Fact]
    public void Parse_IgnoresMarkdownFences()
    {
        const string reply = """
            ```json
            [{"question": "Which year was ChatGPT released?", "answer": 2022}]
            ```
            """;

        Assert.Single(_sut.Parse(reply));
    }

    [Fact]
    public void Parse_IgnoresProseAroundTheArray()
    {
        const string reply = """
            Sure, here are the questions:
            [{"question": "Which year was ChatGPT released?", "answer": 2022}]
            Hope this helps!
            """;

        Assert.Single(_sut.Parse(reply));
    }

    [Fact]
    public void Parse_KeepsUsableItemsAndSkipsBrokenOnes()
    {
        const string reply = """
            [
              {"question": "Which year was ChatGPT released?", "answer": 2022},
              {"question": "Missing answer"},
              {"answer": 5},
              "not an object",
              {"question": "How many players are on a pitch?", "answer": "11"}
            ]
            """;

        var questions = _sut.Parse(reply);

        Assert.Equal(2, questions.Count);
        Assert.Equal(["2022", "11"], questions.Select(q => q.Answer));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("I cannot help with that.")]
    [InlineData("[{\"question\": broken}]")]
    [InlineData("{\"question\": \"Not an array\", \"answer\": 1}")]
    public void Parse_ReturnsNothingForUnusableReplies(string? reply)
    {
        Assert.Empty(_sut.Parse(reply));
    }

    [Fact]
    public void Parse_ReturnsNothingForAnEmptyArray()
    {
        Assert.Empty(_sut.Parse("[]"));
    }
}
