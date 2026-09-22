namespace ToJePrivela.Application.Games.Dtos;

public sealed class UpdateGameRequest
{
    public DateTime? Started { get; init; }

    public DateTime? Finished { get; init; }
}
