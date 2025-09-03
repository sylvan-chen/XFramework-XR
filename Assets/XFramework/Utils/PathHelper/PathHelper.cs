using System;
using System.IO;
using UnityEngine;

namespace XGame.Core
{
    public static class PathHelper
    {
        public static bool IsFileExists(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            return File.Exists(path);
        }

        /// <summary>
        /// 获取规范化的路径
        /// </summary>
        /// <remarks>
        /// 将路径中的 '\\' 全部替换为 '/'。
        /// </remarks>
        public static string GetRegularPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            path = path.Replace(@"\\", "/");
            path = path.Replace(@"\", "/");
            path = path.Replace("//", "/");
            return path;
        }

        public static string RemoveExtension(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            int lastDotIndex = path.LastIndexOf('.');
            if (lastDotIndex == -1)
            {
                return path;
            }
            else
            {
                return path.Substring(0, lastDotIndex);
            }
        }

        public static string GetExtension(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            return Path.GetExtension(path);
        }

        public static bool HasExtension(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            return Path.HasExtension(path);
        }

        public static string GetFileName(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            path = GetRegularPath(path);
            return Path.GetFileName(path);
        }

        public static string GetFileNameWithoutExtension(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            path = GetRegularPath(path);
            return Path.GetFileNameWithoutExtension(path);
        }

        public static string Combine(string[] paths)
        {
            return Path.Combine(paths);
        }

        public static string Combine(string path1, string path2)
        {
            return Path.Combine(path1, path2);
        }

        public static string Combine(string path1, string path2, string path3)
        {
            return Path.Combine(path1, path2, path3);
        }

        public static string Combine(string path1, string path2, string path3, string path4)
        {
            return Path.Combine(path1, path2, path3, path4);
        }

        /// <summary>
        /// 获取 WWW 文件格式的路径（'file://' 前缀）
        /// </summary>
        public static string ConvertToWWWFilePath(string path)
        {
            string prefix;
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                    prefix = "file:///";
                    break;
                case RuntimePlatform.Android:
                    prefix = "jar:file://";
                    break;
                case RuntimePlatform.IPhonePlayer:
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.OSXPlayer:
                    prefix = "file://";
                    break;
                default:
                    throw new NotImplementedException();
            }
            return ConvertToWWWPathInternal(path, prefix);
        }

        /// <summary>
        /// 获取 HTTP 格式的路径（'http://' 前缀）
        /// </summary>
        public static string ConvertToHttpPath(string path)
        {
            return ConvertToWWWPathInternal(path, "http://");
        }

        /// <summary>
        /// 获取 HTTPS 格式的路径（'http://' 前缀）
        /// </summary>
        public static string ConvertToHttpsPath(string path)
        {
            return ConvertToWWWPathInternal(path, "https://");
        }

        private static string ConvertToWWWPathInternal(string path, string prefix)
        {
            string regularPath = GetRegularPath(path);
            if (regularPath == null)
            {
                return null;
            }

            if (regularPath.StartsWith(prefix))
            {
                return regularPath;
            }
            else
            {
                string fullPath = prefix + regularPath;
                // 去掉重复的斜杠
                return fullPath.Replace(prefix + "/", prefix);
            }
        }
    }
}