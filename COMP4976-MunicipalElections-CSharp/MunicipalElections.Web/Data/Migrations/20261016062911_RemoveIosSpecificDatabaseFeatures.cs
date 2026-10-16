using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MunicipalElections.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIosSpecificDatabaseFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Refuse to discard data, including when migrations run on another
            // developer's database or through Development startup in the future.
            // A failed CHECK aborts the transaction before any persistent DDL.
            migrationBuilder.Sql("""
                CREATE TEMP TABLE "__StandaloneCleanupGuard" (
                    "SavedCount" INTEGER NOT NULL
                        CONSTRAINT "SavedCandidates is not empty; cleanup cancelled" CHECK ("SavedCount" = 0),
                    "FullNameCount" INTEGER NOT NULL
                        CONSTRAINT "FullName contains data; cleanup cancelled" CHECK ("FullNameCount" = 0)
                );
                INSERT INTO "__StandaloneCleanupGuard" ("SavedCount", "FullNameCount")
                SELECT (SELECT COUNT(*) FROM "SavedCandidates"),
                       (SELECT COUNT(*) FROM "AspNetUsers" WHERE "FullName" IS NOT NULL);
                DROP TABLE "__StandaloneCleanupGuard";
                """);

            // Native SQLite DROP COLUMN (3.35+) preserves the existing Identity
            // table, indexes, and relationships without EF's table rebuild.
            migrationBuilder.Sql("""ALTER TABLE "AspNetUsers" DROP COLUMN "FullName";""");

            migrationBuilder.DropTable(
                name: "SavedCandidates");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "AspNetUsers",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SavedCandidates",
                columns: table => new
                {
                    SavedCandidateId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CandidateId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    DateSaved = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedCandidates", x => x.SavedCandidateId);
                    table.ForeignKey(
                        name: "FK_SavedCandidates_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SavedCandidates_Candidates_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "Candidates",
                        principalColumn: "CandidateId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SavedCandidates_CandidateId",
                table: "SavedCandidates",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedCandidates_UserId_CandidateId",
                table: "SavedCandidates",
                columns: new[] { "UserId", "CandidateId" },
                unique: true);
        }
    }
}
