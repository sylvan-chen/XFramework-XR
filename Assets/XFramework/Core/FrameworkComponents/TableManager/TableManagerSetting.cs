using UnityEngine;

namespace XGame.Core
{
    /// <summary>
    /// 配置表管理器组件数据
    /// </summary>
    [CreateAssetMenu(fileName = "TableManagerSetting", menuName = "XFramework/TableManagerSetting")]
    public class TableManagerSetting : ScriptableObject
    {
        public bool PreloadOnInit = true;
        public string[] PreloadTablePaths = new[] { "Configs/Schemes" };
    }
}
