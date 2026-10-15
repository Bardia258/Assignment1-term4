using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MunicipalElections.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddElectionModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "AspNetUsers",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MunicipalityId",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Municipalities",
                columns: table => new
                {
                    MunicipalityId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Province = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ElectionDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    LogoFileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    SeedKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Municipalities", x => x.MunicipalityId);
                });

            migrationBuilder.CreateTable(
                name: "Positions",
                columns: table => new
                {
                    PositionId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SeatsAvailable = table.Column<int>(type: "INTEGER", nullable: false),
                    MunicipalityId = table.Column<int>(type: "INTEGER", nullable: false),
                    SeedKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Positions", x => x.PositionId);
                    table.UniqueConstraint("AK_Positions_PositionId_MunicipalityId", x => new { x.PositionId, x.MunicipalityId });
                    table.CheckConstraint("CK_Positions_SeatsAvailable", "\"SeatsAvailable\" > 0");
                    table.ForeignKey(
                        name: "FK_Positions_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "MunicipalityId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Candidates",
                columns: table => new
                {
                    CandidateId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Profile = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    PhotoFileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    Website = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 254, nullable: true),
                    DateCreated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PositionId = table.Column<int>(type: "INTEGER", nullable: false),
                    MunicipalityId = table.Column<int>(type: "INTEGER", nullable: false),
                    SeedKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Candidates", x => x.CandidateId);
                    table.ForeignKey(
                        name: "FK_Candidates_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "MunicipalityId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Candidates_Positions_PositionId_MunicipalityId",
                        columns: x => new { x.PositionId, x.MunicipalityId },
                        principalTable: "Positions",
                        principalColumns: new[] { "PositionId", "MunicipalityId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SavedCandidates",
                columns: table => new
                {
                    SavedCandidateId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    CandidateId = table.Column<int>(type: "INTEGER", nullable: false),
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
                name: "IX_AspNetUsers_MunicipalityId",
                table: "AspNetUsers",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_Candidates_MunicipalityId",
                table: "Candidates",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_Candidates_PositionId_MunicipalityId",
                table: "Candidates",
                columns: new[] { "PositionId", "MunicipalityId" });

            migrationBuilder.CreateIndex(
                name: "IX_Candidates_SeedKey",
                table: "Candidates",
                column: "SeedKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Municipalities_SeedKey",
                table: "Municipalities",
                column: "SeedKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Positions_MunicipalityId",
                table: "Positions",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_SeedKey",
                table: "Positions",
                column: "SeedKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SavedCandidates_CandidateId",
                table: "SavedCandidates",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedCandidates_UserId_CandidateId",
                table: "SavedCandidates",
                columns: new[] { "UserId", "CandidateId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Municipalities_MunicipalityId",
                table: "AspNetUsers",
                column: "MunicipalityId",
                principalTable: "Municipalities",
                principalColumn: "MunicipalityId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Municipalities_MunicipalityId",
                table: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "SavedCandidates");

            migrationBuilder.DropTable(
                name: "Candidates");

            migrationBuilder.DropTable(
                name: "Positions");

            migrationBuilder.DropTable(
                name: "Municipalities");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_MunicipalityId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "MunicipalityId",
                table: "AspNetUsers");
        }
    }
}
