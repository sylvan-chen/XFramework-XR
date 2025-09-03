using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace XGame.Core
{
    /// <summary>
    /// 总启动器
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("XFramework/Game Launcher")]
    internal sealed class GameLauncher : MonoSingletonPersistent<GameLauncher>
    {
        private const int FOUNDATION_COMPONENT_PRIORITY = 0;
        private const int KERNEL_COMPONENT_PRIORITY = 100;
        private const int SYSTEM_COMPONENT_PRIORITY = 200;
        private const int GAME_COMPONENT_PRIORITY = 300;

        [Header("Framework Component Settings")]
        [SerializeField] private TableManagerSetting _tableManagerSetting;
        [SerializeField] private AssetManagerSetting _assetManagerSetting;
        [SerializeField] private UIManagerSetting _uiManagerSetting;
        [SerializeField] private ProcedureManagerSetting _procedureManagerSetting;

        private readonly List<FrameworkComponent> _cachedComponents = new();
        private readonly Dictionary<Type, FrameworkComponent> _componentMap = new();

        public bool IsInitialized { get; private set; } = false;

        protected override void Awake()
        {
            base.Awake();

            gameObject.name ??= "[GameLauncher]";
            if (Camera.main != null) DontDestroyOnLoad(Camera.main.gameObject);
        }

        private void Start()
        {
            LoadFoundationComponents();
            LoadKernelComponents();
            LoadSystemComponents();
            LoadGameComponents();

            _cachedComponents.Sort((a, b) => a.Priority.CompareTo(b.Priority));

            InitComponents().Forget();
            EnterGame().Forget();
        }

        private async UniTaskVoid InitComponents()
        {
            foreach (FrameworkComponent component in _cachedComponents)
            {
                component.Init();
                await UniTask.NextFrame(); // 等待一帧让组件完成初始化
            }

            IsInitialized = true;
        }

        private async UniTaskVoid EnterGame()
        {
            await UniTask.WaitUntil(() => IsInitialized);

            var procedureManager = GetFrameworkComponent<ProcedureManager>();
            procedureManager.StartProcedure();
        }

        private void Update()
        {
            foreach (FrameworkComponent component in _cachedComponents)
            {
                component.Update(Time.deltaTime, Time.unscaledDeltaTime);
            }
        }

        private void OnDestroy()
        {
            ShutdownFramework();
        }

        public T GetFrameworkComponent<T>() where T : FrameworkComponent
        {
            return GetFrameworkComponent(typeof(T)) as T;
        }

        public FrameworkComponent GetFrameworkComponent(Type type)
        {
            _componentMap.TryGetValue(type, out var component);

            if (component == null)
                Log.Error($"[GameLauncher] Framework component not found: {type.Name}");

            return component;
        }

        private void LoadFoundationComponents()
        {
            var gameSetting = new GameSetting() { Priority = FOUNDATION_COMPONENT_PRIORITY };
            CacheComponentInstance(typeof(GameSetting), gameSetting);
        }

        private void LoadKernelComponents()
        {
            var poolManager = new PoolManager() { Priority = KERNEL_COMPONENT_PRIORITY };
            CacheComponentInstance(typeof(PoolManager), poolManager);

            var configManager = new TableManager(_tableManagerSetting) { Priority = KERNEL_COMPONENT_PRIORITY };
            CacheComponentInstance(typeof(TableManager), configManager);

            var assetManager = new AssetManager(_assetManagerSetting) { Priority = KERNEL_COMPONENT_PRIORITY };
            CacheComponentInstance(typeof(AssetManager), assetManager);

            var eventManager = new EventManager() { Priority = KERNEL_COMPONENT_PRIORITY };
            CacheComponentInstance(typeof(EventManager), eventManager);

            var stateMachineManager = new StateMachineManager() { Priority = KERNEL_COMPONENT_PRIORITY };
            CacheComponentInstance(typeof(StateMachineManager), stateMachineManager);
        }

        private void LoadSystemComponents()
        {
            var uiManager = new UIManager(_uiManagerSetting) { Priority = SYSTEM_COMPONENT_PRIORITY };
            CacheComponentInstance(typeof(UIManager), uiManager);
        }

        private void LoadGameComponents()
        {
            var procedureManager = new ProcedureManager(_procedureManagerSetting) { Priority = GAME_COMPONENT_PRIORITY };
            CacheComponentInstance(typeof(ProcedureManager), procedureManager);
        }

        private void CacheComponentInstance(Type componentType, FrameworkComponent instance)
        {
            if (_cachedComponents.Contains(instance) || _componentMap.ContainsKey(componentType))
            {
                Log.Warning($"[GameLauncher] Duplicate component cache attempted: {componentType.Name}");
                return;
            }

            _cachedComponents.Add(instance);
            _componentMap[componentType] = instance;
        }

        /// <summary>
        /// 关闭并清理框架
        /// </summary>
        private void ShutdownFramework()
        {
            Log.Debug("[GameLauncher] Shutdown XFramework...");
            _cachedComponents.Reverse();
            foreach (FrameworkComponent component in _cachedComponents)
            {
                component.Shutdown();
            }
            _cachedComponents.Clear();
            _componentMap.Clear();

            IsInitialized = false;
        }
    }
}