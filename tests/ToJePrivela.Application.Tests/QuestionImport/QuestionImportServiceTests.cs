using System.Text.Json;
using NSubstitute;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Application.Common;
using ToJePrivela.Application.QuestionGeneration;
using ToJePrivela.Application.QuestionImport;
using ToJePrivela.Application.QuestionImport.Dtos;
using ToJePrivela.Application.Tests.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Tests.QuestionImport;

public class QuestionImportServiceTests
{
    private const string TextSk = "Koľko hráčov je na futbalovom ihrisku?";
    private const string TextEn = "How many players are on a football pitch?";

    private static readonly DateTimeOffset Now = new(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);

    private readonly IQuestionCategoryRepository _categories = Substitute.For<IQuestionCategoryRepository>();
    private readonly IQuestionRepository _questions = Substitute.For<IQuestionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly QuestionCategory _category = TestEntities.Category(4, "Šport", "Sport");
    private readonly QuestionImportService _sut;

    public QuestionImportServiceTests()
    {
        _categories.GetByIdAsync(4, Arg.Any<CancellationToken>()).Returns(_category);
        _questions.GetTextsAsync(4, Arg.Any<CancellationToken>()).Returns([]);

        _sut = new QuestionImportService(
            _categories,
            _questions,
            new QuestionImporter(_questions, new FixedBadPointsPicker(3), new FixedTimeProvider(Now)),
            new FixedCurrentLanguage(),
            _unitOfWork);
    }

    [Fact]
    public async Task ImportAsync_StoresTheQuestionsAsAiQuestionsInOneSave()
    {
        var result = await _sut.ImportAsync(4, Request(
            Item(TextSk, TextEn, "22"),
            Item("V ktorom roku vznikla OSN?", "Which year was the UN founded?", "1945")));

        Assert.True(result.IsSuccess);
        Assert.Equal((2, 2), (result.Value.Received, result.Value.Imported));
        Assert.Empty(result.Value.Rejected);
        await _questions.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<Question>>(questions => questions.All(q =>
                q.Category == _category && q.Source == QuestionSource.Ai && q.BadPoints == 3 && q.CreatedAt == Now.UtcDateTime)),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        Assert.Equal([TextSk, "V ktorom roku vznikla OSN?"], result.Value.Questions.Select(q => q.TextSk));
    }

    [Fact]
    public async Task ImportAsync_ReportsEveryItemItLeavesOutByItsPosition()
    {
        var result = await _sut.ImportAsync(4, Request(
            Item(TextSk, TextEn, "22"),
            "\"not a question\"",
            Item("Krátka", TextEn, "22"),
            Item("Koľko nôh má pavúk?", "How many legs does a spider have?", "eight"),
            Item("Koľko hviezd má naša galaxia?", "How many stars are in our galaxy?", "5000000000000"),
            Item("V roku 1969 pristálo Apollo 11 na Mesiaci, v ktorom roku?", "Which year did Apollo 11 land?", "1969"),
            Item(" koľko HRÁČOV je na futbalovom ihrisku ", "How many football players are there?", "22")));

        Assert.Equal((7, 1), (result.Value.Received, result.Value.Imported));
        Assert.Equal(
            [
                new RejectedQuestionDto(2, QuestionRejection.Malformed),
                new RejectedQuestionDto(3, QuestionRejection.InvalidText),
                new RejectedQuestionDto(4, QuestionRejection.InvalidAnswer),
                new RejectedQuestionDto(5, QuestionRejection.AnswerTooLarge),
                new RejectedQuestionDto(6, QuestionRejection.StatesAnswer),
                new RejectedQuestionDto(7, QuestionRejection.Duplicate),
            ],
            result.Value.Rejected);
    }

    [Fact]
    public async Task ImportAsync_LeavesOutQuestionsTheCategoryAlreadyHasAndSavesNothingThen()
    {
        _questions.GetTextsAsync(4, Arg.Any<CancellationToken>()).Returns([TextSk + "?"]);

        var result = await _sut.ImportAsync(4, Request(Item(TextSk, TextEn, "22")));

        Assert.Equal(new RejectedQuestionDto(1, QuestionRejection.Duplicate), Assert.Single(result.Value.Rejected));
        Assert.Equal(0, result.Value.Imported);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("not json", "QuestionImport.InvalidJson")]
    [InlineData("[{\"questionSk\": 1,]", "QuestionImport.InvalidJson")]
    [InlineData("[]", "QuestionImport.Empty")]
    public async Task ImportAsync_RejectsTextWithoutQuestions(string json, string code)
    {
        var result = await _sut.ImportAsync(4, new ImportQuestionsRequest { Json = json });

        Assert.Equal((ErrorType.Validation, code), (result.Error.Type, result.Error.Code));
        await _questions.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<Question>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ImportAsync_RejectsMoreItemsThanOneImportTakes()
    {
        var items = Enumerable.Repeat(Item(TextSk, TextEn, "22"), QuestionImporter.MaxQuestions + 1).ToArray();

        var result = await _sut.ImportAsync(4, Request(items));

        Assert.Equal("QuestionImport.TooMany", result.Error.Code);
    }

    [Fact]
    public async Task ImportAsync_RequiresTheJsonText()
    {
        var result = await _sut.ImportAsync(4, new ImportQuestionsRequest());

        Assert.Equal("Request.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task ImportAsync_ReturnsNotFoundForUnknownCategory()
    {
        var result = await _sut.ImportAsync(9, Request(Item(TextSk, TextEn, "22")));

        Assert.Equal((ErrorType.NotFound, "QuestionCategory.NotFound"), (result.Error.Type, result.Error.Code));
    }

    private static ImportQuestionsRequest Request(params string[] items) => new() { Json = $"[{string.Join(',', items)}]" };

    private static string Item(string textSk, string textEn, string answer) =>
        JsonSerializer.Serialize(new { questionSk = textSk, questionEn = textEn, answer });
}
