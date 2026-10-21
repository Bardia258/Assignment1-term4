using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace MunicipalElections.Web.Data;

/// <summary>Reconciles the one historical standalone schema, then applies the integrated chain.</summary>
public static class DatabaseMigration
{
    public static async Task MigrateAsync(ApplicationDbContext db, CancellationToken cancellationToken = default)
    {
        // Retain applied IDs and user data. EF ignores historical IDs no longer in the assembly.
        const string historicalStandaloneId = "20261016062911_RemoveIosSpecificDatabaseFeatures";
        if ((await db.Database.GetAppliedMigrationsAsync(cancellationToken)).Contains(historicalStandaloneId))
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var connection = db.Database.GetDbConnection();
            await using var command = connection.CreateCommand();
            command.Transaction = transaction.GetDbTransaction();
            command.CommandText = "SELECT COUNT(*) FROM pragma_table_info('AspNetUsers') WHERE name = 'FullName'";
            if (Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 0)
                await db.Database.ExecuteSqlRawAsync("ALTER TABLE AspNetUsers ADD COLUMN FullName TEXT NULL;", cancellationToken);
            await db.Database.ExecuteSqlRawAsync("""
                CREATE TABLE IF NOT EXISTS "SavedCandidates" (
                    "SavedCandidateId" INTEGER NOT NULL CONSTRAINT "PK_SavedCandidates" PRIMARY KEY AUTOINCREMENT,
                    "UserId" TEXT NOT NULL,
                    "CandidateId" INTEGER NOT NULL,
                    "DateSaved" TEXT NOT NULL,
                    CONSTRAINT "FK_SavedCandidates_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_SavedCandidates_Candidates_CandidateId" FOREIGN KEY ("CandidateId") REFERENCES "Candidates" ("CandidateId") ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS "IX_SavedCandidates_CandidateId" ON "SavedCandidates" ("CandidateId");
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_SavedCandidates_UserId_CandidateId" ON "SavedCandidates" ("UserId", "CandidateId");
                """, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        await db.Database.MigrateAsync(cancellationToken);
    }
}
