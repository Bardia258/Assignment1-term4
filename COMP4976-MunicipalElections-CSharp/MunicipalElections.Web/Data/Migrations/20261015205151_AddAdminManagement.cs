using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MunicipalElections.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Positions_MunicipalityId",
                table: "Positions");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Positions",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                collation: "NOCASE",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "Revision",
                table: "Positions",
                type: "TEXT",
                nullable: false,
                defaultValue: "initial");

            migrationBuilder.AlterColumn<string>(
                name: "Province",
                table: "Municipalities",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                collation: "NOCASE",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Municipalities",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                collation: "NOCASE",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "Revision",
                table: "Municipalities",
                type: "TEXT",
                nullable: false,
                defaultValue: "initial");

            migrationBuilder.AddColumn<string>(
                name: "Revision",
                table: "Candidates",
                type: "TEXT",
                nullable: false,
                defaultValue: "initial");

            migrationBuilder.CreateTable(
                name: "MunicipalityAdminGroups",
                columns: table => new
                {
                    MunicipalityId = table.Column<int>(type: "INTEGER", nullable: false),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MunicipalityAdminGroups", x => x.MunicipalityId);
                    table.ForeignKey(
                        name: "FK_MunicipalityAdminGroups_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MunicipalityAdminGroups_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "MunicipalityId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Positions_MunicipalityId_Name",
                table: "Positions",
                columns: new[] { "MunicipalityId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Municipalities_Name_Province",
                table: "Municipalities",
                columns: new[] { "Name", "Province" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MunicipalityAdminGroups_RoleId",
                table: "MunicipalityAdminGroups",
                column: "RoleId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MunicipalityAdminGroups");

            migrationBuilder.DropIndex(
                name: "IX_Positions_MunicipalityId_Name",
                table: "Positions");

            migrationBuilder.DropIndex(
                name: "IX_Municipalities_Name_Province",
                table: "Municipalities");

            migrationBuilder.DropColumn(
                name: "Revision",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "Revision",
                table: "Municipalities");

            migrationBuilder.DropColumn(
                name: "Revision",
                table: "Candidates");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Positions",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 100,
                oldCollation: "NOCASE");

            migrationBuilder.AlterColumn<string>(
                name: "Province",
                table: "Municipalities",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 100,
                oldCollation: "NOCASE");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Municipalities",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 100,
                oldCollation: "NOCASE");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_MunicipalityId",
                table: "Positions",
                column: "MunicipalityId");
        }
    }
}
