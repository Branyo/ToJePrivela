# Frontend

Reserved for the client application. The backend is ready for it:

- Base URL `http://localhost:5178` (`https://localhost:7030`)
- OpenAPI document: `/swagger/v1/swagger.json`, Swagger UI: `/swagger`
- Add the dev server origin to `Cors:AllowedOrigins` in
  `src/ToJePrivela.Api/appsettings.Development.json` (ports 4200, 5173 and 3000 are preconfigured)
