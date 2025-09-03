using UnityEngine;

[CreateAssetMenu(fileName = "ProcedureManagerSetting", menuName = "XFramework/ProcedureManagerSetting")]
public class ProcedureManagerSetting : ScriptableObject
{
    public string StartupProcedureTypeName;
    public string[] AvailableProcedureTypeNames;
}
