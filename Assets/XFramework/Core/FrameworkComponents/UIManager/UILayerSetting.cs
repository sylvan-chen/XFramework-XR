using UnityEngine;
using XGame.Core;

[CreateAssetMenu(fileName = "UILayerSetting", menuName = "XFramework/UIManager/UILayerSetting")]
public class UILayerSetting : ScriptableObject
{
    public string Name;
    public int SortingOrder;
    public UILayer.StackSwitchType StackSwitchType;
}
