using NSubstitute;
using ToJePrivela.Application.Abstractions.Ai;
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

    private readonly IQuestionRepository _questions = Substitute.For<IQuestionRepository>();
    private readonly IQuestionCategoryRepository _categories = Substitute.For<IQuestionCategoryRepository>();
    private readonly IQuestionGenerator _generator = Substitute.For<IQuestionGenerator>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly QuestionService _sut;

    public QuestionServiceTests()
    {
        _sut = new QuestionService(_questions, _categories, _generator, _unitOfWork);
    }

    [Fact]
    public async Task GetAsync_PassesTheFilterToTheRepository()
    {
        _questions.FindAsync("Sport", 3, Arg.Any<CancellationToken>())
            .Returns([TestEntities.Question(1, ValidText, "2022", "Sport", 3)]);

        var result = await _sut.GetAsync(new QuestionFilter { Category = "Sport", Difficulty = 3 });

        Assert.True(result.IsSuccess);
        var question = Assert.Single(result.Value);
        Assert.Equal("Sport", question.Category);
        Assert.Equal(3, question.BadPoints);
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
    public async Task CreateAsync_StoresTheQuestion()
    {
        var request = new CreateQuestionRequest
        {
            Text = ValidText,
            Answer = "2022",
            Category = "History",
            Difficulty = 2
        };

        var result = await _sut.CreateAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal("2022", result.Value.Answer);
        Assert.Equal(4, result.Value.BadPoints);
        await _questions.Received(1).AddAsync(Arg.Any<Question>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ChangesTheStoredQuestion()
    {
        var question = TestEntities.Question(1, ValidText, "2022", "History", 3);
        _questions.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(question);

        var result = await _sut.UpdateAsync(1, new UpdateQuestionRequest
        {
            Text = "How many players are on a football pitch?",
            Answer = "11",
            Category = "Sport",
            Difficulty = 1
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("11", question.Answer);
        Assert.Equal("Sport", question.Category);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNotFoundForUnknownQuestion()
    {
        _questions.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns((Question?)null);

        var result = await _sut.UpdateAsync(7, new UpdateQuestionRequest
        {
            Text = ValidText,
            Answer = "2022",
            Category = "History",
            Difficulty = 3
        });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheQuestion()
    {
        var question = TestEntities.Question(1, ValidText, "2022", "History", 3);
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
