
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace RobotServer
{
    public static class AesHelper
    {
        public static byte[] Encrypt(string plainText, byte[] key)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.GenerateIV();
                using (var ms = new MemoryStream())
                {
                    ms.Write(aes.IV, 0, aes.IV.Length);
                    using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    using (var sw = new StreamWriter(cs))
                    {
                        sw.Write(plainText);
                    }
                    return ms.ToArray();
                }
            }
        }

        public static string Decrypt(byte[] cipherWithIv, byte[] key)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                byte[] iv = new byte[16];
                Array.Copy(cipherWithIv, 0, iv, 0, iv.Length);
                aes.IV = iv;
                using (var ms = new MemoryStream())
                {
                    ms.Write(cipherWithIv, iv.Length, cipherWithIv.Length - iv.Length);
                    ms.Position = 0;
                    using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    using (var sr = new StreamReader(cs))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
        }
    }
}
