using CivicHero.Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CivicHero.Backend.Migrations;

[DbContext(typeof(CivicDbContext))]
[Migration("20260801183000_Change31FMultipleActiveSessions")]
public partial class Change31FMultipleActiveSessions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "user_sessions",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                UserId = table.Column<long>(type: "bigint", nullable: false),
                SessionId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                RefreshTokenHash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                AuthorizationVersion = table.Column<int>(type: "int", nullable: false),
                ExpiresAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                LastSeenAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                RevokedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                RevokedReason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                DeviceLabel = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                IpAddress = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                UserAgent = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_user_sessions", x => x.Id);
                table.ForeignKey(
                    name: "FK_user_sessions_users_UserId",
                    column: x => x.UserId,
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_user_sessions_RefreshTokenHash",
            table: "user_sessions",
            column: "RefreshTokenHash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_user_sessions_SessionId",
            table: "user_sessions",
            column: "SessionId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_user_sessions_UserId_AuthorizationVersion",
            table: "user_sessions",
            columns: new[] { "UserId", "AuthorizationVersion" });

        migrationBuilder.CreateIndex(
            name: "IX_user_sessions_UserId_RevokedAt_ExpiresAt",
            table: "user_sessions",
            columns: new[] { "UserId", "RevokedAt", "ExpiresAt" });

        // Preserve currently valid refresh sessions during deployment. Their next refresh
        // rotates the token and supplies normal device/IP metadata.
        migrationBuilder.Sql("""
            INSERT INTO user_sessions
                (UserId, SessionId, RefreshTokenHash, AuthorizationVersion, ExpiresAt, LastSeenAt,
                 RevokedAt, RevokedReason, DeviceLabel, IpAddress, UserAgent, CreatedAt, UpdatedAt)
            SELECT
                Id,
                LOWER(REPLACE(UUID(), '-', '')),
                RefreshTokenHash,
                AuthorizationVersion,
                RefreshTokenExpiresAt,
                COALESCE(LastLoginAt, RefreshTokenCreatedAt, UTC_TIMESTAMP(6)),
                NULL,
                NULL,
                'Migrated existing session',
                NULL,
                NULL,
                COALESCE(RefreshTokenCreatedAt, UTC_TIMESTAMP(6)),
                UTC_TIMESTAMP(6)
            FROM users
            WHERE RefreshTokenHash IS NOT NULL
              AND RefreshTokenExpiresAt IS NOT NULL
              AND RefreshTokenExpiresAt > UTC_TIMESTAMP(6);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "user_sessions");
    }
}
