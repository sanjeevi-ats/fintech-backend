using System;
using System.IO;
using System.Threading.Tasks;
using Fintech.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fintech;

public class SeedDataExecutor
{
    public static async Task ExecuteSeedDataAsync(FinVedaDbContext dbContext)
    {
        try
        {
            var seedFilePath = "seed_data_v2.sql";
            if (!File.Exists(seedFilePath))
            {
                Console.WriteLine($"Seed file not found: {seedFilePath}");
                return;
            }

            var sqlContent = await File.ReadAllTextAsync(seedFilePath);
            
            // Split by statements (crude but works for this script)
            var statements = sqlContent.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
            
            int executed = 0;
            foreach (var statement in statements)
            {
                var trimmed = statement.Trim();
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("--"))
                    continue;

                try
                {
                    await dbContext.Database.ExecuteSqlRawAsync(trimmed + ";");
                    executed++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error executing statement: {ex.Message}");
                }
            }

            Console.WriteLine($"Executed {executed} SQL statements from seed file");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in ExecuteSeedDataAsync: {ex.Message}");
        }
    }
}
