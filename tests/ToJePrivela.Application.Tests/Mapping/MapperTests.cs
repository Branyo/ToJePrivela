using ToJePrivela.Application.Abstractions.Persistence;
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
using ToJePrivela.Domain.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Tests.Mapping;

public class MapperTests
{
    private static readonly DateTime Start = new(2026, 9, 22, 18, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void PlayerMapper_CopiesIdNameAndAvatar()
    {
        var dto = PlayerMapper.ToDto(TestEntities.Player(4, "Brano"));

        Assert.Equal(4, dto.Id);
        Assert.Equal("Brano", dto.Name);
        Assert.Equal("🦊", dto.Avatar);
    }

    [Fact]
    public void PlayerMapper_BuildsEntityFromRequest()
    {
        var player = PlayerMapper.ToEntity(new CreatePlayerRequest { Name = " Brano " }, TestAccountId, "🐸");

        Assert.Equal("Brano", player.Name);
        Assert.Equal("🐸", player.Avatar);
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
    public void GameMapper_ToDetailsDto_CarriesTheGamesVerdictInSeatOrder()
    {
        var game = TestEntities.Game(3, [6, 5], Start);
        var question = TestEntities.Question(1, "How many wheels does a car have?", "4", TestEntities.Category(1, "Cars"), badPoints: 2);
        game.AwardBadCard(6, question, null, Start);

        var details = GameMapper.ToDetailsDto(game);

        Assert.Equal([5, 6], details.Players.Select(p => p.PlayerId));
        Assert.Equal([(2, false), (1, true)], details.Players.Select(p => (p.Rank, p.IsLoser)));
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

    [Theory]
    [InlineData(Language.Sk, "Šport")]
    [InlineData(Language.En, "Sport")]
    public void QuestionCategoryMapper_GivesTheNameInTheLanguageAndBothNames(Language language, string name)
    {
        Assert.Equal(
            new QuestionCategoryDto(1, name, "Šport", "Sport", 5, 2),
            QuestionCategoryMapper.ToDto(TestEntities.Category(1, "Šport", "Sport"), new QuestionCounts(5, 2), language));
    }

    [Fact]
    public void QuestionCategoryMapper_ToDtos_CountsNoQuestionsForACategoryWithoutAny()
    {
        var dtos = QuestionCategoryMapper.ToDtos(
            [TestEntities.Category(1, "Šport", "Sport"), TestEntities.Category(2, "Hudba", "Music")],
            new Dictionary<int, QuestionCounts> { [1] = new(4, 1) },
            Language.Sk);

        Assert.Equal([(4, 1), (0, 0)], dtos.Select(dto => (dto.QuestionCount, dto.AiQuestionCount)));
    }

    [Fact]
    public void QuestionCategoryMapper_ToCreatedDto_IncludesTheGenerationSummary()
    {
        var category = TestEntities.Category(4, "Hudba", "Music");

        var dto = QuestionCategoryMapper.ToCreatedDto(category, new QuestionGenerationResult([], 10, 3), Language.En);

        Assert.Equal(4, dto.Id);
        Assert.Equal("Music", dto.Name);
        Assert.Equal("Hudba", dto.NameSk);
        Assert.Equal("Music", dto.NameEn);
        Assert.Equal(new QuestionGenerationSummaryDto(10, 0, 3), dto.QuestionGeneration);
    }

    [Fact]
    public void QuestionMapper_CopiesEveryField()
    {
        var question = TestEntities.Question(
            9,
            "V ktorom roku bol verejne spustený ChatGPT?",
            "2022",
            TestEntities.Category(3, "História", "History"),
            4,
            QuestionSource.Ai,
            textEn: "Which year was ChatGPT publicly released?");

        var dto = QuestionMapper.ToDto(question, Language.En);

        Assert.Equal(9, dto.Id);
        Assert.Equal("Which year was ChatGPT publicly released?", dto.Text);
        Assert.Equal("V ktorom roku bol verejne spustený ChatGPT?", dto.TextSk);
        Assert.Equal("Which year was ChatGPT publicly released?", dto.TextEn);
        Assert.Equal("2022", dto.Answer);
        Assert.Equal(3, dto.CategoryId);
        Assert.Equal("History", dto.CategoryName);
        Assert.Equal(4, dto.BadPoints);
        Assert.Equal("Ai", dto.Source);
        Assert.Equal(TestEntities.CreatedAt, dto.CreatedAt);
        Assert.Equal(0, dto.ViewCount);
        Assert.Null(dto.LastViewedAt);
    }

    [Theory]
    [InlineData(Language.Sk)]
    [InlineData(Language.En)]
    public void QuestionMapper_ShowsTheSlovakTextOfAQuestionWithoutAnEnglishOne(Language language)
    {
        var question = TestEntities.WithoutEnglish(
            TestEntities.Question(9, "V ktorom roku bol verejne spustený ChatGPT?", "2022", TestEntities.Category(3, "História", "History")));

        var dto = QuestionMapper.ToDto(question, language);

        Assert.Equal("V ktorom roku bol verejne spustený ChatGPT?", dto.Text);
        Assert.Null(dto.TextEn);
    }

    [Fact]
    public void QuestionMapper_BuildsAManualEntityFromRequest()
    {
        var category = TestEntities.Category(3, "History");

        var question = QuestionMapper.ToEntity(
            new CreateQuestionRequest
            {
                TextSk = "V ktorom roku bol verejne spustený ChatGPT?",
                TextEn = "Which year was ChatGPT publicly released?",
                Answer = "2022",
                CategoryId = 3
            },
            category,
            badPoints: 2,
            createdAt: Start);

        Assert.Equal(("V ktorom roku bol verejne spustený ChatGPT?", "Which year was ChatGPT publicly released?"), (question.TextSk, question.TextEn));
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
