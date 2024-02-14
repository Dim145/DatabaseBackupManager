using System.Security.Cryptography;

namespace DatabaseBackupManager.Services;

internal static class EncryptedStringConverter
{
    private static string DefaultEncryptKey { get; set; }
    
    private static Aes Aes { get; } = Aes.Create();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="text">text to encrypt</param>
    /// <param name="keyString">key to use this time only</param>
    /// <returns>Encrypted string in base64 string</returns>
    /// <exception cref="ArgumentNullException">When keyString is null and no default key found in appsettings</exception>
    internal static string Encrypt(this string text, string keyString = null)
    {
        if (string.IsNullOrWhiteSpace(keyString))
            keyString = DefaultEncryptKey;
        else
            DefaultEncryptKey = keyString;

        if (string.IsNullOrWhiteSpace(keyString))
            throw new ArgumentNullException(nameof(keyString), "keyString and default_encrypt_key in appsettings.json is null");

        var key = Convert.FromBase64String(keyString);
        
        using var encryptor = Aes.CreateEncryptor(key, Aes.IV);
        using var msEncrypt = new MemoryStream();
        
        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            using (var swEncrypt = new StreamWriter(csEncrypt))
            {
                swEncrypt.Write(text);
            }

        var iv = Aes.IV;

        var decryptedContent = msEncrypt.ToArray();

        var result = new byte[iv.Length + decryptedContent.Length];

        Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
        Buffer.BlockCopy(decryptedContent, 0, result, iv.Length, decryptedContent.Length);

        return Convert.ToBase64String(result);
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cipherText">text to decrypt in base64</param>
    /// <param name="keyString">key to use this time only</param>
    /// <returns>Decrypted string</returns>
    /// <exception cref="ArgumentNullException">When keyString is null and no default key found in appsettings</exception>
    internal static string Decrypt(this string cipherText, string keyString = null)
    {
        if (string.IsNullOrWhiteSpace(keyString))
            keyString = DefaultEncryptKey;
        else
            DefaultEncryptKey = keyString;
        
        if (string.IsNullOrWhiteSpace(keyString))
            throw new ArgumentNullException(nameof(keyString), "keyString and default_encrypt_key in appsettings.json is null");

        var fullCipher = Convert.FromBase64String(cipherText);

        var iv = new byte[Aes.IV.Length];
        var cipher = new byte[fullCipher.Length - iv.Length];

        Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, cipher.Length);
        var key = Convert.FromBase64String(keyString);

        using var decryptor = Aes.CreateDecryptor(key, iv);
        using var msDecrypt = new MemoryStream(cipher);
        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using var srDecrypt = new StreamReader(csDecrypt);
        
        var result = srDecrypt.ReadToEnd();

        return result;
    }

    internal static string GetKey(IConfiguration conf)
    {
        var key = Environment.GetEnvironmentVariable("password_secret_key");

        if (!string.IsNullOrWhiteSpace(key))
            return key;
        
        key = GenerateKey();
        
        Console.WriteLine($"\n\n\n\nYour key is '{key}' please save it in a safe place and set it in the environment variable password_secret_key\n\n\n\n\n");
            
        try
        {
            Environment.SetEnvironmentVariable("password_secret_key", key);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            
            conf["password_secret_key"] = key;
        }
        
        return key;
    }

    /// <summary>
    ///  Generate key for AES (256 bits) <br/>
    /// New Key not saved and not replace default key !
    /// </summary>
    /// <returns>New key in base64</returns>
    internal static string GenerateKey()
    {
        var aes = Aes.Create();
        
        aes.GenerateKey();
        
        return Convert.ToBase64String(aes.Key);
    }
}