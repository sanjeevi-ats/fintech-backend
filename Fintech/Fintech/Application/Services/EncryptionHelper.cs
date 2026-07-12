using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Fintech.Application.Services;

public static class EncryptionHelper
{
    // In a real app, this should come from configuration/key vault
    private static readonly byte[] Key = Encoding.UTF8.GetBytes("12345678901234567890123456789012");
    private static readonly byte[] Iv = Encoding.UTF8.GetBytes("1234567890123456");

    public static string Encrypt(string plainText)
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

    public static string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return cipherText;

        try
        {
            using var aes = Aes.Create();
            aes.Key = Key;
            aes.IV = Iv;

            var buffer = Convert.FromBase64String(cipherText);
            using var ms = new MemoryStream(buffer);
            var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);
            return sr.ReadToEnd();
        }
        catch
        {
            return cipherText;
        }
    }
}
