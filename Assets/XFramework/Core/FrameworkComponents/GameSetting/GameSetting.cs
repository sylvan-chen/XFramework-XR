using UnityEngine;

namespace XGame.Core
{
    /// <summary>
    /// 游戏设置管理器
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("XFramework/Game Setting")]
    public sealed class GameSetting : FrameworkComponent
    {
        private int _frameRate = 30;                  // 帧率
        private float _gameSpeed = 1f;                // 游戏速度
        private bool _allowRunInBackground = true;    // 允许后台运行
        private bool _neverSleep = false;             // 保持屏幕常亮
        private float _gameSpeedBeforePause = 1f;     // 游戏暂停前的游戏速度

        /// <summary>
        /// 帧率
        /// </summary>
        public int FrameRate
        {
            get { return _frameRate; }
            set { Application.targetFrameRate = _frameRate = value; }
        }

        /// <summary>
        /// 游戏速度
        /// </summary>
        public float GameSpeed
        {
            get { return _gameSpeed; }
            set { Time.timeScale = _gameSpeed = value >= 0f ? value : 0f; }
        }

        /// <summary>
        /// 允许后台运行
        /// </summary>
        public bool AllowRunInBackground
        {
            get { return _allowRunInBackground; }
            set { Application.runInBackground = _allowRunInBackground = value; }
        }

        /// <summary>
        /// 保持屏幕常亮
        /// </summary>
        public bool NeverSleep
        {
            get { return _neverSleep; }
            set
            {
                _neverSleep = value;
                Screen.sleepTimeout = value ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
            }
        }

        public bool IsGamePaused
        {
            get { return Time.timeScale == 0f; }
        }

        internal override void Init()
        {
            base.Init();
#if UNITY_5_3_OR_NEWER
            Application.targetFrameRate = _frameRate;
            Application.runInBackground = _allowRunInBackground;
            Time.timeScale = _gameSpeed;
            Screen.sleepTimeout = _neverSleep ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
#else
            Log.Fatal("XFrameworkUnity just support Unity 5.3 or later");
            Application.Quit();
#endif
#if UNITY_5_6_OR_NEWER
            Application.lowMemory += OnLowMemory;
#endif
        }

        internal override void Shutdown()
        {
            base.Shutdown();
#if UNITY_5_6_OR_NEWER
            Application.lowMemory -= OnLowMemory;
#endif
        }

        internal override void Update(float deltaTime, float unscaledDeltaTime)
        {
            base.Update(deltaTime, unscaledDeltaTime);
        }

        /// <summary>
        /// 暂停游戏
        /// </summary>
        public void PauseGame()
        {
            if (IsGamePaused)
            {
                return;
            }
            _gameSpeedBeforePause = _gameSpeed;
            GameSpeed = 0f;
        }

        /// <summary>
        /// 恢复游戏
        /// </summary>
        public void ResumeGame()
        {
            if (!IsGamePaused)
            {
                return;
            }
            GameSpeed = _gameSpeedBeforePause;
        }

        /// <summary>
        /// 重置游戏速度
        /// </summary>
        public void ResetGameSpeed()
        {
            GameSpeed = 1f;
        }

        /// <summary>
        /// 处理内存不足的情况
        /// </summary>
        private void OnLowMemory()
        {
            Log.Warning("[XFramework] [GameSetting] Low memory reported...");
            // TODO: 处理内存不足的情况
        }
    }
}