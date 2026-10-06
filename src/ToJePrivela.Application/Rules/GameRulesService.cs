using ToJePrivela.Application.Accounts;
using ToJePrivela.Application.Common;
using ToJePrivela.Application.QuestionGeneration;
using ToJePrivela.Application.Rules.Dtos;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Rules;

/// <summary>Reads every limit from where it is defined, so the served rules can never drift from the enforced ones.</summary>
public sealed class GameRulesService : IGameRulesService
{
    private static readonly GameRulesDto Rules = new(
        new LimitDto(Game.MinPlayers, Game.MaxPlayers),
        new LimitDto(Game.MinBadCardLimit, Game.MaxBadCardLimit),
        Game.DefaultBadCardLimit,
        new LimitDto(Question.MinBadPoints, Question.MaxBadPoints),
        new LimitDto(Player.NameMinLength, Player.NameMaxLength),
        Account.MaxPlayers,
        new LimitDto(QuestionCategory.NameMinLength, QuestionCategory.NameMaxLength),
        QuestionGenerationOptions.MaxCount,
        PlayerAvatars.All,
        new LimitDto(Account.NameMinLength, Account.NameMaxLength),
        new LimitDto(PasswordRules.MinLength, PasswordRules.MaxLength));

    public Result<GameRulesDto> Get() => Result.Success(Rules);
}
