using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Aimbys.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by <c>dotnet ef</c> when generating or
/// applying migrations. The shipped <c>Aimbys.Web/appsettings.Development.json</c>
/// targets database <c>AimbysDb</c>; this factory's last-resort fallback
/// must use the <em>same</em> name so design-time and runtime converge on
/// one database. Earlier revisions defaulted to a separate
/// <c>Aimbys.DesignTime</c> database, which silently bifurcated schema
/// between developers' machines.
///
/// <para>
/// Resolution order, first non-empty wins:
/// </para>
/// <list type="number">
///   <item>The <c>AIMBYS_CONNECTION_STRING</c> environment variable
///         (preserves the existing CI / scripted-override contract).</item>
///   <item>The standard ASP.NET <c>ConnectionStrings__Default</c>
///         environment variable.</item>
///   <item>The hardcoded LocalDB fallback (<see cref="DefaultDesignTimeConnectionString"/>),
///         which targets <c>AimbysDb</c> &mdash; the same database the
///         dev runtime uses.</item>
/// </list>
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    /// <summary>
    /// Environment variable inspected first; lets a CI job point migrations at
    /// any reachable SQL Server without editing files.
    /// </summary>
    public const string ConnectionStringEnvVar = "AIMBYS_CONNECTION_STRING";

    /// <summary>
    /// Last-resort fallback when no environment variable is set. Targets
    /// SQL Server LocalDB and the <c>AimbysDb</c> database so design-time
    /// migrations land in the same database the dev runtime uses.
    /// </summary>
    public const string DefaultDesignTimeConnectionString =
        "Server=(localdb)\\mssqllocaldb;Database=AimbysDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

    public AppDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable(ConnectionStringEnvVar);

        if (string.IsNullOrWhiteSpace(conn))
        {
            conn = Environment.GetEnvironmentVariable("ConnectionStrings__Default");
        }

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
