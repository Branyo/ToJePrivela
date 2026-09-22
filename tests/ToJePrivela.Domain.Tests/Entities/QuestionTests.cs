using ToJePrivela.Domain.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Domain.Tests.Entities;

public class QuestionTests
{
    private const string ValidText = "Which year was ChatGPT publicly released?";

    [Theory]
    [InlineData(1, 5)]
    [InlineData(3, 3)]
    [InlineData(5, 1)]
    public void BadPoints_AreInverseToDifficulty(int difficulty, int expectedBadPoints)
    {
        var question = new Question(ValidText, "2022", "History", difficulty);

        Assert.Equal(expectedBadPoints, question.BadPoints);
    }

    [Theory]
    [InlineData("2022")]
    [InlineData("-5")]
    [InlineData("3.14")]
    [InlineData(" 42 ")]
    public void Constructor_AcceptsNumericAnswers(string answer)
    {
        var question = new Question(ValidText, answer, "History", 3);

        Assert.Equal(answer.Trim(), question.Answer);
    }

    [Theory]
    [InlineData("two thousand")]
    [InlineData("2022 AD")]
    [InlineData("")]
    public void Constructor_RejectsNonNumericAnswers(string answer)
    {
        Assert.Throws<DomainException>(() => new Question(ValidText, answer, "History", 3));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Constructor_RejectsDifficultyOutOfRange(int difficulty)
    {
        Assert.Throws<DomainException>(() => new Question(ValidText, "2022", "History", difficulty));
    }

    [Fact]
    public void Constructor_RejectsTooShortText()
    {
        Assert.Throws<DomainException>(() => new Question("Short", "2022", "History", 3));
    }

    [Fact]
    public void Constructor_RejectsTooLongText()
    {
        var text = new string('x', Question.TextMaxLength + 1);

        Assert.Throws<DomainException>(() => new Question(text, "2022", "History", 3));
    }

    [Fact]
    public void Constructor_RejectsInvalidCategory()
    {
        Assert.Throws<DomainException>(() => new Question(ValidText, "2022", "H", 3));
    }

    [Fact]
    public void Update_ReplacesEveryValue()
    {
        var question = new Question(ValidText, "2022", "History", 3);

        question.Update("How many players are on a football pitch?", "11", "Sport", 1);

        Assert.Equal("How many players are on a football pitch?", question.Text);
        Assert.Equal("11", question.Answer);
        Assert.Equal("Sport", question.Category);
        Assert.Equal(1, question.Difficulty);
        Assert.Equal(5, question.BadPoints);
    }

    [Fact]
    public void Update_KeepsOriginalValuesWhenRejected()
    {
        var question = new Question(ValidText, "2022", "History", 3);

        Assert.Throws<DomainException>(() => question.Update(ValidText, "not a number", "History", 3));
        Assert.Equal("2022", question.Answer);
    }
}
