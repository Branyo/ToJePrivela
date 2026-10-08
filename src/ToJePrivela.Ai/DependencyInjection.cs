using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ToJePrivela.Ai.OpenAi;
using ToJePrivela.Ai.Parsing;
using ToJePrivela.Ai.Prompts;
using ToJePrivela.Application.Abstractions.Ai;

namespace ToJePrivela.Ai;

public static class DependencyInjection
{
    public static IServiceCollection AddAiQuestionGeneration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<OpenAiOptions>()
            .Bind(configuration.GetSection(OpenAiOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IQuestionPromptBuilder, QuestionPromptBuilder>();
        services.AddSingleton<IGeneratedQuestionParser, GeneratedQuestionParser>();
        services.AddSingleton<ITranslationPromptBuilder, TranslationPromptBuilder>();
        services.AddSingleton<ITranslationParser, TranslationParser>();

        services.AddHttpClient<IChatCompletionClient, OpenAiChatCompletionClient>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<OpenAiOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("ToJePrivela-OpenAI-Client");
        });

        services.AddScoped<IQuestionGenerator, OpenAiQuestionGenerator>();
        services.AddScoped<ITextTranslator, OpenAiTextTranslator>();

        return services;
    }
}
