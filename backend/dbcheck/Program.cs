using MySqlConnector;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        var cs = "Server=civichero-db.c36qeok2m7y1.ap-south-1.rds.amazonaws.com;Port=3306;Database=civicherodb;Uid=admin;Pwd=M256#b489;SslMode=Preferred;";
        await using var conn = new MySqlConnection(cs);
        await conn.OpenAsync();

        Console.WriteLine("ALL TABLES:");
        var existingTables = new List<string>();
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA='civicherodb' ORDER BY TABLE_NAME;";
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var tableName = reader.GetString(0);
                existingTables.Add(tableName);
                Console.WriteLine(tableName);
            }
        }

        Console.WriteLine();
        Console.WriteLine("EXPECTED TABLES MISSING FROM CURRENT DB:");
        var expectedTables = new[]
        {
            "ai_fraud_analyses",
            "chat_messages",
            "chat_sessions",
            "complaints",
            "complaint_assignments",
            "complaint_images",
            "complaint_progress_updates",
            "complaint_timelines",
            "complaint_verifications",
            "complaint_votes",
            "contractors",
            "departments",
            "dispute_audit_logs",
            "notifications",
            "notification_preferences",
            "redemptions",
            "reputation_logs",
            "reward_catalog",
            "users",
            "wards",
            "__EFMigrationsHistory"
        };

        foreach (var expected in expectedTables)
        {
            if (!existingTables.Contains(expected))
            {
                Console.WriteLine(expected);
            }
        }

        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("LEGACY TABLE COUNTS:");
        var legacyTables = new[]
        {
            "Departments",
            "departments",
            "Users",
            "users",
            "Wards",
            "wards",
            "DisputeAuditLogs",
            "dispute_audit_logs"
        };

        await using (var cmd = conn.CreateCommand())
        {
            foreach (var table in legacyTables)
            {
                cmd.CommandText = $"SELECT COUNT(*) FROM `{table}`;";
                try
                {
                    var count = await cmd.ExecuteScalarAsync();
                    Console.WriteLine($"{table}: {count}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{table}: ERROR ({ex.Message})");
                }
            }
        }

        Console.WriteLine();
        Console.WriteLine("MIGRATION HISTORY:");
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA='civicherodb' AND TABLE_NAME='__EFMigrationsHistory';";
            var exists = await cmd.ExecuteScalarAsync();
            if (exists is null)
            {
                Console.WriteLine("__EFMigrationsHistory does not exist.");
            }
            else
            {
                cmd.CommandText = "SELECT MigrationId, ProductVersion FROM __EFMigrationsHistory ORDER BY MigrationId;";
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    Console.WriteLine($"{reader.GetString(0)} | {reader.GetString(1)}");
                }
            }
        }
    }
}
