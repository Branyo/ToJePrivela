using NSubstitute;
using ToJePrivela.Application.Abstractions.Ai;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Application.Common;
using ToJePrivela.Application.Questions;
using ToJePrivela.Application.Questions.Dtos;
using ToJePrivela.Application.Tests.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Tests.Questions;

public class QuestionServiceGenerationTests
{
    private readonly IQuestionRepository _questions = Substitute.For<IQuestionRepository>();
    private readonly IQuestionCategoryRepository _categories = Substitute.For<IQuestionCategoryRepository>();
    private readonly IQuestionGenerator _generator = Substitute.For<IQuestionGenerator>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly QuestionService _sut;

    public QuestionServiceGenerationTests()
    {
        _sut = new QuestionService(_questions, _categories, _generator, _unitOfWork);
    }

    [Fact]
    public async Task GenerateAsync_RejectsUnknownCategory()
    {
        _categories.GetByNameAsync("Aliens", Arg.Any<CancellationToken>()).Returns((QuestionCategory?)null);

        var result = await _sut.GenerateAsync(new GenerateQuestionsRequest { Category = "Aliens", Count = 5 });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        await _generator.DidNotReceive().GenerateAsync(Arg.Any<QuestionGenerationRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_MapsGeneratedQuestions()
    {
        _categories.GetByNameAsync("Sport", Arg.Any<CancellationToken>())
            .Returns(TestEntities.Category(1, "Sport"));

        _generator.GenerateAsync(Arg.Any<QuestionGenerationRequest>(), Arg.Any<CancellationToken>())
            .Returns([new GeneratedQuestion("How many players are on a football pitch?", "11", "Sport", 2)]);

        var result = await _sut.GenerateAsync(new GenerateQuestionsRequest { Category = "Sport", Count = 1 });

        Assert.True(result.IsSuccess);
        var question = Assert.Single(result.Value);
        Assert.Equal("11", question.Answer);
        Assert.Equal("Sport", question.Category);
        Assert.Equal(4, question.BadPoints);
    }

    [Fact]
    public async Task GenerateAsync_PassesTheRequestedCountAndLanguage()
    {
        _categories.GetByNameAsync("Sport", Arg.Any<CancellationToken>())
            .Returns(TestEntities.Category(1, "Sport"));

        _generator.GenerateAsync(Arg.Any<QuestionGenerationRequest>(), Arg.Any<CancellationToken>())
            .Returns([new GeneratedQuestion("How many players are on a football pitch?", "11", "Sport", 3)]);

        await _sut.GenerateAsync(new GenerateQuestionsRequest { Category = "Sport", Count = 7, Language = "English" });

        await _generator.Received(1).GenerateAsync(
            Arg.Is<QuestionGenerationRequest>(r => r.Category == "Sport" && r.Count == 7 && r.Language == "English"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_FallsBackToARandomCategoryWhenNoneIsGiven()
    {
        _categories.GetRandomAsync(Arg.Any<CancellationToken>()).Returns(TestEntities.Category(2, "History"));

        _generator.GenerateAsync(Arg.Any<QuestionGenerationRequest>(), Arg.Any<CancellationToken>())
            .Returns([new GeneratedQuestion("Which year was ChatGPT publicly released?", "2022", "History", 3)]);

        var result = await _sut.GenerateAsync(new GenerateQuestionsRequest { Count = 1 });

        Assert.True(result.IsSuccess);
        await _generator.Received(1).GenerateAsync(
            Arg.Is<QuestionGenerationRequest>(r => r.Category == "History"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_ReportsUnavailableWhenNothingComesBack()
    {
        _categories.GetByNameAsync("Sport", Arg.Any<CancellationToken>())
            .Returns(TestEntities.Category(1, "Sport"));

        _generator.GenerateAsync(Arg.Any<QuestionGenerationRequest>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await _sut.GenerateAsync(new GenerateQuestionsRequest { Category = "Sport", Count = 5 });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unavailable, result.Error.Type);
    }

    [Fact]
    public async Task GenerateAsync_DoesNotStoreGeneratedQuestions()
    {
        _categories.GetRandomAsync(Arg.Any<CancellationToken>()).Returns(TestEntities.Category(2, "History"));

        _generator.GenerateAsync(Arg.Any<QuestionGenerationRequest>(), Arg.Any<CancellationToken>())
            .Returns([new GeneratedQuestion("Which year was ChatGPT publicly released?", "2022", "History", 3)]);

        await _sut.GenerateAsync(new GenerateQuestionsRequest { Count = 1 });

        await _questions.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<Question>>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
