using Microsoft.Data.Sqlite;

namespace MunicipalElections.Web.Data;

public static class DatabaseConfiguration
{
    public static string ResolveConnectionString(string connectionString, string contentRootPath)
    {
        var connection = new SqliteConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(connection.DataSource))
        {
            throw new InvalidOperationException("DefaultConnection must specify a SQLite Data Source.");
        }

        if (connection.DataSource != ":memory:")
        {
            connection.DataSource = Path.GetFullPath(connection.DataSource, contentRootPath);
            var webRoot = Path.Combine(Path.GetFullPath(contentRootPath), "wwwroot");
            var relativePath = Path.GetRelativePath(webRoot, connection.DataSource);
            if (relativePath != ".." && !relativePath.StartsWith(".." + Path.DirectorySeparatorChar)
                && !Path.IsPathRooted(relativePath))
            {
                throw new InvalidOperationException("The SQLite database must be outside wwwroot.");
            }
        }

        // Enforce FKs on every EF connection, regardless of SQLite build defaults.
        connection.ForeignKeys = true;
        return connection.ToString();
    }
}
