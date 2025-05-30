using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

public class EncryptionHelper
{
    private readonly string _encryptionKey;

    public EncryptionHelper(IConfiguration configuration)
    {
        _encryptionKey = configuration["EncryptionSettings:Key"];
        if (string.IsNullOrWhiteSpace(_encryptionKey) || _encryptionKey.Length != 32)
            throw new Exception("Encryption key must be 32 characters long.");
    }

    public string Encrypt(string plainText)
    {
        byte[] iv = new byte[16];
        using (Aes aes = Aes.Create())
        {
            aes.Key = Encoding.UTF8.GetBytes(_encryptionKey);
            aes.IV = iv;

            using MemoryStream ms = new MemoryStream();
            using CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
            using (StreamWriter sw = new StreamWriter(cs))
            {
                sw.Write(plainText);
            }

            return Convert.ToBase64String(ms.ToArray());
        }
    }

    public string Decrypt(string cipherText)
    {
        byte[] iv = new byte[16];
        byte[] buffer = Convert.FromBase64String(cipherText);

        using Aes aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(_encryptionKey);
        aes.IV = iv;

        using MemoryStream ms = new MemoryStream(buffer);
        using CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using StreamReader sr = new StreamReader(cs);
        return sr.ReadToEnd();
    }
}
