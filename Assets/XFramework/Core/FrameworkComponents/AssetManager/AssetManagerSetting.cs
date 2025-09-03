using UnityEngine;

namespace XGame.Core
{
    [CreateAssetMenu(fileName = "AssetManagerSetting", menuName = "XFramework/AssetManagerSetting")]
    public class AssetManagerSetting : ScriptableObject
    {
        public AssetManager.BuildMode BuildMode = AssetManager.BuildMode.Editor;
        public string MainPackageName = "DefaultPackage";
        public string DefaultHostServer = "http://<Server>/CDN/<Platform>/<Version>";
        public string FallbackHostServer = "http://<Server>/CDN/<Platform>/Fallback";
        public int MaxConcurrentDownloadCount = 10;
        public int FailedDownloadRetryCount = 3;
    }
}
