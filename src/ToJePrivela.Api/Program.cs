using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi;
using Serilog;
using ToJePrivela.Ai;
using ToJePrivela.Api.Common;
using ToJePrivela.Api.Middleware;
using ToJePrivela.Application;
using ToJePrivela.Identity;
using ToJePrivela.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration.WriteTo.Console().ReadFrom.Configuration(context.Configuration));

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAiQuestionGeneration(builder.Configuration);
builder.Services.AddPasswordLogins(builder.Configuration);
builder.Services.AddAccessTokenAuthentication();

// Enums travel as their names ("Chooser"), which is a wire-format concern, so it is set here rather than on the DTOs.
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    // A body ASP.NET cannot read or validate is answered like a use case's Request.Invalid, code included.
    .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = InvalidModelStateResponse.Create);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddExceptionHandler<ConcurrencyConflictExceptionHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ToJePrivela API",
        Version = "v1",
        Description = "Backend for the Slovak trivia game."
    });

    // Lets Swagger UI send the access token from POST /api/auth/sign-in.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "The accessToken returned by POST /api/auth/sign-in."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

var corsOptions = builder.Configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new CorsOptions();

builder.Services.AddCors(options => options.AddPolicy(CorsOptions.PolicyName, policy =>
{
    policy.AllowAnyHeader().AllowAnyMethod();

    if (corsOptions.AllowedOrigins.Length > 0)
    {
        policy.WithOrigins(corsOptions.AllowedOrigins).AllowCredentials();
    }
    else
    {
        policy.AllowAnyOrigin();
    }
}));

var aiRateLimit = builder.Configuration.GetSection(AiRateLimitOptions.SectionName).Get<AiRateLimitOptions>()
    ?? new AiRateLimitOptions();

var signInRateLimit = builder.Configuration.GetSection(SignInRateLimitOptions.SectionName).Get<SignInRateLimitOptions>()
    ?? new SignInRateLimitOptions();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter(AiRateLimitOptions.PolicyName, limiter =>
    {
        limiter.PermitLimit = aiRateLimit.PermitLimit;
        limiter.Window = TimeSpan.FromSeconds(aiRateLimit.WindowSeconds);
        limiter.QueueLimit = 0;
    });
    options.AddPolicy(SignInRateLimitOptions.PolicyName, httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = signInRateLimit.PermitLimit,
            Window = TimeSpan.FromSeconds(signInRateLimit.WindowSeconds),
            QueueLimit = 0
        }));
});

var app = builder.Build();

var isTesting = app.Environment.IsEnvironment("Testing");

if (!isTesting)
{
    await app.Services.InitializeDatabaseAsync();
    await app.Services.ProvisionAdminAccountsAsync();
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();

if (!isTesting)
{
    app.UseHttpsRedirection();
}

app.UseCors(CorsOptions.PolicyName);
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();

/// <summary>Exposed so the integration tests can host the API.</summary>
public partial class Program;
