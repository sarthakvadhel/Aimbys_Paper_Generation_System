using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Aimbys.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by <c>dotnet ef</c> when generating migrations.
///
/// At design time the EF tooling can either spin up the host (which requires
/// the full <see cref="Aimbys.Web"/> startup pipeline) or call this factory
/// directly. Providing a factory keeps migrations self-contained: they only
/// need a connection string, not a configured app.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    /// <summary>
    /// Environment variable inspected first; lets a CI job point migrations at
    /// any reachable SQL Server without editing files.
    /// </summary>
    public const string ConnectionStringEnvVar = "AIMBYS_CONNECTION_STRING";

    /// <summary>
    /// Fallback used when no environment variable is set. Targets SQL Server
    /// LocalDB on Windows, which is the typical dev install. The migration
    /// tooling does not need the database to exist or be reachable to scaffold
    /// migrations &mdash; only to apply them.
    /// </summary>
    public const string DefaultDesignTimeConnectionString =
        "Server=(localdb)\\mssqllocaldb;Database=Aimbys.DesignTime;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

    public AppDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable(ConnectionStringEnvVar);
        if (string.IsNullOrWhiteSpace(conn))
        {
            conn = DefaultDesignTimeConnectionString;
        }

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(conn, sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
            .Options;

        return new AppDbContext(options);
    }
}
