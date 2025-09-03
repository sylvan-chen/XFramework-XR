using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace XGame.Core
{
    public static class HashHelper
    {
        public static string HashBytesToString(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }

        #region SHA1

        public static string StringSHA1(string str)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            return BytesSHA1(bytes);
        }

        public static string FileSHA1(string filePath)
        {
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return StreamSHA1(fs);
            }
        }

        public static string BytesSHA1(byte[] bytes)
        {
            var sha1 = HashAlgorithm.Create();
            byte[] hashBytes = sha1.ComputeHash(bytes);
            return HashBytesToString(hashBytes);
        }

        public static string StreamSHA1(Stream stream)
        {
            var sha1 = HashAlgorithm.Create();
            byte[] hashBytes = sha1.ComputeHash(stream);
            return HashBytesToString(hashBytes);
        }

        #endregion

        #region MD5

        public static string StringMD5(string str)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            return BytesMD5(bytes);
        }

        public static string FileMD5(string filePath)
        {
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return StreamMD5(fs);
            }
        }

        public static string BytesMD5(byte[] bytes)
        {
            var md5Porvider = new MD5CryptoServiceProvider();
            byte[] hashBytes = md5Porvider.ComputeHash(bytes);
            return HashBytesToString(hashBytes);
        }

        public static string StreamMD5(Stream stream)
        {
            var md5Porvider = new MD5CryptoServiceProvider();
            byte[] hashBytes = md5Porvider.ComputeHash(stream);
            return HashBytesToString(hashBytes);
        }

        #endregion

        #region CRC32

        // public static string StringCRC32(string str)
        // {
        //     byte[] bytes = Encoding.UTF8.GetBytes(str);
        //     return BytesCRC32(bytes);
        // }

        // public static string BytesCRC32(byte[] bytes)
        // {
        //     var crc32 = new CRC32Algorithm();
        //     uint crc = crc32.Compute(bytes);
        //     return crc.ToString();
        // }

        #endregion
    }
}