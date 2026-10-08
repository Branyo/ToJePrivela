using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ToJePrivela.Application.Accounts;
using ToJePrivela.Application.Common;
using ToJePrivela.Application.Games;
using ToJePrivela.Application.Players;
using ToJePrivela.Application.QuestionCategories;
using ToJePrivela.Application.QuestionGeneration;
using ToJePrivela.Application.Questions;
using ToJePrivela.Application.Rules;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<QuestionGenerationOptions>()
            .Bind(configuration.GetSection(QuestionGenerationOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<AdminAccountsOptions>()
            .Bind(configuration.GetSection(AdminAccountsOptions.SectionName))
            .Validate(
                options => options.Admins.All(admin => RequestValidator.Validate(admin) is null),
                $"Every Authentication:Admins entry needs a Name ({Account.NameMinLength}–{Account.NameMaxLength} characters) " +
                $"and a Password ({PasswordRules.MinLength}–{PasswordRules.MaxLength} characters).")
            .ValidateOnStart();

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IBadPointsPicker, RandomBadPointsPicker>();
        services.AddSingleton<IQuestionPicker, RandomQuestionPicker>();
        services.AddSingleton<IAvatarPicker, RandomAvatarPicker>();
        services.AddSingleton<IGameRulesService, GameRulesService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IAdminAccountProvisioner, AdminAccountProvisioner>();
        services.AddScoped<IPlayerService, PlayerService>();
        services.AddScoped<IGameService, GameService>();
        services.AddScoped<IQuestionService, QuestionService>();
        services.AddScoped<IQuestionCategoryService, QuestionCategoryService>();
        services.AddScoped<IQuestionGenerationService, QuestionGenerationService>();

        return services;
    }

    /// <summary>Stores the configured admins; run once the database is up to date.</summary>
    public static async Task ProvisionAdminAccountsAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IAdminAccountProvisioner>().ProvisionAsync(cancellationToken);
    }
}
