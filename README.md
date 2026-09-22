# ToJePrivela

Backend for a Slovak-language multiplayer trivia/quiz game. Functionally equivalent to the original
`ToSiPrehnal` API, rebuilt on Clean Architecture with a dedicated AI layer, manual mapping and a full
unit-test suite.

Players, games, questions and question categories are stored in SQLite. Numeric-answer questions can
also be generated on demand by an AI provider (OpenAI Chat Completions).

## Solution layout

```
src/
  ToJePrivela.Domain           entities and invariants; no dependencies
  ToJePrivela.Application      use cases, DTOs, manual mappers, ports (repositories, AI)
  ToJePrivela.Infrastructure   EF Core 10 + SQLite, repositories, migrations
  ToJePrivela.Ai               OpenAI question generation (implements the Application port)
  ToJePrivela.Api              slim controllers, DI composition root
tests/
  ToJePrivela.Domain.Tests           ToJePrivela.Application.Tests
  ToJePrivela.Infrastructure.Tests   ToJePrivela.Ai.Tests
  ToJePrivela.Api.Tests
```

Dependencies point inwards only:

```
Api ──► Application ──► Domain
 │           ▲
 ├──► Infrastructure ──┤
 └──► Ai ──────────────┘
```

`Infrastructure` and `Ai` implement interfaces owned by `Application`; nothing inner references them.
`Api` only wires them up at startup.

## Running

```bash
cd ToJePrivela
dotnet build ToJePrivela.slnx
dotnet run --project src/ToJePrivela.Api      # http://localhost:5178, Swagger UI at /swagger
dotnet test ToJePrivela.slnx
```

The database file is created and migrated on startup, so a fresh clone needs no manual EF step.

### Configuration

| Setting | Purpose |
| --- | --- |
| `ConnectionStrings:ToJePrivelaDbConnectionString` | SQLite file; missing or empty fails fast |
| `OpenAi:ApiKey` | API key — set via user-secrets or `OpenAi__ApiKey`, never in source |
| `OpenAi:Model`, `Temperature`, `MaxTokens`, `MaxRetryAttempts`, `TimeoutSeconds` | Generation tuning |
| `OpenAi:DefaultCategory`, `DefaultLanguage`, `DefaultDifficulty` | Fallbacks when the caller omits them |
| `Cors:AllowedOrigins` | Frontend origins; empty means "any origin" |

```bash
dotnet user-secrets set "OpenAi:ApiKey" "sk-..." --project src/ToJePrivela.Api
```

### Migrations

```bash
dotnet ef migrations add <Name> --project src/ToJePrivela.Infrastructure \
  --startup-project src/ToJePrivela.Infrastructure --output-dir Persistence/Migrations
```

## API

| Method | Route | Notes |
| --- | --- | --- |
| GET | `/api/players` | |
| GET/PUT/DELETE | `/api/players/{id}` | |
| POST | `/api/players` | 409 when the name is taken (case-insensitive) |
| GET | `/api/games` | |
| GET/PUT/DELETE | `/api/games/{id}` | |
| GET | `/api/games/{id}/details` | Includes player names and bad points |
| POST | `/api/games` | 2–10 known, distinct players |
| GET | `/api/questions?category=&difficulty=` | Both filters optional |
| GET/PUT/DELETE | `/api/questions/{id}` | |
| POST | `/api/questions` | |
| GET | `/api/questions/ai?category=&count=&language=` | AI-generated, not stored |
| GET | `/api/question-categories` | |
| GET/PUT/DELETE | `/api/question-categories/{id}` | |
| POST | `/api/question-categories` | |

Failures are returned as `ProblemDetails` with a machine-readable `code` extension.

## Design notes

- **Slim controllers.** A controller resolves one service, calls one method and converts the result
  with `ResultExtensions`. No branching, validation or persistence lives there.
- **Result instead of exceptions for flow.** Use cases return `Result`/`Result<T>` carrying an
  `Error` whose `ErrorType` the API maps to 400/404/409/503.
- **Rich domain.** Entities have private setters and validate their own invariants; a `DomainException`
  that escapes anyway is turned into a 400 by `DomainExceptionHandler`.
- **Manual mapping.** Static mappers per slice replace AutoMapper — explicit, compile-time checked,
  and directly unit-tested.
- **Numeric answers are enforced.** `Question` rejects non-numeric answers, and separators are refused
  so the Slovak decimal comma in `"3,5"` cannot silently become `35`.
- **AI layer is isolated.** `Application` depends only on `IQuestionGenerator`. Inside `ToJePrivela.Ai`,
  prompt building, HTTP transport and reply parsing are three separate, individually tested units.
- **Case-insensitive names.** Player and category names use a `NOCASE` collation plus a unique index,
  so duplicates are rejected by the database as well as by the use case.

## Frontend readiness

The frontend is not written yet; the API is prepared for it:

- CORS origins configured per environment through `Cors:AllowedOrigins`
- OpenAPI document at `/swagger/v1/swagger.json` for client generation
- camelCase JSON, `ProblemDetails` errors, correct status codes and `Location` headers
- `frontend/` is reserved for the client application

## Tests

`dotnet test ToJePrivela.slnx` runs 230 tests: domain invariants, every use case with substituted
ports, mappers, repositories against a migrated in-memory SQLite database, the AI prompt/parse/HTTP
units, result-to-HTTP mapping, and endpoint tests that host the real API with the AI provider stubbed.
