using UnityEngine;

namespace XGame.Core
{
    /// <summary>
    /// 日志工具类
    /// </summary>
    public static class Log
    {
        /// <summary>
        /// 打印 Debug 级别日志
        /// </summary>
        /// <param name="message"></param>
        [HideInCallstack]
        public static void Debug(string message)
        {
#if !DISABLE_DEBUG_LOG
            UnityEngine.Debug.Log("[Debug] " + message);
#endif
        }

        /// <summary>
        /// 打印 Info 级别日志
        /// </summary>
        /// <param name="message"></param>
        [HideInCallstack]
        public static void Info(string message)
        {
            UnityEngine.Debug.Log(message);
        }

        /// <summary>
        /// 打印 Warning 级别日志
        /// </summary>
        /// <param name="message"></param>
        [HideInCallstack]
        public static void Warning(string message)
        {
            UnityEngine.Debug.LogWarning(message);
        }

        /// <summary>
        /// 打印 Error 级别日志
        /// </summary>
        /// <param name="message"></param>
        [HideInCallstack]
        public static void Error(string message)
        {
            UnityEngine.Debug.LogError(message);
        }

        /// <summary>
        /// 打印 Fatal 级别日志
        /// </summary>
        /// <param name="message"></param>
        /// <remarks>
        /// 建议在发生致命错误，即可能导致游戏崩溃的情况时调用此方法，可以尝试重启进程或游戏框架来修复。
        /// </remarks>
        [HideInCallstack]
        public static void Fatal(string message)
        {
            UnityEngine.Debug.LogError("[Fatal] " + message);
        }
    }
}