using BCrypt.Net;

// Generate BCrypt hash for Admin@123 with work factor 11
string password = "Admin@123";
string hash = BCrypt.Net.BCrypt.HashPassword(password, 11);
Console.WriteLine($"Password: {password}");
Console.WriteLine($"Hash: {hash}");
Console.WriteLine($"Hash Length: {hash.Length}");
Console.WriteLine($"Verify: {BCrypt.Net.BCrypt.Verify(password, hash)}");
