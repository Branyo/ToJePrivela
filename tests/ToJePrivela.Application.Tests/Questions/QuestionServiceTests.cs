using NSubstitute;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Application.Common;
using ToJePrivela.Application.Questions;
using ToJePrivela.Application.Questions.Dtos;
using ToJePrivela.Application.Tests.Common;
using ToJePrivela.Domain.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Tests.Questions;

public class QuestionServiceTests
{
    private const string ValidText = "Which year was ChatGPT publicly released?";
    private const string ValidTextSk = "V ktorom roku bol verejne spustený ChatGPT?";
    private const int PickedBadPoints = 2;
    private const int TestAccountId = 7;

    private static readonly DateTimeOffset Now = new(2026, 9, 24, 12, 30, 0, TimeSpan.Zero);

    private readonly QuestionCategory _history = TestEntities.Category(3, "História", "History");
    private readonly QuestionCategory _sport = TestEntities.Category(2, "Šport", "Sport");
    private readonly FixedCurrentLanguage _language = new();
    private readonly IQuestionRepository _questions = Substitute.For<IQuestionRepository>();
    private readonly IQuestionCategoryRepository _categories = Substitute.For<IQuestionCategoryRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly QuestionService _sut;

    public QuestionServiceTests()
    {
        _categories.GetByIdAsync(3, Arg.Any<CancellationToken>()).Returns(_history);
        _categories.GetByIdAsync(2, Arg.Any<CancellationToken>()).Returns(_sport);
        _categories.GetMissingIdsAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<IReadOnlyCollection<int>>().Where(id => id is not (2 or 3)).ToList());

        _sut = new QuestionService(
            _questions,
            _categories,
            new FixedBadPointsPicker(PickedBadPoints),
            new FixedTimeProvider(Now),
            _language,
            new FixedCurrentAccount(TestAccountId),
            _unitOfWork);
    }

    [Theory]
    [InlineData(Language.Sk, "História")]
    [InlineData(Language.En, "History")]
    public async Task GetByIdAsync_NamesTheCategoryInTheRequestedLanguage(Language language, string categoryName)
    {
        _language.Language = language;
        _questions.GetByIdAsync(8, Arg.Any<CancellationToken>())
            .Returns(TestEntities.Question(8, ValidText, "2022", _history));

        var result = await _sut.GetByIdAsync(8);

        Assert.Equal(categoryName, result.Value.CategoryName);
    }

    [Fact]
    public async Task GetRandomAsync_DrawsFromTheSelectedCategoriesEachOnce()
    {
        _questions.GetRandomLeastViewedAsync(
                TestAccountId,
                Arg.Is<IReadOnlyCollection<int>>(ids => ids.SequenceEqual(new[] { 3, 2 })),
                Arg.Any<CancellationToken>())
            .Returns(TestEntities.Question(8, ValidText, "2022", _history));

        var result = await _sut.GetRandomAsync(new RandomQuestionFilter { CategoryIds = [3, 2, 3] });

        Assert.True(result.IsSuccess);
        Assert.Equal(8, result.Value.Id);
    }

    [Fact]
    public async Task GetRandomAsync_RejectsAMissingFilter()
    {
        var result = await _sut.GetRandomAsync(null!);

        Assert.True(result.IsFailure);
        Assert.Equal("Request.Missing", result.Error.Code);
    }

    [Fact]
    public async Task GetRandomAsync_DoesNotCountAView()
    {
        var question = TestEntities.Question(8, ValidText, "2022", _history);
        _questions.GetRandomLeastViewedAsync(TestAccountId, Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>()).Returns(question);

        await _sut.GetRandomAsync(new RandomQuestionFilter { CategoryIds = [3] });

        await _questions.DidNotReceive().RecordViewAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRandomAsync_WithoutCategoriesChoosesFromAllOfThem()
    {
        _questions.GetRandomLeastViewedAsync(TestAccountId, Arg.Is<IReadOnlyCollection<int>>(ids => ids.Count == 0), Arg.Any<CancellationToken>())
            .Returns(TestEntities.Question(5, ValidText, "2022", _sport));

        var result = await _sut.GetRandomAsync(new RandomQuestionFilter());

        Assert.Equal(5, result.Value.Id);
        await _categories.DidNotReceive().GetMissingIdsAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRandomAsync_RejectsNonPositiveCategoryIds()
    {
        var result = await _sut.GetRandomAsync(new RandomQuestionFilter { CategoryIds = [3, 0, -1] });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Question.InvalidCategoryIds", result.Error.Code);
    }

    [Fact]
    public async Task GetRandomAsync_RejectsUnknownCategories()
    {
        var result = await _sut.GetRandomAsync(new RandomQuestionFilter { CategoryIds = [3, 99, 98] });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Question.UnknownCategories", result.Error.Code);
        Assert.Contains("99", result.Error.Message);
        Assert.Contains("98", result.Error.Message);
        await _questions.DidNotReceive().GetRandomLeastViewedAsync(Arg.Any<int>(), Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRandomAsync_ReturnsNotFoundWhenTheCategoriesHaveNoQuestions()
    {
        _questions.GetRandomLeastViewedAsync(TestAccountId, Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns((Question?)null);

        var result = await _sut.GetRandomAsync(new RandomQuestionFilter { CategoryIds = [2] });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Question.NoneAvailable", result.Error.Code);
    }

    [Fact]
    public async Task RecordViewAsync_CountsTheViewForTheSignedInAccountAtOnce()
    {
        _questions.RecordViewAsync(TestAccountId, 1, Now.UtcDateTime, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.RecordViewAsync(1);

        Assert.True(result.IsSuccess);
        await _questions.Received(1).RecordViewAsync(TestAccountId, 1, Now.UtcDateTime, Arg.Any<CancellationToken>());
        // One atomic statement: nothing waits in the unit of work, so nothing can conflict there.
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecordViewAsync_ReturnsNotFoundForUnknownQuestion()
    {
        _questions.RecordViewAsync(TestAccountId, 7, Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.RecordViewAsync(7);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Question.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task GetAsync_RequiresACategory()
    {
        var result = await _sut.GetAsync(new QuestionFilter { Source = QuestionSource.Ai });

        Assert.True(result.IsFailure);
        Assert.Equal("Request.Invalid", result.Error.Code);
        await _questions.DidNotReceive().FindAsync(Arg.Any<int>(), Arg.Any<QuestionSource?>(), Arg.Any<CancellationToken>());
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
        Assert.Equal("História", question.CategoryName);
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
            TextSk = ValidTextSk,
            TextEn = ValidText,
            Answer = "2022",
            CategoryId = 3,
            BadPoints = 5
        });

        Assert.True(result.IsSuccess);
        Assert.Equal((ValidTextSk, ValidTextSk, ValidText), (result.Value.Text, result.Value.TextSk, result.Value.TextEn));
        Assert.Equal("2022", result.Value.Answer);
        Assert.Equal("História", result.Value.CategoryName);
        Assert.Equal(5, result.Value.BadPoints);
        Assert.Equal("Manual", result.Value.Source);
        Assert.Equal(Now.UtcDateTime, result.Value.CreatedAt);
        await _questions.Received(1).AddAsync(Arg.Any<Question>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_RequiresTheEnglishText()
    {
        var result = await _sut.CreateAsync(new CreateQuestionRequest { TextSk = ValidTextSk, Answer = "2022", CategoryId = 3 });

        Assert.Equal("Request.Invalid", result.Error.Code);
        await _questions.DidNotReceive().AddAsync(Arg.Any<Question>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_ReturnsTheQuestionInTheRequestedLanguage()
    {
        _language.Language = Language.En;

        var result = await _sut.CreateAsync(new CreateQuestionRequest { TextSk = ValidTextSk, TextEn = ValidText, Answer = "2022", CategoryId = 3 });

        Assert.Equal((ValidText, "History"), (result.Value.Text, result.Value.CategoryName));
    }

    [Fact]
    public async Task CreateAsync_PicksBadPointsWhenNoneAreGiven()
    {
        var result = await _sut.CreateAsync(new CreateQuestionRequest { TextSk = ValidTextSk, TextEn = ValidText, Answer = "2022", CategoryId = 3 });

        Assert.Equal(PickedBadPoints, result.Value.BadPoints);
    }

    [Fact]
    public async Task CreateAsync_RejectsAnUnknownCategory()
    {
        var result = await _sut.CreateAsync(new CreateQuestionRequest { TextSk = ValidTextSk, TextEn = ValidText, Answer = "2022", CategoryId = 99 });

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
            TextSk = "Koľko hráčov je na futbalovom ihrisku?",
            TextEn = "How many players are on a football pitch?",
            Answer = "11",
            CategoryId = 2,
            BadPoints = 1
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(("Koľko hráčov je na futbalovom ihrisku?", "How many players are on a football pitch?"), (question.TextSk, question.TextEn));
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

        await _sut.UpdateAsync(1, new UpdateQuestionRequest { TextSk = ValidTextSk, Answer = "2023", CategoryId = 3 });

        Assert.Equal(4, question.BadPoints);
    }

    [Fact]
    public async Task UpdateAsync_KeepsTheEnglishTextWhenNoneIsGiven()
    {
        var question = TestEntities.Question(1, ValidTextSk, "2022", _history, 4, textEn: ValidText);
        _questions.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(question);

        await _sut.UpdateAsync(1, new UpdateQuestionRequest { TextSk = "V ktorom roku spustili ChatGPT?", Answer = "2022", CategoryId = 3 });

        Assert.Equal(("V ktorom roku spustili ChatGPT?", ValidText), (question.TextSk, question.TextEn));
    }

    [Fact]
    public async Task UpdateAsync_LeavesAQuestionWithoutEnglishTextAsItIsWhenNoneIsGiven()
    {
        var question = TestEntities.WithoutEnglish(TestEntities.Question(1, ValidTextSk, "2022", _history, 4));
        _questions.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(question);

        var result = await _sut.UpdateAsync(1, new UpdateQuestionRequest { TextSk = ValidTextSk, Answer = "2023", CategoryId = 3 });

        Assert.True(result.IsSuccess);
        Assert.Null(question.TextEn);
        Assert.Equal("2023", question.Answer);
    }

    [Fact]
    public async Task UpdateAsync_TurnsAnEditedAiQuestionIntoAManualOne()
    {
        var question = TestEntities.Question(1, ValidText, "2022", _history, 4, QuestionSource.Ai);
        _questions.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(question);

        await _sut.UpdateAsync(1, new UpdateQuestionRequest { TextSk = ValidTextSk, Answer = "2022", CategoryId = 3 });

        Assert.Equal(QuestionSource.Manual, question.Source);
    }

    [Fact]
    public async Task UpdateAsync_RejectsAnUnknownCategory()
    {
        var question = TestEntities.Question(1, ValidText, "2022", _history, 3);
        _questions.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(question);

        var result = await _sut.UpdateAsync(1, new UpdateQuestionRequest { TextSk = ValidTextSk, Answer = "2022", CategoryId = 99 });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Same(_history, question.Category);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNotFoundForUnknownQuestion()
    {
        _questions.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns((Question?)null);

        var result = await _sut.UpdateAsync(7, new UpdateQuestionRequest { TextSk = ValidTextSk, Answer = "2022", CategoryId = 3 });

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
