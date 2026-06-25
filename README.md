# Full-stack starter — ASP.NET Core 10 + React

A batteries-included starter for building a SPA-backed web app. The backend is
an ASP.NET Core 10 API that also serves the compiled React frontend as static
files; everything deploys flake-native on NixOS (or via Docker / your own CI).

## What's included

- **Backend** — ASP.NET Core 10 Web API
  - JWT-cookie auth (register / login / logout) + **WebAuthn** passkeys & security keys
  - **MongoDB** persistence (`MongoDbService`)
  - **SignalR** realtime hub (`/hub`)
  - **RabbitMQ** publisher (lazy + resilient — no broker needed for local dev)
  - Rate limiting, forwarded-headers, HSTS, security headers, Swagger (dev)
- **Frontend** — React 19 + TypeScript + Vite + Tailwind CSS, dark/light theme,
  auth context, protected routes, a headless browser smoke test
- **Example feature** — a `Notes` CRUD wired end-to-end (REST + Mongo +
  RabbitMQ publish + live SignalR updates). Copy it to build your own resources.
- **Ops** — Nix flake (`packages.default` + reusable `nixosModules.default`),
  multi-stage `Dockerfile`, GitHub Actions CI.

## Quick start

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/), [Bun](https://bun.sh/), and a
  **MongoDB** instance. (RabbitMQ and Grafana Loki are optional.)
- Or just `nix develop` to get the .NET SDK + Bun + Node.

### 1. Configure
```bash
cp .env.example .env
# Generate a JWT key and paste it into .env as Jwt__Key:
openssl rand -base64 32
# Set MongoDB__ConnectionString / __DatabaseName too.
```

### 2. Run (two terminals)
```bash
# Terminal 1 — backend (https://localhost:7xxx, http://localhost:5xxx)
dotnet run

# Terminal 2 — frontend dev server (https://localhost:5173)
cd frontend
bun install
bun run dev
```
Open the Vite URL. Register an account, then visit **Notes** to see the example
CRUD update live across tabs over SignalR.

> In production the frontend is built into `wwwroot` and served by the backend
> from the same origin — no CORS, no second host.

## Commands

| | |
|---|---|
| `dotnet run` | Run the backend |
| `dotnet test Tests/App.Tests.csproj` | Backend tests (integration tests need Docker) |
| `cd frontend && bun run dev` | Frontend dev server |
| `cd frontend && bun run build` | Build the SPA → `../wwwroot` |
| `cd frontend && bun run lint` / `test` | ESLint / Vitest |
| `cd frontend && bun run smoke` | Headless browser smoke test of every route |
| `nix build .#default` | Reproducible production build (SPA + API) |
| `docker build -t app .` | Container image |

## Project layout

```
Program.cs            App wiring (config, auth, CORS, SignalR, middleware, SPA fallback)
Controllers/          AuthController, UserController, WebAuthnController, NotesController
Services/             MongoDbService, RabbitMqService, JwtTokenService, RealtimeHub, WebAuthnChallengeStore
Models/               User, WebAuthnCredential, Note
Resources/            config.example.json (optional config source)
Tests/                xUnit unit + integration tests
frontend/             React + Vite + Tailwind app (builds into ../wwwroot)
flake.nix             Nix package + NixOS module
Dockerfile            Multi-stage build (bun → dotnet publish → runtime)
```

## Adding a feature

`NotesController` + `Models/Note.cs` + `frontend/src/pages/Notes.tsx` are the
reference. To add a resource: add a model, a `GetXCollection()` helper in
`MongoDbService`, a controller (gate it with `[Authorize(Policy =
"PrivilegedOnly")]`), an API client in `frontend/src/backend/api.ts`, and a page
+ route in `frontend/src/routes/AppRoutes.tsx`.

## Configuration keys

Supply via `.env` / environment variables (`__` = nested) or `Resources/config.json`.
See `.env.example` and `Resources/config.example.json`. Required: `Jwt:Key`,
`MongoDB:ConnectionString`, `MongoDB:DatabaseName`.

## Deploy

- **NixOS** — the flake exports `nixosModules.default` (`services.app`). Add the
  repo as a flake input and enable the service; point a reverse proxy at its port.
- **Docker** — `docker build -t app . && docker run -p 8080:8080 --env-file .env app`.

## License

MIT — see `LICENSE` (update the copyright holder).
