using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CivicHero.Backend.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "departments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Code = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_departments", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "reward_catalog",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PointsCost = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StockQuantity = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reward_catalog", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "contractors",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: false),
                    CompanyName = table.Column<string>(type: "varchar(180)", maxLength: 180, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContactPerson = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Phone = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contractors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_contractors_departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "wards",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Code = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BoundaryNorth = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: false),
                    BoundarySouth = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: false),
                    BoundaryEast = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: false),
                    BoundaryWest = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_wards_departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Email = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PasswordHash = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FullName = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Phone = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Role = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    WardId = table.Column<long>(type: "bigint", nullable: true),
                    IsEmailVerified = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AuthorizationVersion = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    EmailVerificationTokenHash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmailVerificationTokenExpiresAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    RefreshTokenHash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RefreshTokenCreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    RefreshTokenExpiresAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    FailedLoginAttempts = table.Column<int>(type: "int", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    LastLoginAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_users_departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_users_wards_WardId",
                        column: x => x.WardId,
                        principalTable: "wards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "chat_sessions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    SessionId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    EndedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_chat_sessions_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "complaints",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CitizenId = table.Column<long>(type: "bigint", nullable: false),
                    AssignedOfficerId = table.Column<long>(type: "bigint", nullable: true),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: false),
                    WardId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Category = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Priority = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Latitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: false),
                    Address = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    ClosedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    AiTriagedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    AiSuggestedCategory = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AiConfidence = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: true),
                    AiRiskScore = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: true),
                    DuplicateOfComplaintId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_complaints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_complaints_departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_complaints_users_AssignedOfficerId",
                        column: x => x.AssignedOfficerId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_complaints_users_CitizenId",
                        column: x => x.CitizenId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_complaints_wards_WardId",
                        column: x => x.WardId,
                        principalTable: "wards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "notification_preferences",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    InAppEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    EmailEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SmsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ComplaintUpdates = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AssignmentUpdates = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    VerificationUpdates = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DisputeUpdates = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RewardUpdates = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SecurityAlerts = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_preferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notification_preferences_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Message = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReferenceType = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReferenceId = table.Column<long>(type: "bigint", nullable: true),
                    ActionUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsRead = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsArchived = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    ReadAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notifications_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "redemptions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    RewardCatalogId = table.Column<long>(type: "bigint", nullable: false),
                    PointsSpent = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RedemptionCode = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    FulfilledAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_redemptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_redemptions_reward_catalog_RewardCatalogId",
                        column: x => x.RewardCatalogId,
                        principalTable: "reward_catalog",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_redemptions_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "reputation_logs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    PointsDelta = table.Column<int>(type: "int", nullable: false),
                    RunningBalance = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReferenceId = table.Column<long>(type: "bigint", nullable: true),
                    ReferenceType = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reputation_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_reputation_logs_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "chat_messages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ChatSessionId = table.Column<long>(type: "bigint", nullable: false),
                    Sender = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Content = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SentAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_chat_messages_chat_sessions_ChatSessionId",
                        column: x => x.ChatSessionId,
                        principalTable: "chat_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ai_fraud_analyses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ComplaintId = table.Column<long>(type: "bigint", nullable: false),
                    FraudScore = table.Column<float>(type: "float", nullable: false),
                    Verdict = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Reasoning = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Provider = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Model = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequiresManualReview = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AnalyzedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_fraud_analyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ai_fraud_analyses_complaints_ComplaintId",
                        column: x => x.ComplaintId,
                        principalTable: "complaints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ai_triage_analyses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ComplaintId = table.Column<long>(type: "bigint", nullable: false),
                    Provider = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Model = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PredictedCategory = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PredictedDepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    ClassificationConfidence = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    DuplicateStatus = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DuplicateComplaintId = table.Column<long>(type: "bigint", nullable: true),
                    DuplicateScore = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    FraudVerdict = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FraudScore = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    PredictedPriority = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PriorityScore = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    RequiresManualReview = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Reasoning = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RawProviderResponse = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AnalyzedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    ReviewedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    ReviewDecision = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReviewNotes = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_triage_analyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ai_triage_analyses_complaints_ComplaintId",
                        column: x => x.ComplaintId,
                        principalTable: "complaints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "complaint_assignments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ComplaintId = table.Column<long>(type: "bigint", nullable: false),
                    OfficerId = table.Column<long>(type: "bigint", nullable: false),
                    AssignedById = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Reason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AssignedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    RespondedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    ReassignedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    AssignmentDueAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    ResolutionDueAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    IsCurrent = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_complaint_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_complaint_assignments_complaints_ComplaintId",
                        column: x => x.ComplaintId,
                        principalTable: "complaints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_complaint_assignments_users_AssignedById",
                        column: x => x.AssignedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_complaint_assignments_users_OfficerId",
                        column: x => x.OfficerId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "complaint_images",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ComplaintId = table.Column<long>(type: "bigint", nullable: false),
                    S3Key = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    S3Url = table.Column<string>(type: "varchar(2048)", maxLength: 2048, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FileName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    MimeType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsResolutionEvidence = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_complaint_images", x => x.Id);
                    table.ForeignKey(
                        name: "FK_complaint_images_complaints_ComplaintId",
                        column: x => x.ComplaintId,
                        principalTable: "complaints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "complaint_progress_updates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ComplaintId = table.Column<long>(type: "bigint", nullable: false),
                    OfficerId = table.Column<long>(type: "bigint", nullable: false),
                    Message = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProgressPercent = table.Column<int>(type: "int", nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_complaint_progress_updates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_complaint_progress_updates_complaints_ComplaintId",
                        column: x => x.ComplaintId,
                        principalTable: "complaints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_complaint_progress_updates_users_OfficerId",
                        column: x => x.OfficerId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "complaint_timelines",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ComplaintId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: true),
                    EventType = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_complaint_timelines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_complaint_timelines_complaints_ComplaintId",
                        column: x => x.ComplaintId,
                        principalTable: "complaints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_complaint_timelines_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "complaint_verifications",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ComplaintId = table.Column<long>(type: "bigint", nullable: false),
                    CitizenId = table.Column<long>(type: "bigint", nullable: false),
                    Decision = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Rating = table.Column<int>(type: "int", nullable: true),
                    Remarks = table.Column<string>(type: "varchar(1500)", maxLength: 1500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SubmittedLatitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    SubmittedLongitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    DistanceMetres = table.Column<double>(type: "double", nullable: true),
                    DueAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    ReminderSentAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    UserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_complaint_verifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_complaint_verifications_complaints_ComplaintId",
                        column: x => x.ComplaintId,
                        principalTable: "complaints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_complaint_verifications_users_CitizenId",
                        column: x => x.CitizenId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_complaint_verifications_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "complaint_votes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ComplaintId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    VotedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_complaint_votes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_complaint_votes_complaints_ComplaintId",
                        column: x => x.ComplaintId,
                        principalTable: "complaints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_complaint_votes_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "dispute_audit_logs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ComplaintId = table.Column<long>(type: "bigint", nullable: false),
                    RaisedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    ReviewedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CycleNumber = table.Column<int>(type: "int", nullable: false),
                    CitizenRemarks = table.Column<string>(type: "varchar(1500)", maxLength: 1500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SupervisorDecision = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SupervisorRemarks = table.Column<string>(type: "varchar(1500)", maxLength: 1500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AdminDecision = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AdminRemarks = table.Column<string>(type: "varchar(1500)", maxLength: 1500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RaisedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    AppealDeadline = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dispute_audit_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_dispute_audit_logs_complaints_ComplaintId",
                        column: x => x.ComplaintId,
                        principalTable: "complaints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_dispute_audit_logs_users_RaisedByUserId",
                        column: x => x.RaisedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dispute_audit_logs_users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "departments",
                columns: new[] { "Id", "Code", "CreatedAt", "DeletedAt", "Description", "IsActive", "IsDeleted", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1L, "ROADS", new DateTimeOffset(new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Road surface, pothole and footpath complaints.", true, false, "Roads & Potholes", new DateTimeOffset(new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 2L, "WASTE", new DateTimeOffset(new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Garbage collection and illegal dumping complaints.", true, false, "Solid Waste Management", new DateTimeOffset(new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 3L, "LIGHT", new DateTimeOffset(new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Streetlight and electrical public-infrastructure complaints.", true, false, "Street Lighting", new DateTimeOffset(new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 4L, "WATER", new DateTimeOffset(new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Water leakage, low pressure and supply complaints.", true, false, "Water Supply", new DateTimeOffset(new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 5L, "DRAIN", new DateTimeOffset(new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Blocked drains, sewage and flooding complaints.", true, false, "Drainage & Sewerage", new DateTimeOffset(new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 6L, "PARKS", new DateTimeOffset(new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Public garden and civic-space maintenance complaints.", true, false, "Parks & Public Spaces", new DateTimeOffset(new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                table: "reward_catalog",
                columns: new[] { "Id", "CreatedAt", "DeletedAt", "Description", "IsActive", "IsDeleted", "Name", "PointsCost", "StockQuantity", "Type", "UpdatedAt" },
                values: new object[,]
                {
                    { 1L, new DateTimeOffset(new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Download a personalised civic participation certificate.", true, false, "Civic Hero Certificate", 250, 100000, "Certificate", new DateTimeOffset(new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 2L, new DateTimeOffset(new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "A demonstration voucher for recognised civic participation.", true, false, "Public Transport Voucher", 500, 500, "Voucher", new DateTimeOffset(new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 3L, new DateTimeOffset(new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "A digital badge for citizens who help close multiple civic issues.", true, false, "Neighborhood Guardian Badge", 750, 100000, "Badge", new DateTimeOffset(new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 4L, new DateTimeOffset(new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "A demonstration eco-friendly merchandise reward.", true, false, "CivicHero Eco Kit", 1200, 100, "Merchandise", new DateTimeOffset(new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 5L, new DateTimeOffset(new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Premium certificate for sustained civic contribution.", true, false, "Community Champion Certificate", 2000, 100000, "Certificate", new DateTimeOffset(new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                table: "wards",
                columns: new[] { "Id", "BoundaryEast", "BoundaryNorth", "BoundarySouth", "BoundaryWest", "Code", "CreatedAt", "DeletedAt", "DepartmentId", "IsActive", "IsDeleted", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1L, 72.9900m, 19.1300m, 18.8900m, 72.7700m, "WARD-A", new DateTimeOffset(new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 1L, true, false, "Ward A", new DateTimeOffset(new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 2L, 73.0100m, 19.1500m, 18.9200m, 72.7900m, "WARD-B", new DateTimeOffset(new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 2L, true, false, "Ward B", new DateTimeOffset(new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 3L, 73.0300m, 19.1700m, 18.9400m, 72.8100m, "WARD-C", new DateTimeOffset(new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 3L, true, false, "Ward C", new DateTimeOffset(new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 4L, 73.0500m, 19.1900m, 18.9600m, 72.8300m, "WARD-D", new DateTimeOffset(new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 4L, true, false, "Ward D", new DateTimeOffset(new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 5L, 73.0700m, 19.2100m, 18.9800m, 72.8500m, "WARD-E", new DateTimeOffset(new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 5L, true, false, "Ward E", new DateTimeOffset(new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 6L, 73.0900m, 19.2300m, 19.0000m, 72.8700m, "WARD-F", new DateTimeOffset(new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, 6L, true, false, "Ward F", new DateTimeOffset(new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ai_fraud_analyses_ComplaintId_AnalyzedAt",
                table: "ai_fraud_analyses",
                columns: new[] { "ComplaintId", "AnalyzedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ai_fraud_analyses_RequiresManualReview",
                table: "ai_fraud_analyses",
                column: "RequiresManualReview");

            migrationBuilder.CreateIndex(
                name: "IX_ai_triage_analyses_ComplaintId_AnalyzedAt",
                table: "ai_triage_analyses",
                columns: new[] { "ComplaintId", "AnalyzedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ai_triage_analyses_RequiresManualReview_ReviewedAt",
                table: "ai_triage_analyses",
                columns: new[] { "RequiresManualReview", "ReviewedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_ChatSessionId_SentAt",
                table: "chat_messages",
                columns: new[] { "ChatSessionId", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_chat_sessions_SessionId",
                table: "chat_sessions",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_chat_sessions_UserId_StartedAt",
                table: "chat_sessions",
                columns: new[] { "UserId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_complaint_assignments_AssignedById",
                table: "complaint_assignments",
                column: "AssignedById");

            migrationBuilder.CreateIndex(
                name: "IX_complaint_assignments_AssignmentDueAt",
                table: "complaint_assignments",
                column: "AssignmentDueAt");

            migrationBuilder.CreateIndex(
                name: "IX_complaint_assignments_ComplaintId_IsCurrent",
                table: "complaint_assignments",
                columns: new[] { "ComplaintId", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "IX_complaint_assignments_OfficerId_Status_IsCurrent",
                table: "complaint_assignments",
                columns: new[] { "OfficerId", "Status", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "IX_complaint_assignments_ResolutionDueAt",
                table: "complaint_assignments",
                column: "ResolutionDueAt");

            migrationBuilder.CreateIndex(
                name: "IX_complaint_images_ComplaintId",
                table: "complaint_images",
                column: "ComplaintId");

            migrationBuilder.CreateIndex(
                name: "IX_complaint_images_S3Key",
                table: "complaint_images",
                column: "S3Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_complaint_progress_updates_ComplaintId_CreatedAt",
                table: "complaint_progress_updates",
                columns: new[] { "ComplaintId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_complaint_progress_updates_OfficerId_CreatedAt",
                table: "complaint_progress_updates",
                columns: new[] { "OfficerId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_complaint_timelines_ComplaintId_Timestamp",
                table: "complaint_timelines",
                columns: new[] { "ComplaintId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_complaint_timelines_UserId",
                table: "complaint_timelines",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_complaint_verifications_CitizenId",
                table: "complaint_verifications",
                column: "CitizenId");

            migrationBuilder.CreateIndex(
                name: "IX_complaint_verifications_ComplaintId",
                table: "complaint_verifications",
                column: "ComplaintId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_complaint_verifications_Decision_DueAt",
                table: "complaint_verifications",
                columns: new[] { "Decision", "DueAt" });

            migrationBuilder.CreateIndex(
                name: "IX_complaint_verifications_UserId",
                table: "complaint_verifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_complaint_votes_ComplaintId_UserId",
                table: "complaint_votes",
                columns: new[] { "ComplaintId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_complaint_votes_UserId",
                table: "complaint_votes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_complaints_AiTriagedAt",
                table: "complaints",
                column: "AiTriagedAt");

            migrationBuilder.CreateIndex(
                name: "IX_complaints_AssignedOfficerId",
                table: "complaints",
                column: "AssignedOfficerId");

            migrationBuilder.CreateIndex(
                name: "IX_complaints_Category_CreatedAt",
                table: "complaints",
                columns: new[] { "Category", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_complaints_CitizenId",
                table: "complaints",
                column: "CitizenId");

            migrationBuilder.CreateIndex(
                name: "IX_complaints_DepartmentId_WardId_Status",
                table: "complaints",
                columns: new[] { "DepartmentId", "WardId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_complaints_DuplicateOfComplaintId",
                table: "complaints",
                column: "DuplicateOfComplaintId");

            migrationBuilder.CreateIndex(
                name: "IX_complaints_Latitude_Longitude",
                table: "complaints",
                columns: new[] { "Latitude", "Longitude" });

            migrationBuilder.CreateIndex(
                name: "IX_complaints_WardId",
                table: "complaints",
                column: "WardId");

            migrationBuilder.CreateIndex(
                name: "IX_contractors_DepartmentId_Status",
                table: "contractors",
                columns: new[] { "DepartmentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_departments_Code",
                table: "departments",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dispute_audit_logs_ComplaintId_RaisedAt",
                table: "dispute_audit_logs",
                columns: new[] { "ComplaintId", "RaisedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_dispute_audit_logs_RaisedByUserId",
                table: "dispute_audit_logs",
                column: "RaisedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_audit_logs_ReviewedByUserId",
                table: "dispute_audit_logs",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_notification_preferences_UserId",
                table: "notification_preferences",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notifications_UserId_IsArchived_CreatedAt",
                table: "notifications",
                columns: new[] { "UserId", "IsArchived", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_notifications_UserId_IsRead_CreatedAt",
                table: "notifications",
                columns: new[] { "UserId", "IsRead", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_redemptions_RedemptionCode",
                table: "redemptions",
                column: "RedemptionCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_redemptions_RewardCatalogId",
                table: "redemptions",
                column: "RewardCatalogId");

            migrationBuilder.CreateIndex(
                name: "IX_redemptions_UserId_CreatedAt",
                table: "redemptions",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_reputation_logs_UserId_CreatedAt",
                table: "reputation_logs",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_reputation_logs_UserId_ReferenceType_ReferenceId_Reason",
                table: "reputation_logs",
                columns: new[] { "UserId", "ReferenceType", "ReferenceId", "Reason" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reward_catalog_IsActive_PointsCost",
                table: "reward_catalog",
                columns: new[] { "IsActive", "PointsCost" });

            migrationBuilder.CreateIndex(
                name: "IX_users_DepartmentId_WardId_Role",
                table: "users",
                columns: new[] { "DepartmentId", "WardId", "Role" });

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_RefreshTokenHash",
                table: "users",
                column: "RefreshTokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_WardId",
                table: "users",
                column: "WardId");

            migrationBuilder.CreateIndex(
                name: "IX_wards_Code",
                table: "wards",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wards_DepartmentId",
                table: "wards",
                column: "DepartmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_fraud_analyses");

            migrationBuilder.DropTable(
                name: "ai_triage_analyses");

            migrationBuilder.DropTable(
                name: "chat_messages");

            migrationBuilder.DropTable(
                name: "complaint_assignments");

            migrationBuilder.DropTable(
                name: "complaint_images");

            migrationBuilder.DropTable(
                name: "complaint_progress_updates");

            migrationBuilder.DropTable(
                name: "complaint_timelines");

            migrationBuilder.DropTable(
                name: "complaint_verifications");

            migrationBuilder.DropTable(
                name: "complaint_votes");

            migrationBuilder.DropTable(
                name: "contractors");

            migrationBuilder.DropTable(
                name: "dispute_audit_logs");

            migrationBuilder.DropTable(
                name: "notification_preferences");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "redemptions");

            migrationBuilder.DropTable(
                name: "reputation_logs");

            migrationBuilder.DropTable(
                name: "chat_sessions");

            migrationBuilder.DropTable(
                name: "complaints");

            migrationBuilder.DropTable(
                name: "reward_catalog");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "wards");

            migrationBuilder.DropTable(
                name: "departments");
        }
    }
}
