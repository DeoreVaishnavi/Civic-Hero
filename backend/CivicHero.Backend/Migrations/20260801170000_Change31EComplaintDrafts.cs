using CivicHero.Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CivicHero.Backend.Migrations;

[DbContext(typeof(CivicDbContext))]
[Migration("20260801170000_Change31EComplaintDrafts")]
public partial class Change31EComplaintDrafts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "complaint_drafts",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                CitizenId = table.Column<long>(type: "bigint", nullable: false),
                Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                Description = table.Column<string>(type: "text", nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                Category = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                CitizenSeverity = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "Medium")
                    .Annotation("MySql:CharSet", "utf8mb4"),
                DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                WardId = table.Column<long>(type: "bigint", nullable: true),
                Latitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                Longitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                Address = table.Column<string>(type: "varchar(400)", maxLength: 400, nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                Landmark = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                PossibleEmergency = table.Column<bool>(type: "tinyint(1)", nullable: false),
                EmergencyReason = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_complaint_drafts", x => x.Id);
                table.ForeignKey(
                    name: "FK_complaint_drafts_departments_DepartmentId",
                    column: x => x.DepartmentId,
                    principalTable: "departments",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_complaint_drafts_users_CitizenId",
                    column: x => x.CitizenId,
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_complaint_drafts_wards_WardId",
                    column: x => x.WardId,
                    principalTable: "wards",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "complaint_draft_evidence",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                ComplaintDraftId = table.Column<long>(type: "bigint", nullable: false),
                S3Key = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                FileName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                FileSize = table.Column<long>(type: "bigint", nullable: false),
                MimeType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                UploadedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_complaint_draft_evidence", x => x.Id);
                table.ForeignKey(
                    name: "FK_complaint_draft_evidence_complaint_drafts_ComplaintDraftId",
                    column: x => x.ComplaintDraftId,
                    principalTable: "complaint_drafts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_complaint_draft_evidence_ComplaintDraftId",
            table: "complaint_draft_evidence",
            column: "ComplaintDraftId");

        migrationBuilder.CreateIndex(
            name: "IX_complaint_draft_evidence_S3Key",
            table: "complaint_draft_evidence",
            column: "S3Key",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_complaint_drafts_CitizenId",
            table: "complaint_drafts",
            column: "CitizenId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_complaint_drafts_DepartmentId_WardId",
            table: "complaint_drafts",
            columns: new[] { "DepartmentId", "WardId" });

        migrationBuilder.CreateIndex(
            name: "IX_complaint_drafts_WardId",
            table: "complaint_drafts",
            column: "WardId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "complaint_draft_evidence");
        migrationBuilder.DropTable(name: "complaint_drafts");
    }
}
