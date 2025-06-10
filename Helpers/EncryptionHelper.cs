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

    // ✅ تشفير رقم صحيح (int)
    public string EncryptInt(int number)
    {
        string plainText = number.ToString();
        return Encrypt(plainText);
    }

    // ✅ فك التشفير لاسترجاع الرقم الصحيح (int)
    public int DecryptInt(string cipherText)
    {
        string decryptedText = Decrypt(cipherText);
        return int.Parse(decryptedText);
    }

    // 🔐 التشفير الأساسي للنص
    private string Encrypt(string plainText)
    {
        using Aes aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(_encryptionKey);
        aes.GenerateIV(); // توليد IV عشوائي

        using var ms = new MemoryStream();
        ms.Write(aes.IV, 0, aes.IV.Length); // تخزين IV في البداية

        using var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }

        // تحويل الناتج إلى Base64 URL-safe
        string base64 = Convert.ToBase64String(ms.ToArray());
        string urlSafe = base64.Replace("+", "-").Replace("/", "_").TrimEnd('=');
        return urlSafe;
    }

    // 🔓 فك التشفير
    private string Decrypt(string cipherText)
    {
        // إعادة تحويل Base64 URL-safe إلى Base64 عادي
        string base64 = cipherText.Replace("-", "+").Replace("_", "/");

        // إعادة البادئات الناقصة =
        int padding = 4 - (base64.Length % 4);
        if (padding < 4)
            base64 += new string('=', padding);

        byte[] fullCipher = Convert.FromBase64String(base64);

        using Aes aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(_encryptionKey);

        // استخراج IV
        byte[] iv = new byte[16];
        Array.Copy(fullCipher, 0, iv, 0, iv.Length);
        aes.IV = iv;

        // استخراج البيانات المشفرة
        byte[] cipher = new byte[fullCipher.Length - iv.Length];
        Array.Copy(fullCipher, iv.Length, cipher, 0, cipher.Length);

        using var ms = new MemoryStream(cipher);
        using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        return sr.ReadToEnd();
    }
}
