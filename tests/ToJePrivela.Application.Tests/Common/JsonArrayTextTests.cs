using System.Text.Json;
using ToJePrivela.Application.Common;

namespace ToJePrivela.Application.Tests.Common;

public class JsonArrayTextTests
{
    [Fact]
    public void Find_TakesTheFirstArrayWithAnItemOfTheKind()
    {
        using var document = JsonArrayText.Find("""Pick [1] of these [2, 3]: [{"a": 1}] and [{"b": 2}]""", JsonValueKind.Object);

        Assert.Equal("""[{"a": 1}]""", document!.RootElement.GetRawText());
    }

    [Fact]
    public void Find_FallsBackToTheFirstReadableArray()
    {
        using var document = JsonArrayText.Find("broken [ then [] and [1]", JsonValueKind.Object);

        Assert.Equal("[]", document!.RootElement.GetRawText());
    }

    [Fact]
    public void Find_DoesNotTryArraysNestedInOneAlreadyRead()
    {
        using var document = JsonArrayText.Find("""[[{"a": 1}]]""", JsonValueKind.Object);

        Assert.Equal("""[[{"a": 1}]]""", document!.RootElement.GetRawText());
    }

    [Fact]
    public void Find_GivesUpAfterMaxStarts()
    {
        var text = new string('[', JsonArrayText.MaxStarts) + " " + """[{"a": 1}]""";

        Assert.Null(JsonArrayText.Find(text, JsonValueKind.Object));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    [InlineData("no array here")]
    [InlineData("] no array [")]
    public void Find_ReturnsNullWithoutAReadableArray(string? text)
    {
        Assert.Null(JsonArrayText.Find(text, JsonValueKind.Object));
    }

    [Theory]
    [InlineData("""{"v": "42"}""", "42")]
    [InlineData("""{"v": 42.5}""", "42.5")]
    [InlineData("""{"v": true}""", null)]
    [InlineData("""{"v": null}""", null)]
    [InlineData("""{"other": 1}""", null)]
    public void ReadScalar_ReadsStringsAndNumbersOnly(string json, string? expected)
    {
        using var document = JsonDocument.Parse(json);

        Assert.Equal(expected, JsonArrayText.ReadScalar(document.RootElement, "v"));
    }
}
