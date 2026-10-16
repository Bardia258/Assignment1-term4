using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MunicipalElections.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class BackfillAdministratorGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfill existing Identity groups without changing users, roles, or assignments.
            migrationBuilder.Sql("""
                INSERT INTO MunicipalityAdminGroups (MunicipalityId, RoleId)
                SELECT m.MunicipalityId, r.Id FROM Municipalities m JOIN AspNetRoles r
                ON r.Name = CASE m.SeedKey
                    WHEN 'demo:pitt-meadows' THEN 'Pitt Meadows Admin'
                    WHEN 'demo:maple-ridge' THEN 'Maple Ridge Admin'
                    WHEN 'demo:burnaby' THEN 'Burnaby Admin' END
                WHERE m.SeedKey IN ('demo:pitt-meadows','demo:maple-ridge','demo:burnaby')
                AND NOT EXISTS (SELECT 1 FROM MunicipalityAdminGroups g WHERE g.MunicipalityId=m.MunicipalityId);
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Preserve mappings that may have been used or changed after bootstrapping.

        }
    }
}
