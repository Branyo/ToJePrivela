using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Application.Common;
using ToJePrivela.Application.QuestionCategories;
using ToJePrivela.Application.QuestionCategories.Dtos;
using ToJePrivela.Application.QuestionGeneration;
using ToJePrivela.Application.Tests.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Tests.QuestionCategories;

public class QuestionCategoryServiceTests
{
    private const string ValidText = "How many players are on a football pitch?";

    private readonly IQuestionCategoryRepository _categories = Substitute.For<IQuestionCategoryRepository>();
    private readonly IQuestionRepository _questions = Substitute.For<IQuestionRepository>();
    private readonly IPlayerRepository _players = Substitute.For<IPlayerRepository>();
    private readonly IQuestionGenerationService _generation = Substitute.For<IQuestionGenerationService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly QuestionCategoryService _sut;

    public QuestionCategoryServiceTests()
    {
        _categories.GetByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((QuestionCategory?)null);
        _generation.GenerateAsync(Arg.Any<QuestionCategory>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call => Generated(call.Arg<QuestionCategory>(), call.Arg<int>(), created: call.Arg<int>()));

        _sut = new QuestionCategoryService(_categories, _questions, _players, _generation, _unitOfWork);
    }

    [Fact]
    public async Task GetAllAsync_MapsEveryCategory()
    {
        _categories.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns([TestEntities.Category(1, "Sport"), TestEntities.Category(2, "History")]);

        var result = await _sut.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(["Sport", "History"], result.Value.Select(c => c.Name));
        Assert.All(result.Value, c => Assert.Null(c.AddedByPlayer));
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNotFoundForUnknownCategory()
    {
        _categories.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns((QuestionCategory?)null);

        var result = await _sut.GetByIdAsync(7);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task CreateAsync_StoresTheCategoryWithItsGeneratedQuestionsInOneSave()
    {
        _players.ExistsAsync(1, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.CreateAsync(new CreateQuestionCategoryRequest { Name = "Sport", AddedByPlayerId = 1, QuestionCount = 3 });

        Assert.True(result.IsSuccess);
        Assert.Equal("Sport", result.Value.Name);
        Assert.Equal(new(3, 3, 0), result.Value.QuestionGeneration);
        await _categories.Received(1).AddAsync(Arg.Is<QuestionCategory>(c => c.Name == "Sport"), Arg.Any<CancellationToken>());
        await _questions.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<Question>>(questions => questions.Count() == 3),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithZeroQuestionsStoresJustTheCategory()
    {
        var result = await _sut.CreateAsync(new CreateQuestionCategoryRequest { Name = "Sport", QuestionCount = 0 });

        Assert.True(result.IsSuccess);
        Assert.Equal(new(0, 0, 0), result.Value.QuestionGeneration);
        await _categories.Received(1).AddAsync(Arg.Any<QuestionCategory>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_ReportsAPartialGeneration()
    {
        _generation.GenerateAsync(Arg.Any<QuestionCategory>(), 10, Arg.Any<CancellationToken>())
            .Returns(call => Generated(call.Arg<QuestionCategory>(), 10, created: 6, discarded: 2));

        var result = await _sut.CreateAsync(new CreateQuestionCategoryRequest { Name = "Sport", QuestionCount = 10 });

        Assert.True(result.IsSuccess);
        Assert.Equal(new(10, 6, 2), result.Value.QuestionGeneration);
    }

    [Fact]
    public async Task CreateAsync_StoresNothingWhenGenerationFails()
    {
        _generation.GenerateAsync(Arg.Any<QuestionCategory>(), 5, Arg.Any<CancellationToken>())
            .Returns(call => Generated(call.Arg<QuestionCategory>(), 5, created: 0));

        var result = await _sut.CreateAsync(new CreateQuestionCategoryRequest { Name = "Sport", QuestionCount = 5 });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unavailable, result.Error.Type);
        await _categories.DidNotReceive().AddAsync(Arg.Any<QuestionCategory>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_RejectsDuplicateNameBeforeGenerating()
    {
        _categories.GetByNameAsync("Sport", Arg.Any<CancellationToken>()).Returns(TestEntities.Category(1, "Sport"));

        var result = await _sut.CreateAsync(new CreateQuestionCategoryRequest { Name = "Sport", QuestionCount = 5 });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        await _generation.DidNotReceive().GenerateAsync(Arg.Any<QuestionCategory>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _categories.DidNotReceive().AddAsync(Arg.Any<QuestionCategory>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_RejectsUnknownAuthorBeforeGenerating()
    {
        _players.ExistsAsync(99, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.CreateAsync(new CreateQuestionCategoryRequest { Name = "Sport", AddedByPlayerId = 99, QuestionCount = 5 });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        await _generation.DidNotReceive().GenerateAsync(Arg.Any<QuestionCategory>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_ReportsAConflictWhenTheNameWasTakenDuringGeneration()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new UniqueConstraintException("duplicate", new InvalidOperationException()));

        var result = await _sut.CreateAsync(new CreateQuestionCategoryRequest { Name = "Sport", QuestionCount = 2 });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public async Task CreateAsync_AllowsMissingAuthor()
    {
        var result = await _sut.CreateAsync(new CreateQuestionCategoryRequest { Name = "Sport", QuestionCount = 0 });

        Assert.True(result.IsSuccess);
        await _players.DidNotReceive().ExistsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheCategory()
    {
        var category = TestEntities.Category(1, "Sport");
        _categories.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(category);

        var result = await _sut.DeleteAsync(1);

        Assert.True(result.IsSuccess);
        _categories.Received(1).Remove(category);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ReturnsNotFoundForUnknownCategory()
    {
        _categories.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns((QuestionCategory?)null);

        var result = await _sut.DeleteAsync(7);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task GenerateAiQuestionsAsync_StoresTheQuestionsAndReturnsThem()
    {
        var category = TestEntities.Category(2, "Sport");
        _categories.GetByIdAsync(2, Arg.Any<CancellationToken>()).Returns(category);

        var result = await _sut.GenerateAiQuestionsAsync(2, new GenerateAiQuestionsRequest { Count = 4 });

        Assert.True(result.IsSuccess);
        Assert.Equal(new(4, 4, 0), result.Value.Summary);
        Assert.Equal(4, result.Value.Questions.Count);
        Assert.All(result.Value.Questions, question =>
        {
            Assert.Equal("Sport", question.CategoryName);
            Assert.Equal("Ai", question.Source);
        });
        await _generation.Received(1).GenerateAsync(category, 4, Arg.Any<CancellationToken>());
        await _questions.Received(1).AddRangeAsync(Arg.Any<IEnumerable<Question>>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAiQuestionsAsync_ReportsUnavailableWhenNothingComesBack()
    {
        _categories.GetByIdAsync(2, Arg.Any<CancellationToken>()).Returns(TestEntities.Category(2, "Sport"));
        _generation.GenerateAsync(Arg.Any<QuestionCategory>(), 4, Arg.Any<CancellationToken>())
            .Returns(call => Generated(call.Arg<QuestionCategory>(), 4, created: 0));

        var result = await _sut.GenerateAiQuestionsAsync(2, new GenerateAiQuestionsRequest { Count = 4 });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unavailable, result.Error.Type);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAiQuestionsAsync_ReturnsNotFoundForUnknownCategory()
    {
        _categories.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns((QuestionCategory?)null);

        var result = await _sut.GenerateAiQuestionsAsync(7, new GenerateAiQuestionsRequest { Count = 4 });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        await _generation.DidNotReceive().GenerateAsync(Arg.Any<QuestionCategory>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAiQuestionsAsync_RemovesOnlyTheAiQuestionsOfTheCategory()
    {
        var category = TestEntities.Category(2, "Sport");
        IReadOnlyList<Question> aiQuestions =
        [
            TestEntities.Question(1, ValidText, "22", category, source: QuestionSource.Ai),
            TestEntities.Question(2, "How long is a marathon in metres?", "42195", category, source: QuestionSource.Ai)
        ];
        _categories.ExistsAsync(2, Arg.Any<CancellationToken>()).Returns(true);
        _questions.FindAsync(2, QuestionSource.Ai, Arg.Any<CancellationToken>()).Returns(aiQuestions);

        var result = await _sut.DeleteAiQuestionsAsync(2);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Deleted);
        _questions.Received(1).RemoveRange(aiQuestions);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAiQuestionsAsync_ReturnsNotFoundForUnknownCategory()
    {
        _categories.ExistsAsync(7, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.DeleteAiQuestionsAsync(7);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    private static QuestionGenerationResult Generated(QuestionCategory category, int requested, int created, int discarded = 0) =>
        new(
            Enumerable.Range(1, created)
                .Select(n => new Question($"Generated question number {n}?", n.ToString(), category, 3, QuestionSource.Ai, TestEntities.CreatedAt))
                .ToList(),
            requested,
            discarded);
}
