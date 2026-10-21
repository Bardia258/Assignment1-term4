using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MunicipalElections.Web.Data;

// Only deliberately constructed, non-secret messages may be written to the console.
public sealed class DatabaseInitializationException(string message) : Exception(message);

public static class DatabaseInitialization
{
    public const string SeedDemoDataCommand = "--seed-demo-data";
    public const string ProvisionSuperAdminCommand = "--provision-super-admin";

    public static bool IsInitializationCommand(string argument) =>
        argument is SeedDemoDataCommand or ProvisionSuperAdminCommand;

    public static async Task<int> RunAsync(IServiceProvider services, IConfiguration configuration,
        string[] args, TextWriter output, TextWriter error, CancellationToken cancellationToken = default)
    {
        try
        {
            var commands = args.Where(IsInitializationCommand).ToArray();
            if (commands.Length != 1 || args.Contains("--migrate-database"))
                throw new DatabaseInitializationException("Run exactly one initialization command at a time, separately from --migrate-database.");

            await using var scope = services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var connection = new SqliteConnectionStringBuilder(db.Database.GetConnectionString());
            if (connection.Mode == SqliteOpenMode.Memory || string.IsNullOrWhiteSpace(connection.DataSource)
                || connection.DataSource == ":memory:" || !File.Exists(connection.DataSource))
                throw new DatabaseInitializationException("Initialization requires an existing SQLite database file. Run the reviewed migration command separately first.");

            // Mode=ReadWrite prevents SQLite creating a new file if the target disappears
            // after the existence check. Never use EnsureCreated or Migrate here.
            connection.Mode = SqliteOpenMode.ReadWrite;
            db.Database.SetConnectionString(connection.ToString());
            if (!db.Database.GetMigrations().Any()
                || (await db.Database.GetPendingMigrationsAsync(cancellationToken)).Any()
                || db.Database.HasPendingModelChanges())
                throw new DatabaseInitializationException("Database migrations must be current before initialization. Run the reviewed migration command separately first.");

            if (commands[0] == SeedDemoDataCommand)
            {
                await DbSeeder.SeedAsync(db, cancellationToken);
                await output.WriteLineAsync("Demo election data initialized. Existing records and users were preserved.");
            }
            else
            {
                var created = await ProductionAdminProvisioner.ProvisionAsync(scope.ServiceProvider, configuration, cancellationToken);
                await output.WriteLineAsync(created
                    ? "Production Super Admin created. Remove BootstrapAdmin settings from the deployment configuration."
                    : "Production Super Admin already provisioned. No account, password, or permission changes were made.");
            }
            return 0;
        }
        catch (DatabaseInitializationException exception)
        {
            await error.WriteLineAsync(exception.Message);
        }
        catch (Exception)
        {
            // Database/Identity exceptions can include sensitive values. Do not print
            // exception messages, inner exceptions, configuration values or stack traces.
            await error.WriteLineAsync("Initialization failed. Check database availability, schema, and configuration. No credentials were logged.");
        }
        return 1;
    }
}
