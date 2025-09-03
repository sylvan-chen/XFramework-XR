using UnityEngine;

namespace XGame.Core
{
    [CreateAssetMenu(fileName = "UIManagerSetting", menuName = "XFramework/UIManager/UIManagerSetting")]
    public class UIManagerSetting : ScriptableObject
    {
        public string UIRootName = "[UIRoot]";
        public bool UseSeparateUICamera = false;
        public Vector2 ReferenceResolution = new(1920, 1080);
        public UILayerSetting[] LayerSettings;
    }
}