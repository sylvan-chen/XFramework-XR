using UnityEngine;

namespace XGame.Core
{
    /// <summary>
    /// 框架管理器入口
    /// </summary>
    public static class M
    {
        private static EventManager _eventManager;
        private static GameSetting _gameSetting;
        private static StateMachineManager _stateMachineManager;
        private static PoolManager _poolManager;
        private static ProcedureManager _procedureManager;
        private static AssetManager _assetManager;
        private static UIManager _uiManager;
        private static TableManager _tableManager;

        public static EventManager EventManager
        {
            get
            {
                _eventManager ??= GameLauncher.Instance.GetFrameworkComponent<EventManager>();
                if (_eventManager.IsShutDown)
                    Log.Error("[M] EventManager is already shutdown.");
                return _eventManager;
            }
        }

        public static GameSetting GameSetting
        {
            get
            {
                _gameSetting ??= GameLauncher.Instance.GetFrameworkComponent<GameSetting>();
                if (_gameSetting.IsShutDown)
                    Log.Error("[M] GameSetting is already shutdown.");
                return _gameSetting;
            }
        }

        public static StateMachineManager StateMachineManager
        {
            get
            {
                _stateMachineManager ??= GameLauncher.Instance.GetFrameworkComponent<StateMachineManager>();
                if (_stateMachineManager.IsShutDown)
                    Log.Error("[M] StateMachineManager is already shutdown.");
                return _stateMachineManager;
            }
        }

        public static PoolManager PoolManager
        {
            get
            {
                _poolManager ??= GameLauncher.Instance.GetFrameworkComponent<PoolManager>();
                if (_poolManager.IsShutDown)
                    Log.Error("[M] PoolManager is already shutdown.");
                return _poolManager;
            }
        }

        public static ProcedureManager ProcedureManager
        {
            get
            {
                _procedureManager ??= GameLauncher.Instance.GetFrameworkComponent<ProcedureManager>();
                if (_procedureManager.IsShutDown)
                    Log.Error("[M] ProcedureManager is already shutdown.");
                return _procedureManager;
            }
        }

        public static AssetManager AssetManager
        {
            get
            {
                _assetManager ??= GameLauncher.Instance.GetFrameworkComponent<AssetManager>();
                if (_assetManager.IsShutDown)
                    Log.Error("[M] AssetManager is already shutdown.");
                return _assetManager;
            }
        }

        public static UIManager UIManager
        {
            get
            {
                _uiManager ??= GameLauncher.Instance.GetFrameworkComponent<UIManager>();
                if (_uiManager.IsShutDown)
                    Log.Error("[M] UIManager is already shutdown.");
                return _uiManager;
            }
        }

        public static TableManager TableManager
        {
            get
            {
                _tableManager ??= GameLauncher.Instance.GetFrameworkComponent<TableManager>();
                if (_tableManager.IsShutDown)
                    Log.Error("[M] TableManager is already shutdown.");
                return _tableManager;
            }
        }

        /// <summary>
        /// 退出游戏程序
        /// </summary>
        public static void ShutdownGame()
        {
            Log.Info("[M] Shutdown game...");
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}