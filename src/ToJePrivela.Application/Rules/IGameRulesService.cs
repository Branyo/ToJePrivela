using ToJePrivela.Application.Common;
using ToJePrivela.Application.Rules.Dtos;

namespace ToJePrivela.Application.Rules;

public interface IGameRulesService
{
    Result<GameRulesDto> Get();
}
