using Microsoft.OpenApi;
using Serilog;
using ToJePrivela.Ai;
using ToJePrivela.Api.Common;
using ToJePrivela.Api.Middleware;
using ToJePrivela.Application;
using ToJePrivela.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration.WriteTo.Console().ReadFrom.Configuration(context.Configuration));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAiQuestionGeneration(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => options.SwaggerDoc("v1", new OpenApiInfo
{
    Title = "ToJePrivela API",
    Version = "v1",
    Description = "Backend for the Slovak trivia game."
}));

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

var app = builder.Build();

var isTesting = app.Environment.IsEnvironment("Testing");

if (!isTesting)
{
    await app.MigrateDatabaseAsync();
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
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();

/// <summary>Exposed so the integration tests can host the API.</summary>
public partial class Program;
