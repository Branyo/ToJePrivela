using System.Net;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi;
using Serilog;
using ToJePrivela.Ai;
using ToJePrivela.Api.Common;
using ToJePrivela.Api.Middleware;
using ToJePrivela.Application;
using ToJePrivela.Application.Abstractions.Localization;
using ToJePrivela.Identity;
using ToJePrivela.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

DeploymentSafety.EnsureNoDevelopmentSigningKey(builder.Environment, builder.Configuration);
DeploymentSafety.EnsureDevelopmentNotReachableFromNetwork(builder.Environment, builder.Configuration);

builder.Host.UseSerilog((context, configuration) =>
    configuration.WriteTo.Console().ReadFrom.Configuration(context.Configuration));

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton(ApiInfo.FromAssembly(typeof(Program).Assembly));
builder.Services.AddAiQuestionGeneration(builder.Configuration);
builder.Services.AddPasswordLogins(builder.Configuration);
builder.Services.AddAccessTokenAuthentication();
// Category names and question texts are returned in the language the request's Accept-Language asks for.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentLanguage, HttpCurrentLanguage>();

// Enums travel as their names ("Chooser"), which is a wire-format concern, so it is set here rather than on the DTOs.
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    // A body ASP.NET cannot read or validate is answered like a use case's Request.Invalid, code included.
    .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = InvalidModelStateResponse.Create);
builder.Services.AddProblemDetails();
// /api/health is anonymous and runs a database query, so its healthy answer is reused briefly: a flood of probes
// cannot compete with game writes for the SQLite file.
builder.Services.AddOutputCache();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddExceptionHandler<ConcurrencyConflictExceptionHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = ApiInfo.ApiTitle,
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

builder.Services.AddSingleton(_ => new SignInAddressLimiter(signInRateLimit));

// Behind a reverse proxy every connection comes from the proxy, so the sign-in limits would lump all clients into one
// address. X-Forwarded-For is trusted only from loopback and the proxies listed here.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;

    foreach (var proxy in builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [])
    {
        options.KnownProxies.Add(IPAddress.Parse(proxy));
    }
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = (context, cancellationToken) =>
        RateLimitRejection.WriteAsync(context.HttpContext, context.Lease, cancellationToken);
    options.AddFixedWindowLimiter(AiRateLimitOptions.PolicyName, limiter =>
    {
        limiter.PermitLimit = aiRateLimit.PermitLimit;
        limiter.Window = TimeSpan.FromSeconds(aiRateLimit.WindowSeconds);
        limiter.QueueLimit = 0;
    });
    options.AddPolicy(
        SignInRateLimitOptions.PolicyName,
        httpContext => SignInRateLimiting.Partition(httpContext, signInRateLimit));
});

var app = builder.Build();

var isTesting = app.Environment.IsEnvironment("Testing");

if (!isTesting)
{
    await app.Services.InitializeDatabaseAsync();
    await app.Services.ProvisionAdminAccountsAsync();
}

app.UseForwardedHeaders();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging(options =>
    options.GetLevel = (context, _, exception) => RequestLogLevel.For(context, exception));

// Not in Development: the Angular dev server proxies /api over plain HTTP, and a redirect to the HTTPS port is a
// cross-origin redirect for the browser, which then drops the Authorization header, so every call would answer 401.
// Probe endpoints are exempt: they poll over plain HTTP and would read a 307 as a failure.
if (!isTesting && !app.Environment.IsDevelopment())
{
    app.UseWhen(context => !context.IsProbe(), branch => branch.UseHttpsRedirection());
}

app.UseCors(CorsOptions.PolicyName);
// Names and texts depend on the request's Accept-Language, so a shared cache must not mix the languages up.
app.Use((context, next) =>
{
    context.Response.OnStarting(() =>
    {
        context.Response.Headers.Append(HeaderNames.Vary, HeaderNames.AcceptLanguage);
        return Task.CompletedTask;
    });
    return next();
});
app.UseAuthentication();
app.UseSignInLimits();
app.UseRateLimiter();
app.UseAuthorization();
app.UseOutputCache();
app.MapControllers();
// Only healthy answers are cached (the default policy stores 200s), so a failing check is reported on the next probe.
app.MapHealthChecks("/api/health")
    .AllowAnonymous()
    .AsProbe()
    .CacheOutput(policy => policy.Expire(TimeSpan.FromSeconds(5)));

DeploymentSafety.StopDevelopmentReachableFromNetwork(app);

await app.RunAsync();

/// <summary>Exposed so the integration tests can host the API.</summary>
public partial class Program;
