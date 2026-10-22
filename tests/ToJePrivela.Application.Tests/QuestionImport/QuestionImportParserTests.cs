using ToJePrivela.Application.Abstractions.Ai;
using ToJePrivela.Application.QuestionImport;

namespace ToJePrivela.Application.Tests.QuestionImport;

public class QuestionImportParserTests
{
    [Fact]
    public void Parse_ReadsTheGeneratorsFormat()
    {
        var items = QuestionImportParser.Parse(
            """
            [{"questionSk": " V ktorom roku bol verejne spustený ChatGPT? ", "questionEn": "Which year was ChatGPT publicly released?", "answer": 2022},
             {"questionSk": "Koľko metrov meria Eiffelova veža?", "questionEn": "How many meters tall is the Eiffel Tower?", "answer": "330"}]
            """);

        Assert.Equal(
            [
                new GeneratedQuestion("V ktorom roku bol verejne spustený ChatGPT?", "Which year was ChatGPT publicly released?", "2022"),
                new GeneratedQuestion("Koľko metrov meria Eiffelova veža?", "How many meters tall is the Eiffel Tower?", "330"),
            ],
            items!);
    }

    [Fact]
    public void Parse_IgnoresMarkdownFencesAndProseAroundTheArray()
    {
        var items = QuestionImportParser.Parse(
            "Here you go:\n```json\n[{\"questionSk\": \"Otázka?\", \"questionEn\": \"Question?\", \"answer\": -3.5}]\n```");

        Assert.Equal("-3.5", Assert.Single(items!)!.Answer);
    }

    [Fact]
    public void Parse_PassesOverBracketsInTheProse()
    {
        var items = QuestionImportParser.Parse(
            """Here are [1] question [see below]: [{"questionSk": "Otázka?", "questionEn": "Question?", "answer": 7}] [end]""");

        Assert.Equal("7", Assert.Single(items!)!.Answer);
    }

    [Theory]
    [InlineData("""[{"questionEn": "Question?", "answer": 1}]""")]
    [InlineData("""[{"questionSk": "Otázka?", "questionEn": " ", "answer": 1}]""")]
    [InlineData("""[{"questionSk": "Otázka?", "questionEn": "Question?", "answer": true}]""")]
    [InlineData("""[{"questionSk": "Otázka?", "questionEn": "Question?", "answer": null}]""")]
    [InlineData("""["Otázka?"]""")]
    public void Parse_KeepsTheSlotOfAnItemThatIsNotAQuestion(string json)
    {
        Assert.Null(Assert.Single(QuestionImportParser.Parse(json)!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("""{"questionSk": "Otázka?", "questionEn": "Question?", "answer": 1}""")]
    [InlineData("""[{"questionSk": "Otázka?", "questionEn": "Question?", "answer": 1},]""")]
    [InlineData("] no array [")]
    public void Parse_ReturnsNullWithoutAReadableArray(string? json)
    {
        Assert.Null(QuestionImportParser.Parse(json));
    }

    [Fact]
    public void Parse_ReadsAnEmptyArray()
    {
        Assert.Empty(QuestionImportParser.Parse("[]")!);
    }
}
