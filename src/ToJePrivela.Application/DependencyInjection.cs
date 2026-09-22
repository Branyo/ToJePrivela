using Microsoft.Extensions.DependencyInjection;
using ToJePrivela.Application.Games;
using ToJePrivela.Application.Players;
using ToJePrivela.Application.QuestionCategories;
using ToJePrivela.Application.Questions;

namespace ToJePrivela.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IPlayerService, PlayerService>();
        services.AddScoped<IGameService, GameService>();
        services.AddScoped<IQuestionService, QuestionService>();
        services.AddScoped<IQuestionCategoryService, QuestionCategoryService>();

        return services;
    }
}
