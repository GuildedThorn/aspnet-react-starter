# CLAUDE.md

Guidance for Claude Code when working in this repository.

## Overview

ASP.NET Core 10 Web API + React/TypeScript/Vite SPA. The backend serves the
built frontend from `wwwroot/`. See `README.md` for the full tour.

## Commands

Backend (repo root): `dotnet run`, `dotnet test Tests/App.Tests.csproj`
Frontend (`frontend/`): `bun run dev | build | lint | test | smoke`
Reproducible build: `nix build .#default`

The frontend builds into `../wwwroot` (the backend's static root). In dev, run
`dotnet run` and `bun run dev` together; the SPA proxy serves the frontend.

## Architecture

- **Auth** — JWT issued on login, stored as an HttpOnly `Secure SameSite=Strict`
  cookie named `token`; the bearer handler reads it from the cookie. Roles
  `owner`/`user`; policy `PrivilegedOnly` requires either. WebAuthn (Fido2)
  powers passkeys / security keys and optional 2FA.
- **Services** (`Services/`) — `MongoDbService` (collection helpers),
  `RabbitMqService` (lazy, resilient publisher), `JwtTokenService`,
  `RealtimeHub` (SignalR at `/hub`), `WebAuthnChallengeStore`.
- **Example CRUD** — `NotesController` + `Models/Note.cs` +
  `frontend/src/pages/Notes.tsx`: REST + Mongo + a RabbitMQ publish + a SignalR
  broadcast. Use it as the pattern for new resources.
- **Frontend** — `AuthProvider`/`useAuth` hydrate from `/api/user/me`;
  `ProtectedRouter` guards routes; path aliases (`@components`, `@pages`,
  `@backend`, `@lib`, …) are set in `vite.config.ts`. API calls use relative
  URLs and `credentials: "include"`.

## Config

`Jwt:Key` (base64 HMAC-SHA256), `MongoDB:ConnectionString`/`DatabaseName` are
required. Optional: `Cors:Origins`, `Fido2:*`, `RabbitMQ:*`, `Loki:Uri`. Supply
via `.env` / env vars (`__` = nested) or `Resources/config.json`. Never commit
secrets — `.env` and `Resources/config.json` are gitignored.

## Conventions

- New API resource: model → `GetXCollection()` in `MongoDbService` → controller
  (`[Authorize(Policy = "PrivilegedOnly")]`) → client fn in
  `frontend/src/backend/api.ts` → page + route.
- After changing `frontend/package-lock.json` or the `.csproj` package set,
  regenerate the Nix locks: `npmDepsHash` (`nix run nixpkgs#prefetch-npm-deps --
  frontend/package-lock.json`) and `deps.json` (`nix build
  .#default.passthru.fetch-deps -o fetch-deps && ./fetch-deps deps.json`).
- Run `bun run smoke` before shipping bundler/dependency changes.
