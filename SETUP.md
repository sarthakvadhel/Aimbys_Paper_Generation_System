# AIMBYS Platform — Installation Guide

This is the canonical install guide. Earlier revisions of this document
referenced configuration keys (`ConnectionStrings:AimbysDb`) and database
names (`Aimbys.Dev`, `Aimbys.DesignTime`) that the runtime never actually
reads, which led to "the migration runs but the app can't see the database"
confusion. The truth is simple:

- The runtime reads **`ConnectionStrings:Default`** (one key, one place).
- The platform's database name is **`AimbysDb`** by convention. The shipped
  `appsettings.Development.json` uses it; the design-time migration factory
  uses it as a fallback. They converge on one DB.
- **No SuperAdmin credentials are baked into source.** The Identity seeder
  reads `Identity:DefaultAdmin:Email` and `Identity:DefaultAdmin:Password`
  from configuration — you choose them once before the first run.

---

## 1. Prerequisites

| Tool | Version | Why |
|------|---------|-----|
| .NET 10 SDK | `net10.0` (preview) — pinned in [`global.json`](./global.json) | Build + run |
| `dotnet-ef` CLI | matches the SDK | Apply migrations |
| SQL Server | LocalDB / Express / Docker / Azure SQL — any reachable instance | Persistence |

Install the EF Core CLI globally if it's not already present:

```bash
dotnet tool install --global dotnet-ef
```

(Or run `dotnet tool restore` from the repo root if a `dotnet-tools.json`
manifest is later added.)

---

## 2. Clone

```bash
git clone https://github.com/sarthakvadhel/Aimbys_Paper_Generation_System.git
cd Aimbys_Paper_Generation_System
dotnet restore Aimbys.slnx
dotnet build   Aimbys.slnx
```

The web app starts even when no connection string is configured — it logs a
warning and only fails on the first DB call. That keeps `dotnet run` useful
for UI iteration before SQL Server is reachable.

---

## 3. Configure the database connection

`Aimbys.Web/appsettings.json` ships with `ConnectionStrings:Default = ""` so
nothing leaks into source control. Pick one of the three options below.

### Option A — SQL Server LocalDB (Windows, simplest)

`Aimbys.Web/appsettings.Development.json` already contains:

```json
"ConnectionStrings": {
  "Default": "Server=(localdb)\\mssqllocaldb;Database=AimbysDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

If LocalDB is installed, no further config is needed for development. Make
sure the instance is running:

```powershell
sqllocaldb create mssqllocaldb
sqllocaldb start  mssqllocaldb
```

### Option B — Docker SQL Server (any OS)

```bash
docker run \
  -e "ACCEPT_EULA=Y" \
  -e "MSSQL_SA_PASSWORD=YourStrong!Passw0rd" \
  -p 1433:1433 \
  --name aimbys-sql \
  -d mcr.microsoft.com/mssql/server:2022-latest

dotnet user-secrets --project Aimbys.Web init
dotnet user-secrets --project Aimbys.Web set "ConnectionStrings:Default" \
  "Server=localhost,1433;Database=AimbysDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

### Option C — External SQL Server / Azure SQL

```bash
dotnet user-secrets --project Aimbys.Web init
dotnet user-secrets --project Aimbys.Web set "ConnectionStrings:Default" \
  "Server=tcp:<host>,1433;Database=AimbysDb;User Id=<user>;Password=<pwd>;Encrypt=True;TrustServerCertificate=False;"
```

For containers / CI, set the standard ASP.NET environment variable instead:

```bash
export ConnectionStrings__Default="Server=...;Database=AimbysDb;..."
```

---

## 4. Configure the initial SuperAdmin credentials

The Identity seeder runs on every startup and is **idempotent**. It always
seeds the four canonical roles (`SuperAdmin`, `InstituteAdmin`, `Teacher`,
`Student`) and, if you supply both an email and a password, ensures a
matching `SuperAdmin` user exists.

If both keys are unset, the seeder logs:

> `No Identity:DefaultAdmin configured; skipping super-admin user seed.`

— and skips the user-creation step. You'd have to register through the UI
and then manually elevate the row to `SuperAdmin`. **Don't do that** — set
the keys before first run.

### Dev defaults (one-block setup)

```bash
dotnet user-secrets --project Aimbys.Web set "Identity:DefaultAdmin:Email"    "superadmin@aimbys.local"
dotnet user-secrets --project Aimbys.Web set "Identity:DefaultAdmin:Password" "Admin@123"
```

Now the first launch creates this user:

| Role | Email | Password |
|------|-------|----------|
| `SuperAdmin` | `superadmin@aimbys.local` | `Admin@123` |

The password meets the configured Identity policy (8+ chars, digit, lower,
upper). Change it after the first login at `/Account/ChangePassword`.

### Production guidance

Never commit credentials. In production:

- Set `Identity__DefaultAdmin__Email` and `Identity__DefaultAdmin__Password`
  via your secrets manager (Azure Key Vault, AWS Secrets Manager, Kubernetes
  secrets, GitHub Actions environment, etc.).
- Rotate the password after the first sign-in.
- Once the first SuperAdmin has signed in and created additional admins,
  remove the seed keys from configuration so the seeder no-ops on
  subsequent boots.

---

## 5. Apply migrations

This is the single canonical command — run from the **repository root**:

```bash
dotnet ef database update --project Aimbys.Infrastructure --startup-project Aimbys.Web
```

What happens:

1. EF tooling boots `Aimbys.Web/Program.cs`, picks up
   `ConnectionStrings:Default` from your user-secrets / appsettings /
   environment.
2. The 5 shipped migrations (`InitialCreate`, `SyncPaperQuestionFix`,
   `AddUserPasswordPolicy`, `AddResultStateAndPercentile`,
   `AddCachedLeaderboard`) are applied in order.
3. The schema lands in the database your connection string points at — the
   convention is `AimbysDb`.

If `dotnet ef` cannot bootstrap the host, it falls back to
`AppDbContextFactory`. That factory now also targets `AimbysDb` (the prior
revision targeted a separate `Aimbys.DesignTime` database, which silently
created a parallel schema on developers' machines — that's now fixed).

To override the target without editing files:

```bash
# Either of these env vars works; the repo-specific one wins.
export AIMBYS_CONNECTION_STRING="Server=...;Database=AimbysDb;..."
export ConnectionStrings__Default="Server=...;Database=AimbysDb;..."
```

To preview the SQL without applying it:

```bash
dotnet ef migrations script \
  --project Aimbys.Infrastructure \
  --startup-project Aimbys.Web \
  --idempotent
```

To add a new migration after changing the model:

```bash
dotnet ef migrations add <Name> \
  --project Aimbys.Infrastructure \
  --startup-project Aimbys.Web \
  --output-dir Migrations
```

---

## 6. Run the app

```bash
dotnet run --project Aimbys.Web
```

The console prints the listening URL (default `http://localhost:5094`).

On first boot the logs should include:

```
info: Aimbys.Infrastructure.IdentitySeeder[0]
      Seeded role 'SuperAdmin'.
info: Aimbys.Infrastructure.IdentitySeeder[0]
      Seeded role 'InstituteAdmin'.
info: Aimbys.Infrastructure.IdentitySeeder[0]
      Seeded role 'Teacher'.
info: Aimbys.Infrastructure.IdentitySeeder[0]
      Seeded role 'Student'.
info: Aimbys.Infrastructure.IdentitySeeder[0]
      Seeded default super-admin user 'superadmin@aimbys.local'.
info: Aimbys.Infrastructure.IdentitySeeder[0]
      Added 'superadmin@aimbys.local' to role 'SuperAdmin'.
```

If you see `Identity seed skipped because the database was unreachable`,
revisit step 3 — the connection string is not getting through.

---

## 7. First sign-in

1. Go to `/Account/Login`.
2. Sign in with the credentials configured in step 4.
3. You're redirected to `/SuperAdmin` — the platform-overview dashboard.
4. From `/SuperAdmin/Institutes` you can onboard the first tenant; the
   tenant's `InstituteAdmin` then invites teachers and students.

---

## 8. Resetting the database

When schema diverges or you want a clean slate:

```bash
dotnet ef database drop \
  --project Aimbys.Infrastructure \
  --startup-project Aimbys.Web \
  --force

dotnet ef database update \
  --project Aimbys.Infrastructure \
  --startup-project Aimbys.Web
```

The seeder will recreate roles + the SuperAdmin on the next boot.

---

## 9. Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| `Cannot open database 'AimbysDb'` on first hit | Migrations not applied | Run step 5 |
| Login fails with valid credentials and no rows in `AspNetUsers` | `Identity:DefaultAdmin:*` keys not configured before first boot | Run step 4, restart |
| Two databases visible: `AimbysDb` **and** `Aimbys.DesignTime` | You ran `dotnet ef migrations add` against an old checkout where the design-time factory targeted a separate DB | Drop the orphan: `dotnet ef database drop --connection "Server=(localdb)\mssqllocaldb;Database=Aimbys.DesignTime;..." --force` (or just delete it from SSMS) |
| `No connection string configured` | `ConnectionStrings:Default` is empty | Step 3 |
| `dotnet ef not found` | EF CLI not installed | `dotnet tool install --global dotnet-ef` |
| `.NET 10 SDK not found` | Preview SDK not installed | Install per `global.json` from <https://dot.net> |
| `PendingModelChangesWarning` after pulling new code | New entities landed without a migration | `dotnet ef migrations add <Name> --project Aimbys.Infrastructure --startup-project Aimbys.Web` then re-run step 5 |
| Identity seed succeeds in dev but the SuperAdmin login fails in prod | Different `ConnectionStrings:Default` between environments — seed wrote to the dev DB | Verify the prod runtime is actually reading the prod connection string |

---

## 10. Quick-reference cheat sheet

```bash
# One-time install
dotnet tool install --global dotnet-ef

# Configure (LocalDB on Windows: skip; Development.json already has it)
dotnet user-secrets --project Aimbys.Web init
dotnet user-secrets --project Aimbys.Web set "ConnectionStrings:Default"      "Server=(localdb)\\mssqllocaldb;Database=AimbysDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
dotnet user-secrets --project Aimbys.Web set "Identity:DefaultAdmin:Email"    "superadmin@aimbys.local"
dotnet user-secrets --project Aimbys.Web set "Identity:DefaultAdmin:Password" "Admin@123"

# Apply schema
dotnet ef database update --project Aimbys.Infrastructure --startup-project Aimbys.Web

# Run
dotnet run --project Aimbys.Web

# Reset
dotnet ef database drop   --project Aimbys.Infrastructure --startup-project Aimbys.Web --force
dotnet ef database update --project Aimbys.Infrastructure --startup-project Aimbys.Web
```

## 11. Project layout

```
Aimbys-Paper-Generation-Platform/
├── Aimbys.Domain/           # Entities, Enums, Domain interfaces
├── Aimbys.Application/      # Service interfaces, DTOs (no EF, no MVC)
├── Aimbys.Infrastructure/   # AppDbContext, EF migrations, service impls, Identity
└── Aimbys.Web/              # ASP.NET Core MVC: Areas, Views, Controllers, ViewComponents
```
