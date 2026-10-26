# The API image. Built by docker-compose.yml; the Angular app has its own image (deploy/web.Dockerfile).

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Project files first, so the package restore is cached until a dependency changes.
COPY src/ToJePrivela.Domain/ToJePrivela.Domain.csproj src/ToJePrivela.Domain/
COPY src/ToJePrivela.Application/ToJePrivela.Application.csproj src/ToJePrivela.Application/
COPY src/ToJePrivela.Infrastructure/ToJePrivela.Infrastructure.csproj src/ToJePrivela.Infrastructure/
COPY src/ToJePrivela.Ai/ToJePrivela.Ai.csproj src/ToJePrivela.Ai/
COPY src/ToJePrivela.Identity/ToJePrivela.Identity.csproj src/ToJePrivela.Identity/
COPY src/ToJePrivela.Api/ToJePrivela.Api.csproj src/ToJePrivela.Api/
# The game's version (VERSION), stamped on the assemblies by Directory.Build.props and reported by GET /api/info.
COPY Directory.Build.props VERSION ./
RUN dotnet restore src/ToJePrivela.Api/ToJePrivela.Api.csproj

COPY src/ src/
RUN dotnet publish src/ToJePrivela.Api/ToJePrivela.Api.csproj -c Release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# The database and log files live in /data, a volume, so they survive a new image. The image runs as the non-root
# "app" user, which must own the folder; a new volume copies this ownership.
RUN mkdir /data && chown app:app /data
VOLUME /data

ENV ConnectionStrings__ToJePrivelaDbConnectionString="Data Source=/data/ToJePrivela.db" \
    Serilog__WriteTo__0__Args__path="/data/logs/log-.txt"

COPY --from=build /app .
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "ToJePrivela.Api.dll"]
