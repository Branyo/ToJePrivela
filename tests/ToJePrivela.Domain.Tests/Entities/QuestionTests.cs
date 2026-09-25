using ToJePrivela.Domain.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Domain.Tests.Entities;

public class QuestionTests
{
    private const string ValidText = "Which year was ChatGPT publicly released?";

    private static readonly DateTime CreatedAt = new(2026, 9, 24, 10, 0, 0, DateTimeKind.Utc);
    private static readonly QuestionCategory History = new("History");
    private static readonly QuestionCategory Sport = new("Sport");

    [Fact]
    public void Constructor_KeepsEveryValue()
    {
        var question = new Question(ValidText, "2022", History, 4, QuestionSource.Ai, CreatedAt);

        Assert.Equal(ValidText, question.Text);
        Assert.Equal("2022", question.Answer);
        Assert.Same(History, question.Category);
        Assert.Equal(4, question.BadPoints);
        Assert.Equal(QuestionSource.Ai, question.Source);
        Assert.Equal(CreatedAt, question.CreatedAt);
        Assert.Equal(0, question.ViewCount);
        Assert.Null(question.LastViewedAt);
    }

    [Fact]
    public void MarkViewed_CountsTheViewAndRemembersWhen()
    {
        var question = Create();
        var firstView = CreatedAt.AddHours(1);
        var secondView = CreatedAt.AddHours(2);

        question.MarkViewed(firstView);
        question.MarkViewed(secondView);

        Assert.Equal(2, question.ViewCount);
        Assert.Equal(secondView, question.LastViewedAt);
    }

    [Fact]
    public void EveryChangeBumpsTheVersion()
    {
        var question = Create();
        var initial = question.Version;

        question.MarkViewed(CreatedAt);
        var afterView = question.Version;
        question.Update(ValidText, "2023", History, 3);

        Assert.True(afterView > initial);
        Assert.True(question.Version > afterView);
    }

    [Fact]
    public void RejectedUpdateKeepsTheVersion()
    {
        var question = Create();
        var initial = question.Version;

        Assert.Throws<DomainException>(() => question.Update(ValidText, "not a number", History, 3));
        Assert.Equal(initial, question.Version);
    }

    [Theory]
    [InlineData("2022")]
    [InlineData("-5")]
    [InlineData("3.14")]
    [InlineData(" 42 ")]
    public void Constructor_AcceptsNumericAnswers(string answer)
    {
        var question = Create(answer: answer);

        Assert.Equal(answer.Trim(), question.Answer);
    }

    [Theory]
    [InlineData("two thousand")]
    [InlineData("2022 AD")]
    [InlineData("")]
    public void Constructor_RejectsNonNumericAnswers(string answer)
    {
        Assert.Throws<DomainException>(() => Create(answer: answer));
    }

    [Theory]
    [InlineData(Question.MinBadPoints)]
    [InlineData(Question.MaxBadPoints)]
    public void Constructor_AcceptsBadPointsWithinRange(int badPoints)
    {
        Assert.Equal(badPoints, Create(badPoints: badPoints).BadPoints);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Constructor_RejectsBadPointsOutOfRange(int badPoints)
    {
        Assert.Throws<DomainException>(() => Create(badPoints: badPoints));
    }

    [Fact]
    public void Constructor_RejectsTooShortText()
    {
        Assert.Throws<DomainException>(() => Create(text: "Short"));
    }

    [Fact]
    public void Constructor_RejectsTooLongText()
    {
        Assert.Throws<DomainException>(() => Create(text: new string('x', Question.TextMaxLength + 1)));
    }

    [Fact]
    public void Constructor_RequiresACategory()
    {
        Assert.Throws<ArgumentNullException>(
            () => new Question(ValidText, "2022", null!, 3, QuestionSource.Manual, CreatedAt));
    }

    [Fact]
    public void Update_ReplacesEveryEditableValue()
    {
        var question = Create();

        question.Update("How many players are on a football pitch?", "11", Sport, 1);

        Assert.Equal("How many players are on a football pitch?", question.Text);
        Assert.Equal("11", question.Answer);
        Assert.Same(Sport, question.Category);
        Assert.Equal(1, question.BadPoints);
        Assert.Equal(CreatedAt, question.CreatedAt);
    }

    [Fact]
    public void Update_TurnsAnAiQuestionIntoAManualOne()
    {
        var question = Create(source: QuestionSource.Ai);

        question.Update(ValidText, "2022", History, 3);

        Assert.Equal(QuestionSource.Manual, question.Source);
    }

    [Fact]
    public void Update_KeepsOriginalValuesWhenRejected()
    {
        var question = Create(source: QuestionSource.Ai);

        Assert.Throws<DomainException>(() => question.Update(ValidText, "not a number", Sport, 3));
        Assert.Equal("2022", question.Answer);
        Assert.Same(History, question.Category);
        Assert.Equal(QuestionSource.Ai, question.Source);
    }

    [Fact]
    public void Update_RejectsBadPointsOutOfRange()
    {
        var question = Create();

        Assert.Throws<DomainException>(() => question.Update(ValidText, "2022", History, 9));
        Assert.Equal(3, question.BadPoints);
    }

    private static Question Create(
        string text = ValidText,
        string answer = "2022",
        int badPoints = 3,
        QuestionSource source = QuestionSource.Manual) =>
        new(text, answer, History, badPoints, source, CreatedAt);
}
