using ToJePrivela.Application.Games.Mapping;
using ToJePrivela.Application.Players.Dtos;
using ToJePrivela.Application.Players.Mapping;
using ToJePrivela.Application.QuestionCategories.Dtos;
using ToJePrivela.Application.QuestionCategories.Mapping;
using ToJePrivela.Application.QuestionGeneration;
using ToJePrivela.Application.QuestionGeneration.Dtos;
using ToJePrivela.Application.QuestionGeneration.Mapping;
using ToJePrivela.Application.Questions.Dtos;
using ToJePrivela.Application.Questions.Mapping;
using ToJePrivela.Application.Tests.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Tests.Mapping;

public class MapperTests
{
    private static readonly DateTime Start = new(2026, 9, 22, 18, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void PlayerMapper_CopiesIdAndName()
    {
        var dto = PlayerMapper.ToDto(TestEntities.Player(4, "Brano"));

        Assert.Equal(4, dto.Id);
        Assert.Equal("Brano", dto.Name);
    }

    [Fact]
    public void PlayerMapper_BuildsEntityFromRequest()
    {
        var player = PlayerMapper.ToEntity(new CreatePlayerRequest { Name = " Brano " });

        Assert.Equal("Brano", player.Name);
        Assert.Equal(0, player.Id);
    }

    [Fact]
    public void GameMapper_ToDto_ListsPlayerIds()
    {
        var dto = GameMapper.ToDto(TestEntities.Game(3, [5, 6], Start));

        Assert.Equal(3, dto.Id);
        Assert.Equal(Start, dto.Started);
        Assert.Null(dto.Finished);
        Assert.Equal([5, 6], dto.PlayerIds);
    }

    [Fact]
    public void GameMapper_ToDetailsDto_FallsBackToEmptyNameWhenPlayerIsNotLoaded()
    {
        var details = GameMapper.ToDetailsDto(TestEntities.Game(3, [5, 6], Start));

        Assert.Equal(2, details.Players.Count);
        Assert.All(details.Players, player =>
        {
            Assert.Equal(string.Empty, player.Name);
            Assert.Equal(0, player.BadPoints);
        });
        Assert.Equal([5, 6], details.Players.Select(p => p.PlayerId));
    }

    [Fact]
    public void QuestionCategoryMapper_MapsAuthorWhenLoaded()
    {
        var category = TestEntities.Category(1, "Sport", addedByPlayerId: 2);
        typeof(QuestionCategory).GetProperty(nameof(QuestionCategory.AddedByPlayer))!
            .SetValue(category, TestEntities.Player(2, "Duri"));

        var dto = QuestionCategoryMapper.ToDto(category);

        Assert.Equal("Sport", dto.Name);
        Assert.NotNull(dto.AddedByPlayer);
        Assert.Equal("Duri", dto.AddedByPlayer!.Name);
    }

    [Fact]
    public void QuestionCategoryMapper_LeavesAuthorNullWhenNotLoaded()
    {
        var dto = QuestionCategoryMapper.ToDto(TestEntities.Category(1, "Sport", addedByPlayerId: 2));

        Assert.Null(dto.AddedByPlayer);
    }

    [Fact]
    public void QuestionCategoryMapper_BuildsEntityFromRequest()
    {
        var category = QuestionCategoryMapper.ToEntity(
            new CreateQuestionCategoryRequest { Name = "Sport", AddedByPlayerId = 3 });

        Assert.Equal("Sport", category.Name);
        Assert.Equal(3, category.AddedByPlayerId);
    }

    [Fact]
    public void QuestionCategoryMapper_ToCreatedDto_IncludesTheGenerationSummary()
    {
        var category = TestEntities.Category(4, "Music");

        var dto = QuestionCategoryMapper.ToCreatedDto(category, new QuestionGenerationResult([], 10, 3));

        Assert.Equal(4, dto.Id);
        Assert.Equal("Music", dto.Name);
        Assert.Equal(new QuestionGenerationSummaryDto(10, 0, 3), dto.QuestionGeneration);
    }

    [Fact]
    public void QuestionMapper_CopiesEveryField()
    {
        var question = TestEntities.Question(
            9, "Which year was ChatGPT publicly released?", "2022", TestEntities.Category(3, "History"), 4, QuestionSource.Ai);

        var dto = QuestionMapper.ToDto(question);

        Assert.Equal(9, dto.Id);
        Assert.Equal("Which year was ChatGPT publicly released?", dto.Text);
        Assert.Equal("2022", dto.Answer);
        Assert.Equal(3, dto.CategoryId);
        Assert.Equal("History", dto.CategoryName);
        Assert.Equal(4, dto.BadPoints);
        Assert.Equal("Ai", dto.Source);
        Assert.Equal(TestEntities.CreatedAt, dto.CreatedAt);
    }

    [Fact]
    public void QuestionMapper_BuildsAManualEntityFromRequest()
    {
        var category = TestEntities.Category(3, "History");

        var question = QuestionMapper.ToEntity(
            new CreateQuestionRequest { Text = "Which year was ChatGPT publicly released?", Answer = "2022", CategoryId = 3 },
            category,
            badPoints: 2,
            createdAt: Start);

        Assert.Equal("2022", question.Answer);
        Assert.Same(category, question.Category);
        Assert.Equal(3, question.CategoryId);
        Assert.Equal(2, question.BadPoints);
        Assert.Equal(QuestionSource.Manual, question.Source);
        Assert.Equal(Start, question.CreatedAt);
    }

    [Fact]
    public void QuestionGenerationMapper_CopiesTheCounts()
    {
        var category = TestEntities.Category(3, "History");
        var question = TestEntities.Question(1, "Which year was ChatGPT publicly released?", "2022", category);

        var summary = QuestionGenerationMapper.ToSummaryDto(new QuestionGenerationResult([question], 5, 2));

        Assert.Equal(new QuestionGenerationSummaryDto(5, 1, 2), summary);
    }
}
