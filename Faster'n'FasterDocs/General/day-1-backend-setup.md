	# Day 1 — ASP.NET Core Backend Setup

**Goal:** Backend project created, Docker running PostgreSQL + Redis, API booting with correct folder structure.

---

## 1 — Create the Project

Run from repo root (`d:\Faster-n-Faster\`):

```
dotnet new webapi -n FasterNFaster.Api
```

Minimal API style — no MVC, no controllers. REST endpoints go inline in `Program.cs` via `app.MapPost(...)` etc. SignalR hubs are mapped the same way.

Remove the default `WeatherForecast` endpoint — it's boilerplate.

---

## 2 — Add NuGet Packages

From inside `FasterNFaster.Api\`:

| Package | Purpose |
|---|---|
| `Microsoft.AspNetCore.SignalR.StackExchangeRedis` | Redis as SignalR backplane |
| `StackExchange.Redis` | Direct Redis access for live game state |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | PostgreSQL via EF Core |
| `Microsoft.EntityFrameworkCore.Design` | EF Core CLI tooling (migrations) |
| `DotNetEnv` | Load `.env` file into config |

---

## 3 — Folder Structure

```
FasterNFaster.Api/
├── Hubs/              # SignalR hub(s)
├── Data/              # AppDbContext + EF Core entity configs
├── Models/            # Domain models / DTOs
├── Services/          # Business logic (comments engine, etc.)
└── Program.cs         # All route definitions + app bootstrap
```

No `Startup.cs` — only `Program.cs`.

---

## 4 — docker-compose.yml

Create at repo root (`d:\Faster-n-Faster\docker-compose.yml`):

```yaml
services:
  postgres:
    image: postgres:16
    ports:
      - "5432:5432"
    environment:
      POSTGRES_DB: fasternfaster
      POSTGRES_USER: fnf
      POSTGRES_PASSWORD: fnf_dev
    volumes:
      - postgres_data:/var/lib/postgresql/data

  redis:
    image: redis:7
    ports:
      - "6379:6379"

volumes:
  postgres_data:
```

---

## 5 — Environment Config

`.env` at repo root (gitignored):

```
DATABASE_URL=Host=localhost;Port=5432;Database=fasternfaster;Username=fnf;Password=fnf_dev
REDIS_URL=localhost:6379
CORS_ORIGINS=http://localhost:3000
```

`.env.example` (committed to git) — same keys, blank values.

---

## 6 — Program.cs Wiring Order

1. `DotNetEnv.Env.Load()` — must run before anything reads config
2. CORS — allow `http://localhost:3000`
3. EF Core — `AppDbContext` with Npgsql
4. Redis — `IConnectionMultiplexer` singleton
5. SignalR — `AddSignalR().AddStackExchangeRedis(redisUrl)`
6. Map routes — `app.MapHub<RaceHub>("/racehub")`, inline `app.MapPost(...)` for REST

---

## 7 — Stubs (compile only)

- `Data/AppDbContext.cs` — empty `DbContext` subclass
- `Hubs/RaceHub.cs` — `Hub` subclass with a single `Ping` → "pong" method

Full implementation comes in Week 2–3.

---

## Done When

- [ ] `docker compose up -d` → PostgreSQL and Redis containers healthy
- [ ] `dotnet run` → API starts with no errors
- [ ] `http://localhost:5000` returns 404 (expected — no routes yet)
