using YooAsset;
using UnityEngine;
using System;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

namespace XGame.Core
{
    /// <summary>
    /// 资源加载管理器，依赖于 YooAsset
    /// </summary>
    /// <remarks>目前只支持单个默认资源包，后续可以扩展支持多个资源包</remarks>
    [DisallowMultipleComponent]
    [AddComponentMenu("XFramework/Asset Manager")]
    public sealed class AssetManager : FrameworkComponent
    {
        public enum BuildMode
        {
            Editor,  // 编辑器模式，编辑器下模拟运行游戏，只在编辑器下有效
            Offline, // 单机运行模式，不需要热更新资源的游戏
            Online,  // 联机模式，需要热更新资源的游戏
            WebGL,   // 针对 WebGL 的特殊模式
        }

        private readonly AssetManagerSetting _setting;

        private ResourcePackage _package;
        private InitResult _initResult;

        public delegate void ProgressCallBack(float progress);

        // 资源下载回调
        public Action<DownloaderFinishData> OnDownloadFinishedEvent;
        public Action<DownloadErrorData> OnDownloadErrorEvent;
        public Action<DownloadUpdateData> OnDownloadUpdateEvent;
        public Action<DownloadFileData> OnDownloadFileBeginEvent;

        public AssetManager(AssetManagerSetting setting)
        {
            _setting = setting;
        }

        internal override void Init()
        {
            base.Init();

            YooAssets.Initialize();
            // 创建资源包实例（与Collector中创建的对应）
            _package = YooAssets.TryGetPackage(_setting.MainPackageName);
            _package ??= YooAssets.CreatePackage(_setting.MainPackageName);
            // 设置默认资源包，之后可以直接使用 YooAssets.XXX 接口来加载该资源包内容
            YooAssets.SetDefaultPackage(_package);
        }

        internal override void Shutdown()
        {
            base.Shutdown();

            ClearAssetHandlerCache();
            ClearSceneHandleCache();

            _package = null;
            _initResult = null;

            OnDownloadFinishedEvent = null;
            OnDownloadErrorEvent = null;
            OnDownloadUpdateEvent = null;
            OnDownloadFileBeginEvent = null;
        }

        internal override void Update(float deltaTime, float unscaledDeltaTime)
        {
            base.Update(deltaTime, unscaledDeltaTime);
        }

        #region 资源包初始化

        async public UniTask<InitResult> InitPackageAsync()
        {
            _initResult = new();

            DateTime startTime = DateTime.Now;

            await InitPackageInternal();

            TimeSpan duration = DateTime.Now - startTime;
            _initResult.InitDuration = duration;

            return _initResult;
        }

        /// <summary>
        /// 销毁资源包
        /// </summary>
        async public UniTask DestroyPackageAsync()
        {
            if (_package == null)
            {
                Log.Warning("[AssetManager] DestroyPackageAsync: Package is null, nothing to destroy.");
                return;
            }

            string packageName = _package.PackageName;
            DestroyOperation operation = _package.DestroyAsync();
            await operation.ToUniTask();

            if (operation.Status == EOperationStatus.Succeed)
            {
                Log.Debug($"[AssetManager] Destroy package ({packageName}) succeed.");
            }
            else
            {
                Log.Error($"[AssetManager] Destroy package ({packageName}) failed. {operation.Error}");
            }

            if (YooAssets.RemovePackage(_package))
            {
                Log.Debug($"[AssetManager] Remove package ({packageName}) from YooAssets succeed.");
            }
            else
            {
                Log.Warning($"[AssetManager] Remove package ({packageName}) from YooAssets failed, it may not exist.");
            }
        }

        /// <summary>
        /// 初始化资源包
        /// </summary>
        async private UniTask InitPackageInternal()
        {
            InitializationOperation operation = null;
            switch (_setting.BuildMode)
            {
                case BuildMode.Editor:
                    var simulateBuildResult = EditorSimulateModeHelper.SimulateBuild(_setting.MainPackageName);
                    var initParametersEditor = new EditorSimulateModeParameters()
                    {
                        EditorFileSystemParameters = FileSystemParameters.CreateDefaultEditorFileSystemParameters(simulateBuildResult.PackageRootDirectory)
                    };
                    operation = _package.InitializeAsync(initParametersEditor);
                    break;
                case BuildMode.Offline:
                    var initParametersOffline = new OfflinePlayModeParameters
                    {
                        BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters()
                    };
                    operation = _package.InitializeAsync(initParametersOffline);
                    break;
                case BuildMode.Online:
                    IRemoteServices remoteServicesOnline = new RemoteServices(_setting.DefaultHostServer, _setting.FallbackHostServer);
                    var initParametersOnline = new HostPlayModeParameters
                    {
                        BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters(),
                        CacheFileSystemParameters = FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServicesOnline)
                    };
                    operation = _package.InitializeAsync(initParametersOnline);
                    break;
                case BuildMode.WebGL:
                    IRemoteServices remoteServicesWebGL = new RemoteServices(_setting.DefaultHostServer, _setting.FallbackHostServer);
                    var initParametersWebGL = new WebPlayModeParameters
                    {
                        WebServerFileSystemParameters = FileSystemParameters.CreateDefaultWebServerFileSystemParameters(),
                        WebRemoteFileSystemParameters = FileSystemParameters.CreateDefaultWebRemoteFileSystemParameters(remoteServicesWebGL)
                    };
                    operation = _package.InitializeAsync(initParametersWebGL);
                    break;
                default:
                    Log.Error($"[AssetManager] Invalid package mode: {_setting.BuildMode}");
                    break;
            }
            await operation.ToUniTask();

            if (operation.Status == EOperationStatus.Succeed)
            {
                Log.Debug($"[AssetManager] Initialize package succeed. ({_setting.BuildMode})");
                await RequestPackageVersion();
            }
            else
            {
                Log.Error($"[AssetManager] Initialize package failed. ({_setting.BuildMode}) {operation.Error}");
                _initResult.Succeed = false;
                _initResult.ErrorMessage = operation.Error;
            }
        }

        /// <summary>
        /// 获取资源版本
        /// </summary>
        async private UniTask RequestPackageVersion()
        {
            var operation = _package.RequestPackageVersionAsync();
            await operation.ToUniTask();

            if (operation.Status == EOperationStatus.Succeed)
            {
                _initResult.PackageVersion = operation.PackageVersion;
                Log.Debug($"[AssetManager] Request package version succeed. {_initResult.PackageVersion}");
                await UpdatePackageManifest();
            }
            else
            {
                Log.Error($"[AssetManager] Request package version failed. {operation.Error}");
                _initResult.Succeed = false;
                _initResult.ErrorMessage = operation.Error;
            }
        }

        /// <summary>
        /// 根据版本号更新资源清单
        /// </summary>
        async private UniTask UpdatePackageManifest()
        {
            var operation = _package.UpdatePackageManifestAsync(_initResult.PackageVersion);
            await operation.ToUniTask();

            if (operation.Status == EOperationStatus.Succeed)
            {
                Log.Debug($"[AssetManager] Update package manifest succeed. Latest version: {_initResult.PackageVersion}");
                await UpdatePackageFiles();
            }
            else
            {
                Log.Error($"[AssetManager] Update package manifest failed. (Latest version: {_initResult.PackageVersion}) {operation.Error}");
                _initResult.Succeed = false;
                _initResult.ErrorMessage = operation.Error;
            }
        }

        /// <summary>
        /// 根据资源清单更新资源文件（下载到缓存资源）
        /// </summary>
        async private UniTask UpdatePackageFiles()
        {
            var downloader = _package.CreateResourceDownloader(_setting.MaxConcurrentDownloadCount, _setting.FailedDownloadRetryCount);

            if (downloader.TotalDownloadCount == 0)
            {
                Log.Debug("[AssetManager] No package files need to update.");
                _initResult.Succeed = true;
                _initResult.ErrorMessage = string.Empty;
                _initResult.DownloadCount = 0;
                _initResult.DownloadBytes = 0;
                _initResult.DownloadDuration = TimeSpan.Zero;
                return;
            }

            int totalDownloadCount = downloader.TotalDownloadCount;
            long totalDownloadBytes = downloader.TotalDownloadBytes;

            downloader.DownloadFinishCallback = (finishData) =>
            {
                OnDownloadFinishedEvent?.Invoke(finishData);
            };
            downloader.DownloadErrorCallback = (errorData) =>
            {
                OnDownloadErrorEvent?.Invoke(errorData);
            };
            downloader.DownloadUpdateCallback = (updateData) =>
            {
                OnDownloadUpdateEvent?.Invoke(updateData);
            };
            downloader.DownloadFileBeginCallback = (fileData) =>
            {
                OnDownloadFileBeginEvent?.Invoke(fileData);
            };

            DateTime startTime = DateTime.Now;

            downloader.BeginDownload();
            await downloader.ToUniTask();

            TimeSpan duration = DateTime.Now - startTime;

            if (downloader.Status == EOperationStatus.Succeed)
            {
                Log.Debug($"[AssetManager] Update package files succeed. Total download count: {totalDownloadCount}, Total download bytes: {totalDownloadBytes}");
                _initResult.Succeed = true;
                _initResult.ErrorMessage = string.Empty;
                _initResult.DownloadCount = totalDownloadCount;
                _initResult.DownloadBytes = totalDownloadBytes;
                _initResult.DownloadDuration = duration;
            }
            else
            {
                Log.Error($"[AssetManager] Update package files failed. {downloader.Error}");
                _initResult.Succeed = false;
                _initResult.ErrorMessage = downloader.Error;
            }
        }

        #endregion

        #region 资源缓存清理

        /// <summary>
        /// 清理所有缓存资源文件
        /// </summary>
        async public UniTask ClearAllCacheBundleFiles()
        {
            var operation = _package.ClearCacheFilesAsync(EFileClearMode.ClearAllBundleFiles);
            await operation.ToUniTask();

            if (operation.Status == EOperationStatus.Succeed)
            {
                Log.Debug($"[AssetManager] Clear all cache bundle files succeed.");
            }
            else
            {
                Log.Error($"[AssetManager] Clear all cache bundle files failed. {operation.Error}");
            }
        }

        /// <summary>
        /// 清理未使用的缓存资源文件
        /// </summary>
        async public UniTask ClearUnusedCacheBundleFiles()
        {
            var operation = _package.ClearCacheFilesAsync(EFileClearMode.ClearUnusedBundleFiles);
            await operation.ToUniTask();

            if (operation.Status == EOperationStatus.Succeed)
            {
                Log.Debug($"[AssetManager] Clear unused cache bundle files succeed.");
            }
            else
            {
                Log.Error($"[AssetManager] Clear unused cache bundle files failed. {operation.Error}");
            }
        }

        /// <summary>
        /// 清理所有缓存清单文件
        /// </summary>
        async public UniTask ClearAllCacheManifestFiles()
        {
            var operation = _package.ClearCacheFilesAsync(EFileClearMode.ClearAllManifestFiles);
            await operation.ToUniTask();

            if (operation.Status == EOperationStatus.Succeed)
            {
                Log.Debug($"[AssetManager] Clear all cache manifest files succeed.");
            }
            else
            {
                Log.Error($"[AssetManager] Clear all cache manifest files failed. {operation.Error}");
            }
        }

        /// <summary>
        /// 清理未使用的缓存清单文件
        /// </summary>
        async public UniTask ClearUnusedCacheManifestFiles()
        {
            var operation = _package.ClearCacheFilesAsync(EFileClearMode.ClearUnusedManifestFiles);
            await operation.ToUniTask();

            if (operation.Status == EOperationStatus.Succeed)
            {
                Log.Debug($"[AssetManager] Clear unused cache manifest files succeed.");
            }
            else
            {
                Log.Error($"[AssetManager] Clear unused cache manifest files failed. {operation.Error}");
            }
        }

        #endregion


        #region 统计信息

        /// <summary>
        /// 获取当前资源使用统计信息
        /// </summary>
        /// <returns>资源使用统计</returns>
        public ResourceUsageStats GetResourceUsageStats()
        {
            return new ResourceUsageStats(
                _assetHandlerCache.Count,
                _sceneInfoCache.Count,
                _assetHandlerCache.Values.Sum(handler => handler.RefCount),
                _assetHandlerCache.Where(kvp => kvp.Value.RefCount <= 0).Count()
            );
        }

        #endregion

        #region 资源加载

        private readonly Dictionary<string, AssetHandler> _assetHandlerCache = new();

        /// <summary>
        /// 获取资源句柄
        /// </summary>
        /// <remarks>
        /// AssetManager 负责引用计数管理，调用者使用完毕后调用 ReleaseAsset
        /// </remarks>
        /// <returns>资源句柄</returns>
        public async UniTask<AssetHandler> LoadAssetAsync<T>(string address) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(address))
            {
                throw new ArgumentException("Asset address cannot be null or empty.", nameof(address));
            }

            if (_package == null)
            {
                Log.Error($"[AssetManager] Cannot load asset '{address}'. Package is not initialized.");
                return null;
            }

            // 检查是否已经加载
            if (_assetHandlerCache.TryGetValue(address, out AssetHandler cachedHandler))
            {
                cachedHandler.RefCount++;
                Log.Debug($"[AssetManager] Reuse cached asset ({address}), ref count: {cachedHandler.RefCount}");
                return cachedHandler;
            }

            // 首次加载
            AssetHandle yooHandle = _package.LoadAssetAsync<T>(address);
            await yooHandle.ToUniTask();

            if (yooHandle.Status == EOperationStatus.Succeed)
            {
                var handler = new AssetHandler(yooHandle, address);
                _assetHandlerCache[address] = handler;
                Log.Debug($"[AssetManager] Load asset ({address}) succeed, ref count: {handler.RefCount}");
                return handler;
            }
            else
            {
                Log.Error($"[AssetManager] Failed to load asset: {address}. Error: {yooHandle.LastError}");
                yooHandle.Release();
                return null;
            }
        }

        private void ClearAssetHandlerCache()
        {
            foreach (var handler in _assetHandlerCache.Values)
            {
                if (handler.RefCount >= 1)
                {
                    handler.ForceRelease();
                }
            }
            _assetHandlerCache.Clear();
        }

        /// <summary>
        /// 批量加载资源
        /// </summary>
        /// <param name="addresses">资源地址列表</param>
        /// <param name="progressCallback">进度回调</param>
        /// <returns>批量加载结果</returns>
        public async UniTask<BatchLoadResult> LoadAssetsAsync<T>(IEnumerable<string> addresses, ProgressCallBack progressCallback = null) where T : UnityEngine.Object
        {
            var addressList = addresses.ToList();
            var successfulAssetHandlers = new Dictionary<string, AssetHandler>();
            var failedAddresses = new List<string>();
            var loadedCount = 0;
            var totalCount = addressList.Count;

            foreach (var address in addressList)
            {
                try
                {
                    var asset = await LoadAssetAsync<T>(address);
                    if (asset != null)
                    {
                        successfulAssetHandlers[address] = asset;
                    }
                    else
                    {
                        failedAddresses.Add(address);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"[AssetManager] Failed to load asset in batch: {address}. Error: {ex.Message}");
                    failedAddresses.Add(address);
                }

                loadedCount++;
                progressCallback?.Invoke((float)loadedCount / totalCount);
            }

            return new BatchLoadResult(successfulAssetHandlers, failedAddresses);
        }

        #endregion


        #region 资源卸载

        /// <summary>
        /// 尝试卸载指定资源
        /// </summary>
        /// <param name="address">资源地址</param>
        /// <remarks>
        /// 如果该资源还在被使用（存在句柄引用），该方法会无效
        /// </remarks>
        /// <returns>是否成功卸载</returns>
        public bool TryUnloadUnusedAsset(string address)
        {
            if (string.IsNullOrEmpty(address))
            {
                Log.Warning("[AssetManager] TryUnloadUnusedAsset: address is null or empty.");
                return false;
            }

            // 如果在缓存中且引用计数为0，则从缓存中移除
            if (_assetHandlerCache.TryGetValue(address, out AssetHandler handler) && handler.RefCount <= 0)
            {
                _assetHandlerCache.Remove(address);
                _package.TryUnloadUnusedAsset(address);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 卸载所有引用句柄为零的资源
        /// </summary>
        /// <remarks>
        /// 可以在切换场景之后调用资源释放方法或者写定时器间隔时间去释放
        /// </remarks>
        /// <returns>卸载操作的结果信息</returns>
        public async UniTask<UnloadResult> UnloadUnusedAssetsAsync()
        {
            Log.Debug($"[AssetManager] Starting to unload unused assets...");

            var startTime = DateTime.Now;

            var operation = _package.UnloadUnusedAssetsAsync();
            await operation.ToUniTask();

            TimeSpan duration = DateTime.Now - startTime;
            bool succeed = operation.Status == EOperationStatus.Succeed;
            string errorMessage = string.Empty;

            if (succeed)
            {
                Log.Debug($"[AssetManager] Unload unused assets completed successfully. " +
                         $"Duration: {duration.TotalMilliseconds:F2}ms");
            }
            else
            {
                Log.Error($"[AssetManager] Unload unused assets failed: {operation.Error}");
                errorMessage = operation.Error;
            }

            var result = new UnloadResult(succeed, errorMessage, duration);
            return result;
        }

        /// <summary>
        /// 强制卸载所有资源
        /// </summary>
        /// <remarks>
        /// Package 在销毁的时候也会自动调用该方法
        /// </remarks>
        /// <returns>卸载操作的结果信息</returns>
        public async UniTask<UnloadResult> ForceUnloadAllAssetsAsync()
        {
            Log.Debug($"[AssetManager] Starting to force unload all assets...");

            var startTime = DateTime.Now;

            // 清理所有缓存
            ClearAssetHandlerCache();
            ClearSceneHandleCache();

            var operation = _package.UnloadAllAssetsAsync();
            await operation.ToUniTask();

            TimeSpan duration = DateTime.Now - startTime;
            bool succeed = operation.Status == EOperationStatus.Succeed;
            string errorMessage = string.Empty;

            if (succeed)
            {
                Log.Debug($"[AssetManager] Force unload all assets completed successfully. " +
                         $"Duration: {duration.TotalMilliseconds:F2}ms");
            }
            else
            {
                Log.Error($"[AssetManager] Force unload all assets failed: {operation.Error}");
                errorMessage = operation.Error;
            }

            var result = new UnloadResult(succeed, errorMessage, duration);
            return result;
        }

        #endregion


        #region 场景资源管理

        private readonly Dictionary<string, SceneInfo> _sceneInfoCache = new();

        /// <summary>
        /// 加载场景
        /// </summary>
        /// <param name="address">场景地址</param>
        /// <param name="sceneMode">场景加载模式</param>
        /// <param name="physicsMode">物理模式</param>
        /// <param name="progressCallback">进度回调</param>
        /// <param name="suspendLoad">是否暂停加载（不激活）</param>
        /// <param name="priority">加载优先级</param>
        /// <returns>加载是否成功</returns>
        public async UniTask<bool> LoadSceneAsync(string address, LoadSceneMode sceneMode = LoadSceneMode.Single,
            LocalPhysicsMode physicsMode = LocalPhysicsMode.None, ProgressCallBack progressCallback = null,
            bool suspendLoad = false, uint priority = 0)
        {
            // 检查当前状态
            var currentState = GetSceneState(address);
            if (currentState == SceneState.Loading)
            {
                Log.Warning($"[AssetManager] Scene ({address}) is already loading.");
                return false;
            }
            else if (currentState == SceneState.LoadedActive || currentState == SceneState.LoadedInactive)
            {
                Log.Warning($"[AssetManager] Scene ({address}) is already loaded.");
                return true;
            }

            SceneHandle handle;
            SceneInfo sceneInfo;

            // 开始加载
            handle = _package.LoadSceneAsync(address, sceneMode, physicsMode, suspendLoad, priority);
            sceneInfo = new SceneInfo(handle, SceneState.Loading, sceneMode);
            _sceneInfoCache[address] = sceneInfo;

            // 监听进度
            if (progressCallback != null)
            {
                while (!handle.IsDone)
                {
                    progressCallback?.Invoke(handle.Progress);
                    await UniTask.Delay(16); // 约 60 FPS 的更新频率
                }
                progressCallback?.Invoke(1.0f);
            }

            await handle.ToUniTask();

            if (handle.Status == EOperationStatus.Succeed)
            {
                // 更新状态
                sceneInfo.State = suspendLoad ? SceneState.LoadedInactive : SceneState.LoadedActive;
                Log.Debug($"[AssetManager] Load scene ({handle.SceneName}) succeed. State: {sceneInfo.State}");
                return true;
            }
            else
            {
                Log.Error($"[AssetManager] Load scene ({address}) failed: {handle.LastError}");
                handle.Release();
                _sceneInfoCache.Remove(address);
                return false;
            }
        }

        /// <summary>
        /// 预加载场景（不激活）
        /// </summary>
        /// <param name="address">场景地址</param>
        /// <param name="physicsMode">物理模式</param>
        /// <param name="progressCallback">进度回调</param>
        /// <param name="priority">加载优先级</param>
        /// <returns>预加载是否成功</returns>
        public async UniTask<bool> PreloadSceneAsync(string address, LocalPhysicsMode physicsMode = LocalPhysicsMode.None,
            ProgressCallBack progressCallback = null, uint priority = 0)
        {
            return await LoadSceneAsync(address, LoadSceneMode.Additive, physicsMode, progressCallback, true, priority);
        }

        /// <summary>
        /// 激活预加载的场景
        /// </summary>
        /// <param name="address">场景地址</param>
        /// <returns>是否激活成功</returns>
        public bool ActivatePreloadedScene(string address)
        {
            if (_sceneInfoCache.TryGetValue(address, out SceneInfo sceneInfo))
            {
                if (sceneInfo.State != SceneState.LoadedInactive)
                {
                    Log.Warning($"[AssetManager] Scene ({address}) is not in preloaded state. Current state: {sceneInfo.State}");
                    return false;
                }

                sceneInfo.Handle.ActivateScene();
                sceneInfo.State = SceneState.LoadedActive;
                Log.Debug($"[AssetManager] Activate preloaded scene ({address}).");
                return true;
            }
            else
            {
                Log.Error($"[AssetManager] Scene ({address}) is not preloaded.");
                return false;
            }
        }

        /// <summary>
        /// 卸载场景
        /// </summary>
        /// <param name="address">场景地址</param>
        /// <returns>卸载是否成功</returns>
        public async UniTask<bool> UnloadSceneAsync(string address)
        {
            if (_sceneInfoCache.TryGetValue(address, out SceneInfo sceneInfo))
            {
                if (sceneInfo.State == SceneState.Unloading)
                {
                    Log.Warning($"[AssetManager] Scene ({address}) is already unloading.");
                    return false;
                }

                // 更新状态为卸载中
                sceneInfo.State = SceneState.Unloading;

                var unloadOperation = sceneInfo.Handle.UnloadAsync();
                await unloadOperation.ToUniTask();

                if (unloadOperation.Status == EOperationStatus.Succeed)
                {
                    _sceneInfoCache.Remove(address);
                    Log.Debug($"[AssetManager] Unload scene ({address}) succeed.");
                    return true;
                }
                else
                {
                    // 卸载失败，恢复状态
                    sceneInfo.State = SceneState.LoadedActive; // 或之前的状态
                    Log.Error($"[AssetManager] Unload scene ({address}) failed: {unloadOperation.Error}");
                    return false;
                }
            }
            else
            {
                Log.Warning($"[AssetManager] Try to unload scene ({address}) that is not loaded.");
                return false;
            }
        }

        /// <summary>
        /// 批量卸载场景，请注意至少保留一个场景
        /// </summary>
        /// <param name="addresses">要卸载的场景地址列表</param>
        /// <returns>卸载结果，包含成功和失败的场景列表</returns>
        public async UniTask<SceneUnloadResult> UnloadScenesAsync(IEnumerable<string> addresses)
        {
            var successList = new List<string>();
            var failedList = new List<string>();

            foreach (var address in addresses)
            {
                bool success = await UnloadSceneAsync(address);
                if (success)
                    successList.Add(address);
                else
                    failedList.Add(address);
            }

            return new SceneUnloadResult(successList, failedList);
        }

        /// <summary>
        /// 卸载所有场景（除了指定的保留场景）
        /// </summary>
        /// <param name="exceptAddresses">要保留的场景地址列表</param>
        /// <returns>卸载结果</returns>
        public async UniTask<SceneUnloadResult> UnloadAllScenesExceptAsync(params string[] exceptAddresses)
        {
            var toUnload = _sceneInfoCache.Keys
                .Where(address => !exceptAddresses.Contains(address))
                .ToList();

            return await UnloadScenesAsync(toUnload);
        }

        /// <summary>
        /// 获取场景诊断信息
        /// </summary>
        /// <returns>场景诊断信息</returns>
        public SceneDiagnosticInfo GetSceneDiagnosticInfo()
        {
            var scenesByState = _sceneInfoCache.Values
                .GroupBy(info => info.State)
                .ToDictionary(g => g.Key, g => g.Count());

            var scenesByMode = _sceneInfoCache.Values
                .GroupBy(info => info.LoadMode)
                .ToDictionary(g => g.Key, g => g.Count());

            return new SceneDiagnosticInfo(
                totalScenes: _sceneInfoCache.Count,
                scenesByState: scenesByState,
                scenesByMode: scenesByMode,
                oldestLoadTime: _sceneInfoCache.Values.Any() ? _sceneInfoCache.Values.Min(info => info.LoadTime) : DateTime.Now,
                newestLoadTime: _sceneInfoCache.Values.Any() ? _sceneInfoCache.Values.Max(info => info.LoadTime) : DateTime.Now
            );
        }

        private void ClearSceneHandleCache()
        {
            foreach (var sceneInfo in _sceneInfoCache.Values)
            {
                sceneInfo.Handle?.Release();
            }
            _sceneInfoCache.Clear();
        }

        #endregion


        #region 场景状态管理

        /// <summary>
        /// 场景状态枚举
        /// </summary>
        public enum SceneState
        {
            NotLoaded,      // 未加载
            Loading,        // 加载中
            LoadedInactive, // 已加载但未激活（预加载状态）
            LoadedActive,   // 已加载且激活
            Unloading       // 卸载中
        }

        /// <summary>
        /// 场景信息
        /// </summary>
        public class SceneInfo
        {
            public SceneHandle Handle { get; set; }
            public SceneState State { get; set; }
            public LoadSceneMode LoadMode { get; set; }
            public DateTime LoadTime { get; set; }

            public SceneInfo(SceneHandle handle, SceneState state, LoadSceneMode mode)
            {
                Handle = handle;
                State = state;
                LoadMode = mode;
                LoadTime = DateTime.Now;
            }
        }

        /// <summary>
        /// 获取场景状态
        /// </summary>
        /// <param name="address">场景地址</param>
        /// <returns>场景状态</returns>
        public SceneState GetSceneState(string address)
        {
            if (_sceneInfoCache.TryGetValue(address, out SceneInfo info))
            {
                return info.State;
            }
            return SceneState.NotLoaded;
        }

        /// <summary>
        /// 获取所有场景信息
        /// </summary>
        /// <returns>场景信息字典</returns>
        public Dictionary<string, SceneInfo> GetAllScenesInfo()
        {
            return new Dictionary<string, SceneInfo>(_sceneInfoCache);
        }

        /// <summary>
        /// 检查场景是否可以安全卸载
        /// </summary>
        /// <param name="address">场景地址</param>
        /// <returns>是否可以安全卸载</returns>
        public bool CanUnloadScene(string address)
        {
            if (!_sceneInfoCache.TryGetValue(address, out SceneInfo sceneInfo))
                return false;

            // 正在卸载中或未加载的场景不能再次卸载
            return sceneInfo.State != SceneState.Unloading && sceneInfo.State != SceneState.NotLoaded;
        }

        /// <summary>
        /// 获取活跃场景数量
        /// </summary>
        /// <returns>活跃场景数量</returns>
        public int GetActiveSceneCount()
        {
            return _sceneInfoCache.Values.Count(info => info.State == SceneState.LoadedActive);
        }

        /// <summary>
        /// 获取所有已加载场景的地址
        /// </summary>
        /// <returns>已加载场景地址列表</returns>
        public List<string> GetLoadedSceneAddresses()
        {
            return _sceneInfoCache
                .Where(kvp => kvp.Value.State == SceneState.LoadedActive || kvp.Value.State == SceneState.LoadedInactive)
                .Select(kvp => kvp.Key)
                .ToList();
        }

        #endregion


        #region 数据结构定义

        public class InitResult
        {
            /// <summary>
            /// 是否初始化成功
            /// </summary>
            public bool Succeed { get; internal set; }

            /// <summary>
            /// 错误消息（如果失败）
            /// </summary>
            public string ErrorMessage { get; internal set; }

            /// <summary>
            /// 包版本号
            /// </summary>
            public string PackageVersion { get; internal set; }

            /// <summary>
            /// 下载的资源数量
            /// </summary>
            public int DownloadCount { get; internal set; }

            /// <summary>
            /// 下载的总字节数
            /// </summary>
            public long DownloadBytes { get; internal set; }

            /// <summary>
            /// 初始化操作总耗时
            /// </summary>
            public TimeSpan InitDuration { get; internal set; }

            /// <summary>
            /// 下载操作总耗时
            /// </summary>
            public TimeSpan DownloadDuration { get; internal set; }

            public override string ToString()
            {
                return $"Success: {Succeed}, " +
                       (Succeed ? "" : $", Error: {ErrorMessage}") +
                       (DownloadCount > 0 ? $", Downloaded: {DownloadCount} files, {DownloadBytes / 1024.0:F2} KB" : "") +
                       (InitDuration != default ? $", Init Duration: {InitDuration.TotalMilliseconds:F2}ms" : "") +
                       (DownloadDuration != default ? $", Download Duration: {DownloadDuration.TotalMilliseconds:F2}ms" : "");
            }
        }

        /// <summary>
        /// 批量加载资源结果
        /// </summary>
        public readonly struct BatchLoadResult
        {
            /// <summary>
            /// 成功加载的资源句柄字典
            /// </summary>
            public readonly Dictionary<string, AssetHandler> SuccessfulAssetHandlers;

            /// <summary>
            /// 加载失败的资源地址列表
            /// </summary>
            public readonly List<string> FailedAddresses;

            /// <summary>
            /// 是否全部成功
            /// </summary>
            public readonly bool AllSuccessful => FailedAddresses.Count == 0;

            /// <summary>
            /// 成功数量
            /// </summary>
            public readonly int SuccessCount => SuccessfulAssetHandlers.Count;

            /// <summary>
            /// 失败数量
            /// </summary>
            public readonly int FailureCount => FailedAddresses.Count;

            /// <summary>
            /// 总数量
            /// </summary>
            public readonly int TotalCount => SuccessCount + FailureCount;

            public BatchLoadResult(Dictionary<string, AssetHandler> successfulAssetHandlers, List<string> failedAddresses)
            {
                SuccessfulAssetHandlers = successfulAssetHandlers ?? new Dictionary<string, AssetHandler>();
                FailedAddresses = failedAddresses ?? new List<string>();
            }

            public override readonly string ToString()
            {
                return $"Batch Load Result: {SuccessCount}/{TotalCount} successful, {FailureCount} failed";
            }
        }

        /// <summary>
        /// 资源卸载操作结果
        /// </summary>
        public readonly struct UnloadResult
        {
            /// <summary>
            /// 操作是否成功
            /// </summary>
            public readonly bool Succeed;

            /// <summary>
            /// 错误消息（如果失败）
            /// </summary>
            public readonly string ErrorMessage;

            /// <summary>
            /// 操作耗时
            /// </summary>
            public readonly TimeSpan Duration;

            public UnloadResult(bool succeed, string errorMessage, TimeSpan duration)
            {
                Succeed = succeed;
                ErrorMessage = errorMessage;
                Duration = duration;
            }

            public override readonly string ToString()
            {
                return $"Success: {Succeed}, Duration: {Duration.TotalMilliseconds:F2}ms, " +
                       (Succeed ? "" : $", Error: {ErrorMessage}");
            }
        }

        /// <summary>
        /// 资源使用统计信息
        /// </summary>
        public readonly struct ResourceUsageStats
        {
            /// <summary>
            /// 缓存的资源数量
            /// </summary>
            public readonly int CachedAssetsCount;

            /// <summary>
            /// 缓存的场景数量
            /// </summary>
            public readonly int CachedScenesCount;

            /// <summary>
            /// 总引用计数
            /// </summary>
            public readonly int TotalReferenceCount;

            /// <summary>
            /// 引用计数为0的资源数量
            /// </summary>
            public readonly int ZeroRefAssetsCount;

            public ResourceUsageStats(int cachedAssetsCount, int cachedScenesCount, int totalReferenceCount, int zeroRefAssetsCount)
            {
                CachedAssetsCount = cachedAssetsCount;
                CachedScenesCount = cachedScenesCount;
                TotalReferenceCount = totalReferenceCount;
                ZeroRefAssetsCount = zeroRefAssetsCount;
            }

            public override readonly string ToString()
            {
                return $"Cached Assets: {CachedAssetsCount}, Cached Scenes: {CachedScenesCount}, " +
                       $"Total Refs: {TotalReferenceCount}, Zero Refs: {ZeroRefAssetsCount}";
            }
        }

        /// <summary>
        /// 场景批量卸载结果
        /// </summary>
        public readonly struct SceneUnloadResult
        {
            /// <summary>
            /// 成功卸载的场景列表
            /// </summary>
            public readonly List<string> SuccessfulScenes;

            /// <summary>
            /// 卸载失败的场景列表
            /// </summary>
            public readonly List<string> FailedScenes;

            /// <summary>
            /// 是否全部成功
            /// </summary>
            public readonly bool AllSuccessful => FailedScenes.Count == 0;

            /// <summary>
            /// 成功数量
            /// </summary>
            public readonly int SuccessCount => SuccessfulScenes.Count;

            /// <summary>
            /// 失败数量
            /// </summary>
            public readonly int FailureCount => FailedScenes.Count;

            public SceneUnloadResult(List<string> successfulScenes, List<string> failedScenes)
            {
                SuccessfulScenes = successfulScenes ?? new List<string>();
                FailedScenes = failedScenes ?? new List<string>();
            }

            public override readonly string ToString()
            {
                return $"Scene Unload Result: {SuccessCount} successful, {FailureCount} failed";
            }
        }

        /// <summary>
        /// 场景诊断信息
        /// </summary>
        public readonly struct SceneDiagnosticInfo
        {
            /// <summary>
            /// 总场景数量
            /// </summary>
            public readonly int TotalScenes;

            /// <summary>
            /// 按状态分组的场景数量
            /// </summary>
            public readonly Dictionary<SceneState, int> ScenesByState;

            /// <summary>
            /// 按加载模式分组的场景数量
            /// </summary>
            public readonly Dictionary<LoadSceneMode, int> ScenesByMode;

            /// <summary>
            /// 最早加载时间
            /// </summary>
            public readonly DateTime OldestLoadTime;

            /// <summary>
            /// 最新加载时间
            /// </summary>
            public readonly DateTime NewestLoadTime;

            public SceneDiagnosticInfo(int totalScenes, Dictionary<SceneState, int> scenesByState,
                Dictionary<LoadSceneMode, int> scenesByMode, DateTime oldestLoadTime, DateTime newestLoadTime)
            {
                TotalScenes = totalScenes;
                ScenesByState = scenesByState ?? new Dictionary<SceneState, int>();
                ScenesByMode = scenesByMode ?? new Dictionary<LoadSceneMode, int>();
                OldestLoadTime = oldestLoadTime;
                NewestLoadTime = newestLoadTime;
            }

            public override readonly string ToString()
            {
                var stateInfo = string.Join(", ", ScenesByState.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
                var modeInfo = string.Join(", ", ScenesByMode.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
                return $"Total: {TotalScenes}, States: [{stateInfo}], Modes: [{modeInfo}]";
            }
        }

        public class RemoteServices : IRemoteServices
        {
            public RemoteServices(string defaultHostServer, string fallbackHostServer)
            {
                DefaultHostServer = defaultHostServer;
                FallbackHostServer = fallbackHostServer;
            }

            public string DefaultHostServer { get; private set; }
            public string FallbackHostServer { get; private set; }

            public string GetRemoteFallbackURL(string fileName)
            {
                return $"{FallbackHostServer}/{fileName}";
            }

            public string GetRemoteMainURL(string fileName)
            {
                return $"{DefaultHostServer}/{fileName}";
            }
        }
        #endregion
    }
}