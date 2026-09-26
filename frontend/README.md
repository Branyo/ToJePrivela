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
needed while developing. No sign-in: players are just names.

## How a game runs

1. **Setup** (`/new`): add 2–12 players (new names or regulars), pick how many bad cards end the game
   (1–10, default 3), and optionally pick question categories (none = all). A category with AI questions
   can be created right there, which a fresh database needs because it ships without questions.
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
  core/i18n/       LanguageService, MessagePipe, translated page titles
  shared/          ranking + seat order, flying-card animation, bad card, avatar, confetti
  features/
    home/          landing page, running games, rules
    setup/         players, card limit, categories, AI category generation
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
