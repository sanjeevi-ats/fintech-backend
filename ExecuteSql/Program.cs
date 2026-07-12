using Npgsql;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var connectionString = "Server=localhost;Port=5432;Database=Fintech;User Id=postgres;Password=Test123;";

        try
        {
            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            
            // Truncate tables for a clean seed
            Console.WriteLine("Truncating all tables...");
            using (var cmd = new NpgsqlCommand(@"
                TRUNCATE TABLE audit_logs, receipts, installments, loan_cases, capital_accounts, partners, journal_lines, journal_entries, accounts, customers, loan_products, users, branches, day_ends, profit_distributions, code_sequences CASCADE;
            ", connection))
            {
                await cmd.ExecuteNonQueryAsync();
            }
            Console.WriteLine("✓ Tables truncated.");

            // Execute seed_data_v2.sql
            string seedFilePath = @"d:\Finance\Backend\Fintech\Fintech\Fintech\seed_data_v2.sql";
            if (!System.IO.File.Exists(seedFilePath))
            {
                Console.WriteLine($"✗ Seed file not found: {seedFilePath}");
                return 1;
            }

            Console.WriteLine("Reading seed data file...");
            var lines = await System.IO.File.ReadAllLinesAsync(seedFilePath);
            var cleanSql = string.Join("\n", lines.Where(l => !l.Trim().StartsWith("--")));
            var statements = cleanSql.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
            
            int executed = 0;
            foreach (var statement in statements)
            {
                var trimmed = statement.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;

                try
                {
                    using (var cmd = new NpgsqlCommand(trimmed + ";", connection))
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }
                    executed++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Error in statement:\n{trimmed}\nError: {ex.Message}\n");
                }
            }

            Console.WriteLine($"✓ Executed {executed} seed SQL statements successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error: {ex.Message}");
            return 1;
        }

        return 0;
    }
}
