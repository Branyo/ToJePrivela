using ToJePrivela.Application.Common;
using ToJePrivela.Application.Info.Dtos;

namespace ToJePrivela.Application.Info;

public interface IApiInfoService
{
    Result<ApiInfoDto> Get();
}
