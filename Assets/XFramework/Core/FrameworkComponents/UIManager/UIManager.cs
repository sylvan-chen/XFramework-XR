using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;
using XGame.Extensions;
using UnityEngine.Rendering;
using System.Linq;
using Cysharp.Threading.Tasks;

namespace XGame.Core
{
    [DisallowMultipleComponent]
    [AddComponentMenu("XFramework/UI Manager")]
    public sealed class UIManager : FrameworkComponent
    {
        private readonly UIManagerSetting _setting;

        private Camera _uiCamera;
        private Transform _uiRoot;
        private Transform _closedPanelRoot;
        private readonly Dictionary<string, UILayer> _layers = new();
        private readonly Dictionary<int, UIPanelBase> _loadedPanels = new();
        private readonly Dictionary<int, UIPanelBase> _openedPanels = new();
        private readonly List<AssetHandler> _assetHandlers = new();

        public UIManager(UIManagerSetting setting)
        {
            _setting = setting;
        }

        internal override void Init()
        {
            base.Init();

            CreateUIRoot();
            CreateUICamera();
            CreateUILayers();
        }

        internal override void Shutdown()
        {
            base.Shutdown();

            foreach (var handler in _assetHandlers)
            {
                handler.Release();
            }
            _assetHandlers.Clear();
            _loadedPanels.Clear();
            _openedPanels.Clear();
            _layers.Clear();

            if (_uiRoot != null)
            {
                Object.Destroy(_uiRoot.gameObject);
                _uiRoot = null;
                Log.Debug("UI root destroyed.");
            }
        }

        internal override void Update(float deltaTime, float unscaledDeltaTime)
        {
            base.Update(deltaTime, unscaledDeltaTime);
        }

        private void CreateUIRoot()
        {
            if (_uiRoot != null) return;

            _uiRoot = new GameObject(_setting.UIRootName).transform;
            _uiRoot.SetParent(null);
            _uiRoot.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            Object.DontDestroyOnLoad(_uiRoot.gameObject);
            _closedPanelRoot = new GameObject("[ClosedPanels]").transform;
            _closedPanelRoot.SetParent(_uiRoot, false);
        }

        private void CreateUICamera()
        {
            if (!_setting.UseSeparateUICamera)
            {
                _uiCamera = Camera.main;
                return;
            }

            // 排除主相机UI层级的渲染
            Camera.main.ExcludeLayer("UI");
            // 创建UI层级专用的摄像机
            var cameraObj = new GameObject("[UICamera]")
            {
                layer = LayerMask.NameToLayer("UI")
            };
            cameraObj.transform.SetParent(_uiRoot);
            cameraObj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            _uiCamera = cameraObj.AddComponent<Camera>();
            _uiCamera.clearFlags = CameraClearFlags.Depth;             // 使用深度清除
            _uiCamera.cullingMask = 1 << LayerMask.NameToLayer("UI");  // 只渲染UI层
            _uiCamera.orthographic = true;                             // 使用正交投影
            _uiCamera.depth = 100;                                     // 确保在其他摄像机之上
            _uiCamera.useOcclusionCulling = false;                     // 不需要遮挡剔除，节约性能

            // URP: 把UICamera添加到主相机的渲染堆栈中
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset)
            {
                var mainCamData = Camera.main.GetUniversalAdditionalCameraData();
                var uiCamData = _uiCamera.GetUniversalAdditionalCameraData();

                mainCamData.renderType = CameraRenderType.Base;
                uiCamData.renderType = CameraRenderType.Overlay;

                if (!mainCamData.cameraStack.Contains(_uiCamera))
                    mainCamData.cameraStack.Add(_uiCamera);

                uiCamData.renderShadows = false;
            }
        }

        private void CreateUILayers()
        {
            var layerSettings = _setting.LayerSettings;
            foreach (var layerSetting in layerSettings)
            {
                // 检查该层级是否已经存在
                if (_layers.ContainsKey(layerSetting.Name))
                {
                    Log.Warning($"[UIManager] UILayer '{layerSetting.Name}' already exists, skipping creation.");
                    continue;
                }

                // 创建新的 UILayer 对象
                var uiLayer = new UILayer(_uiRoot, _uiCamera, _setting.ReferenceResolution, layerSetting);
                _layers.Add(layerSetting.Name, uiLayer);
            }

            // 按照层级顺序排序节点
            var sortedLayers = _layers.Values.OrderBy(layer => layer.Canvas.sortingOrder).ToArray();
            for (int i = 0; i < sortedLayers.Length; i++)
            {
                sortedLayers[i].Transform.SetSiblingIndex(i);
            }

            // 设置所有层级的层级
            foreach (var layer in _layers.Values)
            {
                layer.Transform.gameObject.layer = LayerMask.NameToLayer("UI");
            }
        }

        public UILayer GetUILayer(string name)
        {
            if (_layers.TryGetValue(name, out var layer))
            {
                return layer;
            }
            Log.Error($"[UIManager] UILayer '{name}' not found.");
            return null;
        }

        public async UniTask<UIPanelBase> LoadPanelAsync(int id, string address, string parentLayer)
        {
            // 检查缓存
            if (_loadedPanels.TryGetValue(id, out var loadedPanel))
            {
                return loadedPanel;
            }

            var assetHandler = await M.AssetManager.LoadAssetAsync<GameObject>(address);
            _assetHandlers.Add(assetHandler);
            var panelObj = await assetHandler.InstantiateAsync();
            if (!panelObj.TryGetComponent<UIPanelBase>(out var panel))
            {
                Log.Error($"[UIManager] UIPanelBase component not found in panel object for '{id}' ({address}).");
                Object.Destroy(panelObj);
                return null;
            }
            panel.Init(id, parentLayer);
            panel.transform.SetParent(_closedPanelRoot, false);
            _loadedPanels[id] = panel;
            return panel;
        }

        public UIPanelBase OpenPanel(int id)
        {
            if (_openedPanels.TryGetValue(id, out var openedPanel))        // 已经打开
            {
                return openedPanel;
            }
            else if (_loadedPanels.TryGetValue(id, out var loadedPanel))   // 已经加载但未打开
            {
                var layer = GetUILayer(loadedPanel.ParentLayer);
                if (layer == null)
                {
                    Log.Error($"[UIManager] UILayer({loadedPanel.ParentLayer}) for panel({id}) not found.");
                    return null;
                }
                layer.OpenPanel(loadedPanel);
                _openedPanels[id] = loadedPanel;
                return loadedPanel;
            }
            else
            {
                Log.Error($"[UIManager] Panel({id}) is unloaded.");
                return null;
            }
        }

        public void ClosePanel(int id)
        {
            if (_openedPanels.TryGetValue(id, out var openedPanel))
            {
                var layer = GetUILayer(openedPanel.ParentLayer);
                layer?.ClosePanel(openedPanel);
                _openedPanels.Remove(id);
                openedPanel.transform.SetParent(_closedPanelRoot, false);
            }
        }

        public void ClosePanel(UIPanelBase panel)
        {
            ClosePanel(panel.ID);
        }

        public void UnloadPanel(int id)
        {
            if (_loadedPanels.TryGetValue(id, out var loadedPanel))
            {
                ClosePanel(id);
                _loadedPanels.Remove(id);
                loadedPanel.Clear();
                Object.Destroy(loadedPanel.gameObject);
            }
        }

        public void UnloadPanel(UIPanelBase panel)
        {
            UnloadPanel(panel.ID);
        }
    }
}