using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CivicHero.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddDisputeManagementAndRelatedChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_fraud_analysis",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ComplaintId = table.Column<int>(type: "int", nullable: false),
                    FraudScore = table.Column<int>(type: "int", nullable: false),
                    RiskLevel = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Reasons = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Reviewed = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_fraud_analysis", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DisputeAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ComplaintId = table.Column<int>(type: "int", nullable: false),
                    InitiatedByUserId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Details = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisputeAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DisputeAuditLogs_Users_InitiatedByUserId",
                        column: x => x.InitiatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ReputationLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    PointsChange = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReputationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReputationLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RewardCatalogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PointsRequired = table.Column<int>(type: "int", nullable: false),
                    QuantityAvailable = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RewardCatalogs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Redemptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RewardId = table.Column<int>(type: "int", nullable: false),
                    PointsSpent = table.Column<int>(type: "int", nullable: false),
                    RedeemedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Notes = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Redemptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Redemptions_RewardCatalogs_RewardId",
                        column: x => x.RewardId,
                        principalTable: "RewardCatalogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Redemptions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ai_fraud_analysis_ComplaintId",
                table: "ai_fraud_analysis",
                column: "ComplaintId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_fraud_analysis_FraudScore",
                table: "ai_fraud_analysis",
                column: "FraudScore");

            migrationBuilder.CreateIndex(
                name: "IX_ai_fraud_analysis_RiskLevel",
                table: "ai_fraud_analysis",
                column: "RiskLevel");

            migrationBuilder.CreateIndex(
                name: "IX_DisputeAuditLogs_InitiatedByUserId",
                table: "DisputeAuditLogs",
                column: "InitiatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Redemptions_RewardId",
                table: "Redemptions",
                column: "RewardId");

            migrationBuilder.CreateIndex(
                name: "IX_Redemptions_UserId",
                table: "Redemptions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReputationLogs_UserId",
                table: "ReputationLogs",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_fraud_analysis");

            migrationBuilder.DropTable(
                name: "DisputeAuditLogs");

            migrationBuilder.DropTable(
                name: "Redemptions");

            migrationBuilder.DropTable(
                name: "ReputationLogs");

            migrationBuilder.DropTable(
                name: "RewardCatalogs");
        }
    }
}
