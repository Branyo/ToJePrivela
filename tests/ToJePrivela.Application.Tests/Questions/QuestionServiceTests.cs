using NSubstitute;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Application.Common;
using ToJePrivela.Application.Questions;
using ToJePrivela.Application.Questions.Dtos;
using ToJePrivela.Application.Tests.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Tests.Questions;

public class QuestionServiceTests
{
    private const string ValidText = "Which year was ChatGPT publicly released?";
    private const int PickedBadPoints = 2;

    private static readonly DateTimeOffset Now = new(2026, 9, 24, 12, 30, 0, TimeSpan.Zero);

    private readonly QuestionCategory _history = TestEntities.Category(3, "History");
    private readonly QuestionCategory _sport = TestEntities.Category(2, "Sport");
    private readonly IQuestionRepository _questions = Substitute.For<IQuestionRepository>();
    private readonly IQuestionCategoryRepository _categories = Substitute.For<IQuestionCategoryRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly QuestionService _sut;

    public QuestionServiceTests()
    {
        _categories.GetByIdAsync(3, Arg.Any<CancellationToken>()).Returns(_history);
        _categories.GetByIdAsync(2, Arg.Any<CancellationToken>()).Returns(_sport);

        _sut = new QuestionService(
            _questions,
            _categories,
            new FixedBadPointsPicker(PickedBadPoints),
            new FixedTimeProvider(Now),
            _unitOfWork);
    }

    [Fact]
    public async Task GetAsync_PassesTheFilterToTheRepository()
    {
        _questions.FindAsync(3, QuestionSource.Ai, Arg.Any<CancellationToken>())
            .Returns([TestEntities.Question(1, ValidText, "2022", _history, 4, QuestionSource.Ai)]);

        var result = await _sut.GetAsync(new QuestionFilter { CategoryId = 3, Source = QuestionSource.Ai });

        Assert.True(result.IsSuccess);
        var question = Assert.Single(result.Value);
        Assert.Equal(3, question.CategoryId);
        Assert.Equal("History", question.CategoryName);
        Assert.Equal("Ai", question.Source);
        Assert.Equal(4, question.BadPoints);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNotFoundForUnknownQuestion()
    {
        _questions.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns((Question?)null);

        var result = await _sut.GetByIdAsync(7);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task CreateAsync_StoresAManualQuestionWithTheGivenBadPoints()
    {
        var result = await _sut.CreateAsync(new CreateQuestionRequest
        {
            Text = ValidText,
            Answer = "2022",
            CategoryId = 3,
            BadPoints = 5
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("2022", result.Value.Answer);
        Assert.Equal("History", result.Value.CategoryName);
        Assert.Equal(5, result.Value.BadPoints);
        Assert.Equal("Manual", result.Value.Source);
        Assert.Equal(Now.UtcDateTime, result.Value.CreatedAt);
        await _questions.Received(1).AddAsync(Arg.Any<Question>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_PicksBadPointsWhenNoneAreGiven()
    {
        var result = await _sut.CreateAsync(new CreateQuestionRequest { Text = ValidText, Answer = "2022", CategoryId = 3 });

        Assert.Equal(PickedBadPoints, result.Value.BadPoints);
    }

    [Fact]
    public async Task CreateAsync_RejectsAnUnknownCategory()
    {
        var result = await _sut.CreateAsync(new CreateQuestionRequest { Text = ValidText, Answer = "2022", CategoryId = 99 });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        await _questions.DidNotReceive().AddAsync(Arg.Any<Question>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ChangesTheStoredQuestion()
    {
        var question = TestEntities.Question(1, ValidText, "2022", _history, 3);
        _questions.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(question);

        var result = await _sut.UpdateAsync(1, new UpdateQuestionRequest
        {
            Text = "How many players are on a football pitch?",
            Answer = "11",
            CategoryId = 2,
            BadPoints = 1
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("11", question.Answer);
        Assert.Same(_sport, question.Category);
        Assert.Equal(1, question.BadPoints);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_KeepsBadPointsWhenNoneAreGiven()
    {
        var question = TestEntities.Question(1, ValidText, "2022", _history, 4);
        _questions.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(question);

        await _sut.UpdateAsync(1, new UpdateQuestionRequest { Text = ValidText, Answer = "2023", CategoryId = 3 });

        Assert.Equal(4, question.BadPoints);
    }

    [Fact]
    public async Task UpdateAsync_TurnsAnEditedAiQuestionIntoAManualOne()
    {
        var question = TestEntities.Question(1, ValidText, "2022", _history, 4, QuestionSource.Ai);
        _questions.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(question);

        await _sut.UpdateAsync(1, new UpdateQuestionRequest { Text = ValidText, Answer = "2022", CategoryId = 3 });

        Assert.Equal(QuestionSource.Manual, question.Source);
    }

    [Fact]
    public async Task UpdateAsync_RejectsAnUnknownCategory()
    {
        var question = TestEntities.Question(1, ValidText, "2022", _history, 3);
        _questions.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(question);

        var result = await _sut.UpdateAsync(1, new UpdateQuestionRequest { Text = ValidText, Answer = "2022", CategoryId = 99 });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Same(_history, question.Category);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNotFoundForUnknownQuestion()
    {
        _questions.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns((Question?)null);

        var result = await _sut.UpdateAsync(7, new UpdateQuestionRequest { Text = ValidText, Answer = "2022", CategoryId = 3 });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheQuestion()
    {
        var question = TestEntities.Question(1, ValidText, "2022", _history, 3);
        _questions.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(question);

        var result = await _sut.DeleteAsync(1);

        Assert.True(result.IsSuccess);
        _questions.Received(1).Remove(question);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsNotFoundForUnknownQuestion()
    {
        _questions.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns((Question?)null);

        var result = await _sut.DeleteAsync(7);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }
}
