# AIMBYS Platform — Local Setup Guide

## Prerequisites

- [.NET 10 SDK](https://dot.net/download) (preview)
- SQL Server (any of the following):
  - **LocalDB** (included with Visual Studio)
  - **SQL Server Express** (free download)
  - **Docker** (cross-platform)

## Quick Start (Windows + LocalDB)

```powershell
# 1. Clone the repository
git clone https://github.com/sarthakvadhel/Aimbys-Paper-Generation-Platform.git
cd Aimbys-Paper-Generation-Platform

# 2. Set the database connection string
cd Aimbys.Web
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:AimbysDb" "Server=(localdb)\mssqllocaldb;Database=AimbysDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"

# 3. Apply database migrations
cd ..
dotnet ef database update --project Aimbys.Infrastructure --startup-project Aimbys.Web

# 4. Run the application
cd Aimbys.Web
dotnet run
```

The app will start at `https://localhost:5001` (or the port shown in console output).

## Quick Start (Docker — Any OS)

```bash
# 1. Start SQL Server in Docker
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong!Passw0rd" \
  -p 1433:1433 --name aimbys-sql \
  -d mcr.microsoft.com/mssql/server:2022-latest

# 2. Clone and configure
git clone https://github.com/sarthakvadhel/Aimbys-Paper-Generation-Platform.git
cd Aimbys-Paper-Generation-Platform/Aimbys.Web
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:AimbysDb" "Server=localhost,1433;Database=AimbysDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;MultipleActiveResultSets=true"

# 3. Apply migrations and run
cd ..
dotnet ef database update --project Aimbys.Infrastructure --startup-project Aimbys.Web
cd Aimbys.Web
dotnet run
```

## Installing dotnet-ef Tool

If `dotnet ef` is not available:

```bash
dotnet tool install --global dotnet-ef
```

## Default Login Credentials

After the database is created, the app seeds default roles and an admin user:

| Role | Email | Password |
|------|-------|----------|
| SuperAdmin | `admin@aimbys.com` | `Admin@123` |

> Check `Aimbys.Infrastructure/Identity/IdentitySeeder.cs` for the exact credentials if they differ.

## Troubleshooting

| Error | Solution |
|-------|----------|
| `Cannot open database 'AimbysDb'` | Run `dotnet ef database update` first |
| `No connection string configured` | Set user-secrets as shown above |
| `dotnet ef not found` | Run `dotnet tool install --global dotnet-ef` |
| `.NET 10 SDK not found` | Download preview SDK from [dot.net](https://dot.net) |
| `PendingModelChangesWarning` | Run `dotnet ef migrations add Fix --project Aimbys.Infrastructure --startup-project Aimbys.Web` then `dotnet ef database update` |

## Resetting the Database

If you need to start fresh:

```powershell
dotnet ef database drop --project Aimbys.Infrastructure --startup-project Aimbys.Web --force
dotnet ef database update --project Aimbys.Infrastructure --startup-project Aimbys.Web
```

## Project Structure

```
Aimbys-Paper-Generation-Platform/
├── Aimbys.Domain/           # Entities, Enums, Domain interfaces
├── Aimbys.Application/      # Service interfaces, DTOs
├── Aimbys.Infrastructure/   # EF Core, Identity, Service implementations
├── Aimbys.Web/              # ASP.NET MVC app (Areas, Views, Controllers)
└── docs/                    # Documentation
```
