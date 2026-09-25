using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ToJePrivela.Application.Games;
using ToJePrivela.Application.Players;
using ToJePrivela.Application.QuestionCategories;
using ToJePrivela.Application.QuestionGeneration;
using ToJePrivela.Application.Questions;

namespace ToJePrivela.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<QuestionGenerationOptions>()
            .Bind(configuration.GetSection(QuestionGenerationOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IBadPointsPicker, RandomBadPointsPicker>();
        services.AddScoped<IPlayerService, PlayerService>();
        services.AddScoped<IGameService, GameService>();
        services.AddScoped<IQuestionService, QuestionService>();
        services.AddScoped<IQuestionCategoryService, QuestionCategoryService>();
        services.AddScoped<IQuestionGenerationService, QuestionGenerationService>();

        return services;
    }
}
