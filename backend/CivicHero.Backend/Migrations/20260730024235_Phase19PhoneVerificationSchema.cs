using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CivicHero.Backend.Migrations
{
    /// <inheritdoc />
    public partial class Phase19PhoneVerificationSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPhoneVerified",
                table: "users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSystemAccount",
                table: "users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedPhone",
                table: "users",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EmergencyRequestedAt",
                table: "complaints",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyReviewStatus",
                table: "complaints",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "IsAnonymous",
                table: "complaints",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PossibleEmergency",
                table: "complaints",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "anonymous_complaint_access",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ComplaintId = table.Column<long>(type: "bigint", nullable: false),
                    TrackingTokenHash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TrackingExpiresAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    ContactEmailProtected = table.Column<string>(type: "varchar(2048)", maxLength: 2048, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContactPhoneProtected = table.Column<string>(type: "varchar(2048)", maxLength: 2048, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContactHint = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CaptchaProvider = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SubmissionIpHash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CaptchaScore = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: true),
                    LastAccessedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    AccessCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_anonymous_complaint_access", x => x.Id);
                    table.ForeignKey(
                        name: "FK_anonymous_complaint_access_complaints_ComplaintId",
                        column: x => x.ComplaintId,
                        principalTable: "complaints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "complaint_comments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ComplaintId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Body = table.Column<string>(type: "varchar(1500)", maxLength: 1500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Visibility = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ModerationStatus = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ModerationReason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ModeratedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModeratedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_complaint_comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_complaint_comments_complaints_ComplaintId",
                        column: x => x.ComplaintId,
                        principalTable: "complaints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_complaint_comments_users_ModeratedByUserId",
                        column: x => x.ModeratedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_complaint_comments_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "complaint_emergency_reviews",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ComplaintId = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReporterReason = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OriginalPriority = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConfirmedPriority = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReviewedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    DecisionReason = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_complaint_emergency_reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_complaint_emergency_reviews_complaints_ComplaintId",
                        column: x => x.ComplaintId,
                        principalTable: "complaints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_complaint_emergency_reviews_users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "phone_otp_challenges",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    PhoneNumber = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Purpose = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CodeHash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    FailedAttempts = table.Column<int>(type: "int", nullable: false),
                    MaximumAttempts = table.Column<int>(type: "int", nullable: false),
                    RequestedIpHash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProviderMessageId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_phone_otp_challenges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_phone_otp_challenges_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "visual_verification_analyses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ComplaintId = table.Column<long>(type: "bigint", nullable: false),
                    Provider = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Model = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Verdict = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CompletionScore = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    ImageQualityScore = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    ManipulationRiskScore = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    OverallConfidence = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    Reasoning = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ObservationsJson = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BeforeImageIdsJson = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AfterImageIdsJson = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EvidenceFingerprint = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RawResponse = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequiresHumanReview = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    HumanDecision = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HumanNotes = table.Column<string>(type: "varchar(1500)", maxLength: 1500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReviewedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_visual_verification_analyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_visual_verification_analyses_complaints_ComplaintId",
                        column: x => x.ComplaintId,
                        principalTable: "complaints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_visual_verification_analyses_users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_users_IsSystemAccount",
                table: "users",
                column: "IsSystemAccount");

            migrationBuilder.CreateIndex(
                name: "IX_users_NormalizedPhone",
                table: "users",
                column: "NormalizedPhone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_complaints_EmergencyReviewStatus_Priority",
                table: "complaints",
                columns: new[] { "EmergencyReviewStatus", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_complaints_IsAnonymous_CreatedAt",
                table: "complaints",
                columns: new[] { "IsAnonymous", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_anonymous_complaint_access_ComplaintId",
                table: "anonymous_complaint_access",
                column: "ComplaintId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_anonymous_complaint_access_SubmissionIpHash_CreatedAt",
                table: "anonymous_complaint_access",
                columns: new[] { "SubmissionIpHash", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_anonymous_complaint_access_TrackingExpiresAt",
                table: "anonymous_complaint_access",
                column: "TrackingExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_anonymous_complaint_access_TrackingTokenHash",
                table: "anonymous_complaint_access",
                column: "TrackingTokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_complaint_comments_ComplaintId_CreatedAt",
                table: "complaint_comments",
                columns: new[] { "ComplaintId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_complaint_comments_ModeratedByUserId",
                table: "complaint_comments",
                column: "ModeratedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_complaint_comments_UserId",
                table: "complaint_comments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_complaint_emergency_reviews_ComplaintId",
                table: "complaint_emergency_reviews",
                column: "ComplaintId");

            migrationBuilder.CreateIndex(
                name: "IX_complaint_emergency_reviews_ReviewedByUserId",
                table: "complaint_emergency_reviews",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_complaint_emergency_reviews_Status_CreatedAt",
                table: "complaint_emergency_reviews",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_phone_otp_challenges_ExpiresAt",
                table: "phone_otp_challenges",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_phone_otp_challenges_PhoneNumber_Purpose_CreatedAt",
                table: "phone_otp_challenges",
                columns: new[] { "PhoneNumber", "Purpose", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_phone_otp_challenges_UserId",
                table: "phone_otp_challenges",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_visual_verification_analyses_ComplaintId_CreatedAt",
                table: "visual_verification_analyses",
                columns: new[] { "ComplaintId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_visual_verification_analyses_ComplaintId_EvidenceFingerprint",
                table: "visual_verification_analyses",
                columns: new[] { "ComplaintId", "EvidenceFingerprint" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_visual_verification_analyses_RequiresHumanReview_ReviewedAt",
                table: "visual_verification_analyses",
                columns: new[] { "RequiresHumanReview", "ReviewedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_visual_verification_analyses_ReviewedByUserId",
                table: "visual_verification_analyses",
                column: "ReviewedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "anonymous_complaint_access");

            migrationBuilder.DropTable(
                name: "complaint_comments");

            migrationBuilder.DropTable(
                name: "complaint_emergency_reviews");

            migrationBuilder.DropTable(
                name: "phone_otp_challenges");

            migrationBuilder.DropTable(
                name: "visual_verification_analyses");

            migrationBuilder.DropIndex(
                name: "IX_users_IsSystemAccount",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_NormalizedPhone",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_complaints_EmergencyReviewStatus_Priority",
                table: "complaints");

            migrationBuilder.DropIndex(
                name: "IX_complaints_IsAnonymous_CreatedAt",
                table: "complaints");

            migrationBuilder.DropColumn(
                name: "IsPhoneVerified",
                table: "users");

            migrationBuilder.DropColumn(
                name: "IsSystemAccount",
                table: "users");

            migrationBuilder.DropColumn(
                name: "NormalizedPhone",
                table: "users");

            migrationBuilder.DropColumn(
                name: "EmergencyRequestedAt",
                table: "complaints");

            migrationBuilder.DropColumn(
                name: "EmergencyReviewStatus",
                table: "complaints");

            migrationBuilder.DropColumn(
                name: "IsAnonymous",
                table: "complaints");

            migrationBuilder.DropColumn(
                name: "PossibleEmergency",
                table: "complaints");
        }
    }
}
