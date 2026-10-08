using ToJePrivela.Ai.Parsing;

namespace ToJePrivela.Ai.Tests.Parsing;

public class GeneratedQuestionParserTests
{
    private readonly GeneratedQuestionParser _sut = new();

    [Fact]
    public void Parse_ReadsAPlainJsonArray()
    {
        const string reply = """[{"questionSk": "V ktorom roku vyšiel ChatGPT?", "questionEn": "Which year was ChatGPT released?", "answer": 2022}]""";

        var questions = _sut.Parse(reply);

        var question = Assert.Single(questions);
        Assert.Equal("V ktorom roku vyšiel ChatGPT?", question.QuestionSk);
        Assert.Equal("Which year was ChatGPT released?", question.QuestionEn);
        Assert.Equal("2022", question.Answer);
    }

    [Fact]
    public void Parse_ReadsQuotedAnswers()
    {
        const string reply = """[{"questionSk": "V ktorom roku vyšiel ChatGPT?", "questionEn": "Which year was ChatGPT released?", "answer": "2022"}]""";

        Assert.Equal("2022", Assert.Single(_sut.Parse(reply)).Answer);
    }

    [Fact]
    public void Parse_IgnoresMarkdownFences()
    {
        const string reply = """
            ```json
            [{"questionSk": "V ktorom roku vyšiel ChatGPT?", "questionEn": "Which year was ChatGPT released?", "answer": 2022}]
            ```
            """;

        Assert.Single(_sut.Parse(reply));
    }

    [Fact]
    public void Parse_IgnoresProseAroundTheArray()
    {
        const string reply = """
            Sure, here are the questions:
            [{"questionSk": "V ktorom roku vyšiel ChatGPT?", "questionEn": "Which year was ChatGPT released?", "answer": 2022}]
            Hope this helps!
            """;

        Assert.Single(_sut.Parse(reply));
    }

    [Fact]
    public void Parse_KeepsUsableItemsAndSkipsBrokenOnes()
    {
        const string reply = """
            [
              {"questionSk": "V ktorom roku vyšiel ChatGPT?", "questionEn": "Which year was ChatGPT released?", "answer": 2022},
              {"questionSk": "Chýba odpoveď", "questionEn": "Missing answer"},
              {"questionSk": "Chýba anglický text?", "answer": 5},
              {"questionEn": "Missing Slovak text?", "answer": 5},
              {"answer": 5},
              "not an object",
              {"questionSk": "Koľko hráčov je na ihrisku?", "questionEn": "How many players are on a pitch?", "answer": "11"}
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
    [InlineData("[{\"questionSk\": broken}]")]
    [InlineData("{\"questionSk\": \"Nie je pole\", \"questionEn\": \"Not an array\", \"answer\": 1}")]
    public void Parse_ReturnsNothingForUnusableReplies(string? reply)
    {
        Assert.Empty(_sut.Parse(reply));
    }

    [Fact]
    public void Parse_ReturnsNothingForAnEmptyArray()
    {
        Assert.Empty(_sut.Parse("[]"));
    }

    [Fact]
    public void ParseSubtopics_ReadsAFencedArrayOfStrings()
    {
        const string reply = """
            ```json
            ["Football", " Tennis ", "Ice hockey"]
            ```
            """;

        Assert.Equal(["Football", "Tennis", "Ice hockey"], _sut.ParseSubtopics(reply));
    }

    [Fact]
    public void ParseSubtopics_SkipsBlankDuplicateAndNonStringItems()
    {
        const string reply = """["Football", "", "football", 5, {"name": "x"}, "Tennis"]""";

        Assert.Equal(["Football", "Tennis"], _sut.ParseSubtopics(reply));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("I cannot help with that.")]
    [InlineData("[\"broken]")]
    public void ParseSubtopics_ReturnsNothingForUnusableReplies(string? reply)
    {
        Assert.Empty(_sut.ParseSubtopics(reply));
    }
}
