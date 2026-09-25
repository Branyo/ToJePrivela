namespace ToJePrivela.Application.QuestionGeneration.Dtos;

/// <param name="Discarded">Generated items dropped as duplicates or over the requested count.</param>
public sealed record QuestionGenerationSummaryDto(int Requested, int Created, int Discarded);
