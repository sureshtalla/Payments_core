using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Payments_core.Helpers
{
    public static class BillAvenueCrypto
    {
        // Fixed IV: 00 01 02 ... 0F (as confirmed by BillAvenue)
        private static readonly byte[] FixedIV = new byte[]
        {
            0x00, 0x01, 0x02, 0x03,
            0x04, 0x05, 0x06, 0x07,
            0x08, 0x09, 0x0A, 0x0B,
            0x0C, 0x0D, 0x0E, 0x0F
        };

        // ----------------------------------------------------
        // ENCRYPT (ALL APIs)
        // PHP: openssl_encrypt(..., AES-128-CBC, md5(key), RAW, IV)
        // Output: HEX
        // ----------------------------------------------------
        public static string Encrypt(string plainText, string workingKey)
        {
            if (string.IsNullOrWhiteSpace(plainText))
                throw new Exception("Plain text is empty");

            byte[] keyBytes = MD5Hash(workingKey);
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

            using var aes = Aes.Create();
            aes.Key = keyBytes;
            aes.IV = FixedIV;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7; // PKCS5 equivalent

            using var encryptor = aes.CreateEncryptor();
            byte[] cipherBytes =
                encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            // PHP uses bin2hex
            return BytesToHex(cipherBytes);
        }

        // ----------------------------------------------------
        // DECRYPT (ALL APIs)
        // PHP: openssl_decrypt(hex, AES-128-CBC, md5(key), RAW, IV)
        // ----------------------------------------------------
        public static string Decrypt(string encryptedHex, string workingKey)
        {
            if (string.IsNullOrWhiteSpace(encryptedHex))
                throw new Exception("Encrypted text is empty");

            byte[] keyBytes = MD5Hash(workingKey);
            byte[] cipherBytes = HexToBytes(encryptedHex);

            using var aes = Aes.Create();
            aes.Key = keyBytes;
            aes.IV = FixedIV;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            byte[] plainBytes =
                decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

            return Encoding.UTF8.GetString(plainBytes);
        }

        // ----------------------------------------------------
        // Helpers
        // ----------------------------------------------------
        private static byte[] MD5Hash(string input)
        {
            using var md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            return hash; // 16 bytes
        }

        private static byte[] HexToBytes(string hex)
        {
            if (hex.Length % 2 != 0)
                throw new Exception("Invalid HEX string");

            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);

            return bytes;
        }

        private static string BytesToHex(byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
                sb.AppendFormat("{0:x2}", b);
            return sb.ToString();
        }

        // File: Payments_core/Helpers/BillAvenueCrypto.cs
        public static bool LooksLikeHex(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            input = input.Trim();

            // HEX must be even length and contain only 0-9, a-f, A-F
            if (input.Length % 2 != 0)
                return false;

            foreach (char c in input)
            {
                bool isHex =
                    (c >= '0' && c <= '9') ||
                    (c >= 'a' && c <= 'f') ||
                    (c >= 'A' && c <= 'F');

                if (!isHex)
                    return false;
            }

            return true;
        }

      
        // =====================================================
        // ENCRYPT FOR MDM (HEX KEY, NO MD5, NO XML DECLARATION)
        // =====================================================
        public static string EncryptForMdm(string plainText, string workingKeyHex)
        {
            if (string.IsNullOrWhiteSpace(plainText))
                throw new Exception("Plain text is empty");

            // ✅ HEX → BYTES (MUST BE 16 BYTES)
            byte[] keyBytes = HexToBytes(workingKeyHex);
            if (keyBytes.Length != 16)
                throw new Exception("Invalid WorkingKey length for MDM");

            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

            using var aes = Aes.Create();
            aes.Key = keyBytes;
            aes.IV = FixedIV;                  // 00 01 02 ... 0F
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            byte[] cipherBytes =
                encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            Console.WriteLine("MDM AES KEY LENGTH = " + keyBytes.Length);

            return BytesToHex(cipherBytes);
        }

        // =====================================================
        // DECRYPT FOR MDM (HEX KEY, NO MD5)
        // =====================================================
        public static string DecryptForMdm(string encryptedHex, string workingKeyHex)
        {
            if (string.IsNullOrWhiteSpace(encryptedHex))
                throw new Exception("Encrypted text is empty");

            byte[] keyBytes = HexToBytes(workingKeyHex);
            if (keyBytes.Length != 16)
                throw new Exception("Invalid WorkingKey length for MDM");

            byte[] cipherBytes = HexToBytes(encryptedHex);

            using var aes = Aes.Create();
            aes.Key = keyBytes;
            aes.IV = FixedIV;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            byte[] plainBytes =
                decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

            return Encoding.UTF8.GetString(plainBytes);
        }
    }
}