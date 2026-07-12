using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Npgsql;

class FixCustomerEncryption
{
    private static readonly byte[] Key = Encoding.UTF8.GetBytes("12345678901234567890123456789012");
    private static readonly byte[] Iv = Encoding.UTF8.GetBytes("1234567890123456");

    static string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return plainText;

        using var aes = Aes.Create();
        aes.Key = Key;
        aes.IV = Iv;

        var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }
        return Convert.ToBase64String(ms.ToArray());
    }

    static async Task Main(string[] args)
    {
        var connectionString = "Host=localhost;Port=5432;Database=Fintech;Username=postgres;Password=Test123";

        // Generate encrypted values
        var aadhaar1 = Encrypt("123456789012");
        var pan1 = Encrypt("ABCDE1234F");
        var aadhaar2 = Encrypt("987654321098");
        var pan2 = Encrypt("XYZAB5678C");

        Console.WriteLine("========================================");
        Console.WriteLine("FIXING CUSTOMER ENCRYPTION");
        Console.WriteLine("========================================");
        Console.WriteLine();
        Console.WriteLine("Encrypted Values:");
        Console.WriteLine($"Aadhaar 1: {aadhaar1}");
        Console.WriteLine($"PAN 1: {pan1}");
        Console.WriteLine($"Aadhaar 2: {aadhaar2}");
        Console.WriteLine($"PAN 2: {pan2}");
        Console.WriteLine();

        try
        {
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();

            // Update customer 1 (Amit Sharma)
            var query1 = $@"
                UPDATE customers 
                SET aadhaar_encrypted = '{aadhaar1}', pan_encrypted = '{pan1}'
                WHERE id = '44444444-4444-4444-4444-444444444441'";
            
            await using var cmd1 = new NpgsqlCommand(query1, conn);
            var rows1 = await cmd1.ExecuteNonQueryAsync();
            Console.WriteLine($"[UPDATED] Amit Sharma - {rows1} row(s) updated");

            // Update customer 2 (Meera Patel)
            var query2 = $@"
                UPDATE customers 
                SET aadhaar_encrypted = '{aadhaar2}', pan_encrypted = '{pan2}'
                WHERE id = '44444444-4444-4444-4444-444444444442'";
            
            await using var cmd2 = new NpgsqlCommand(query2, conn);
            var rows2 = await cmd2.ExecuteNonQueryAsync();
            Console.WriteLine($"[UPDATED] Meera Patel - {rows2} row(s) updated");

            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.WriteLine("Customer encryption fixed successfully!");
            Console.WriteLine("========================================");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] {ex.Message}");
        }
    }
}
