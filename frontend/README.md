# To je priveľa! — frontend

Angular 22 client for the estimation party game: standalone components, signals, zoneless change
detection, Vitest.

```bash
npm install
npm start          # http://localhost:4200; start the API first (dotnet run --project ../src/ToJePrivela.Api)
npm test
npm run build
```

The dev server proxies `/api` to `http://localhost:5178` (`proxy.conf.json`), so no CORS setup is
needed while developing.

## Signing in

Every screen but `/sign-in` needs a signed-in login (`signedInGuard`). The sign-in screen asks for a login
name and password and posts them to `POST /api/auth/sign-in`. When the API answers `Auth.UnknownLogin`, a
dialog asks whether to create that login; on yes, a second window offers the name (prefilled, still
editable), a password and its repetition, checked against the limits `/api/rules` serves, and posts to
`POST /api/auth/accounts`. Either way the API's access token is kept by `AuthStore` (in `localStorage`
until it expires) and sent with every `/api` request by `authInterceptor`. A 401 drops the session and
returns to sign-in with a `returnUrl`.

`/settings` is the login's settings page: its own players (add, delete; at most `maxPlayersPerAccount`,
100) for everyone, plus, for admins only (`account.isAdmin`), the shared questions: create categories, add
or delete AI questions, delete categories. The API enforces the same rule; hiding the section only spares
everyone else a screen that would fail. Admin logins come from the API's `Authentication:Admins`
configuration — see the backend README.

## How a game runs

1. **Setup** (`/new`): add 2–12 players (new names or regulars), pick how many bad cards end the game
   (1–10, default 3), and optionally pick question categories (none = all). A fresh database ships
   without questions; an admin creates categories with AI questions on `/settings`.
2. **Play** (`/games/:id?categories=1,2`): a question is drawn from the least viewed ones and counted as
   shown. Players estimate out loud; whoever says "to je priveľa!" taps the button to reveal the answer
   and then taps the player who takes the donkey card, which flies over to that seat. **Skip** (before or
   after the reveal) gives nobody a card. **End game** stops early.
3. **Results** (`/games/:id/summary`): the game ends when a player reaches the card limit; the player
   with the most bad points is the loser (card count breaks a tie). Rematch reuses players, limit and
   categories.

## Structure

```
src/app/
  core/api/        typed HTTP clients mirroring the backend DTOs, ProblemDetails → message
  core/auth/       AuthStore (session), authInterceptor, guards
  core/i18n/       LanguageService, MessagePipe, translated page titles
  shared/          ranking + seat order, flying-card animation, bad card, avatar, confetti
  features/
    sign-in/       name + password sign-in, create-login dialogs
    home/          landing page, running games, rules
    setup/         players, card limit, categories
    settings/      the login's players (add, delete) and, for admins, AI question generation
    play/          PlayStore (round state machine) + game screen
    summary/       loser spotlight and scoreboard
```

`PlayStore` is provided per game screen and owns the round phases
(`loading → asking → revealed → awarding → …`, plus `no-questions`, `finished`, `error`). The server
decides when the game is over; the client only follows `finished`. Players are seated by id on every
screen, so each keeps the same animal and colour.

## Languages

Slovak (default) and English, via [ngx-translate](https://github.com/ngx-translate/core). Texts live in
`public/i18n/{sk,en}.json` and load before the first screen renders; the SK/EN switch in the top bar
changes the language live and remembers it in `localStorage`. `LanguageService` also gives the locale
(`sk-SK` / `en-GB`) for dates, numbers and name sorting.

- Plurals use CLDR categories: a key such as `play.limit` holds `one`/`few`/`many`/`other` (Slovak) or
  `one`/`other` (English); pick the form with `i18n.pluralKey('play.limit', count)`.
- Messages kept in signals (errors, notices) are `Message` objects (`{ key, params }`), rendered with
  the `message` pipe, so they re-translate on a language switch.
- Backend errors are translated by their `code` (`errors.api.<code>`); an unknown code shows the
  backend's own text.
- To add a language: add its JSON file, an entry in `LANGUAGES` (`core/i18n/language.ts`), and register
  its Angular locale data in `app.config.ts`.
