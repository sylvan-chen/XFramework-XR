using System;
using System.IO;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace XGame.Core
{
    public static class FileHelper
    {
        public static bool Exists(string path)
        {
            return File.Exists(path);
        }

        public static void Delete(string path)
        {
            File.Delete(path);
        }

        public static long GetFileSize(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return 0;
            }
            return new FileInfo(path).Length;
        }

        public static string ReadAllText(string path)
        {
            return ReadAllText(path, Encoding.UTF8);
        }

        public static string ReadAllText(string path, Encoding encoding)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("ReadAllText failed. Path is null or empty.");
            }
            if (!File.Exists(path))
            {
                throw new InvalidOperationException($"ReadAllText failed. File '{path}' not found.");
            }

            return File.ReadAllText(path, encoding);
        }

        public static async UniTask<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default)
        {
            return await ReadAllTextAsync(path, Encoding.UTF8, cancellationToken);
        }

        public static async UniTask<string> ReadAllTextAsync(string path, Encoding encoding, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("ReadAllTextAsync failed. Path is null or empty.");
            }

            if (!File.Exists(path))
            {
                throw new InvalidOperationException($"ReadAllTextAsync failed. File '{path}' not found.");
            }

            string content;
            // Android平台需要使用UnityWebRequest读取
            if (Application.platform == RuntimePlatform.Android)
            {
                var result = await WebRequestHelper.WebGetBufferAsync(path);
                if (result.Status == WebRequestStatus.Success)
                {
                    content = result.DownloadBuffer.Text;
                }
                else
                {
                    Log.Error($"Failed to read file on Android by web request: {result.Error}");
                    return null;
                }
            }
            else
            {
                // 其他平台直接读取文件
                content = await File.ReadAllTextAsync(path, encoding, cancellationToken);
            }

            return content;
        }

        public static byte[] ReadAllBytes(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("ReadAllBytes failed. Path is null or empty.");
            }
            if (!File.Exists(path))
            {
                throw new InvalidOperationException($"ReadAllBytes failed. File '{path}' not found.");
            }

            return File.ReadAllBytes(path);
        }

        public static async UniTask<byte[]> ReadAllBytesAsync(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("ReadAllBytesAsync failed. Path is null or empty.");
            }
            if (!File.Exists(path))
            {
                throw new InvalidOperationException($"ReadAllBytesAsync failed. File '{path}' not found.");
            }

            return await File.ReadAllBytesAsync(path);
        }

        public static void WriteAllText(string path, string content)
        {
            WriteAllText(path, content, Encoding.UTF8);
        }

        public static void WriteAllText(string path, string content, Encoding encoding)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("WriteAllText failed. Path is null or empty.");
            }

            CreateFileDirectoryIfNotExist(path);
            byte[] bytes = encoding.GetBytes(content);
            File.WriteAllBytes(path, bytes);
        }

        public static async UniTask WriteAllTextAsync(string path, string content)
        {
            await WriteAllTextAsync(path, content, Encoding.UTF8);
        }

        public static async UniTask WriteAllTextAsync(string path, string content, Encoding encoding)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("WriteAllTextAsync failed. Path is null or empty.");
            }

            CreateFileDirectoryIfNotExist(path);
            byte[] bytes = encoding.GetBytes(content);
            await File.WriteAllBytesAsync(path, bytes);
        }

        public static void WriteAllBytes(string path, byte[] bytes)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("WriteAllBytes failed. Path is null or empty.");
            }

            CreateFileDirectoryIfNotExist(path);
            File.WriteAllBytes(path, bytes);
        }

        public static async UniTask WriteAllBytesAsync(string path, byte[] bytes)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("WriteAllBytesAsync failed. Path is null or empty.");
            }

            CreateFileDirectoryIfNotExist(path);
            await File.WriteAllBytesAsync(path, bytes);
        }

        /// <summary>
        /// 如果文件所在的目录不存在，则创建目录
        /// </summary>
        /// <param name="path">文件路径</param>
        public static void CreateFileDirectoryIfNotExist(string path)
        {
            string directory = Path.GetDirectoryName(path);
            CreateDirectoryIfNotExist(directory);
        }

        /// <summary>
        /// 如果目录不存在，则创建目录
        /// </summary>
        /// <param name="directory">目录路径</param>
        public static void CreateDirectoryIfNotExist(string directory)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }
}