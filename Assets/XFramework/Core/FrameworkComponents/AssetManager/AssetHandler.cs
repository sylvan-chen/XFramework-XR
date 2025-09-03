using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;

namespace XGame.Core
{
    /// <summary>
    /// 资源句柄
    /// </summary>
    /// <remarks>
    /// NOTE：不需要之后一定要调用 Release() 方法释放资源，否则会造成内存泄露！
    /// </remarks>
    public class AssetHandler
    {
        private AssetHandle _handle;
        private readonly string _address;

        internal int RefCount { get; set; }

        /// <summary>
        /// 实际资源对象
        /// </summary>
        public UnityEngine.Object AssetObject => _handle?.AssetObject;

        /// <summary>
        /// 资源地址
        /// </summary>
        public string Address => _address;

        internal AssetHandler(AssetHandle handle, string address)
        {
            if (handle == null)
            {
                throw new ArgumentNullException(nameof(handle), "AssetHandle cannot be null.");
            }
            if (string.IsNullOrEmpty(address))
            {
                throw new ArgumentException("Asset address cannot be null or empty.", nameof(address));
            }

            _handle = handle;
            _address = address;
            RefCount = 1; // 初始化引用计数为 1
        }

        /// <summary>
        /// 异步实例化资源对象
        /// </summary>
        /// <returns>实例化得到的GameObject</returns>
        public async UniTask<GameObject> InstantiateAsync()
        {
            var option = _handle.InstantiateAsync();
            await option.ToUniTask();
            if (option.Status == EOperationStatus.Succeed)
            {
                Log.Debug($"[XFramework] Successfully instantiated asset '{_address}'.");
                return option.Result;
            }
            else
            {
                Log.Error($"[XFramework] Failed to instantiate asset '{_address}': {option.Error}");
                return null;
            }
        }

        /// <summary>
        /// 释放资源引用
        /// </summary>
        public void Release()
        {
            if (RefCount <= 0)
            {
                Log.Warning($"[XFramework] [AssetHandler] Attempting to release asset '{_address}' with non-positive ref count: {RefCount}");
            }
            else if (RefCount == 1)
            {
                Log.Debug($"[XFramework] [AssetHandler] Releasing asset '{_address}'");
                _handle?.Release();
                _handle = null;
                RefCount = 0;
            }
            else
            {
                Log.Debug($"[XFramework] [AssetHandler] Decreasing ref count for asset '{_address}', current count: {RefCount}");
                RefCount--; // 仅减少引用计数
            }
        }

        /// <summary>
        /// 强制清空引用计数，释放句柄（谨慎使用，可能会造成资源缺失！）
        /// </summary>
        public void ForceRelease()
        {
            _handle?.Release();
            _handle = null;
            RefCount = 0;
        }
    }
}