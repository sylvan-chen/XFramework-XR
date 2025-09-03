using UnityEditor;
using XGame.Core;

namespace XGame.Editor
{
    [CustomEditor(typeof(ProcedureManagerDebugger))]
    internal sealed class ProcedureManagerInspector : InspectorBase
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            var targetObject = target as ProcedureManagerDebugger;
            var targetComponent = targetObject.Component as ProcedureManager;

            // 游戏运行时，显示当前 Procedure
            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.LabelField("Current Procedure", targetComponent.CurrentProcedure == null ? "None" : targetComponent.CurrentProcedure.GetType().Name);
                EditorGUILayout.LabelField("Current Procedure Time", targetComponent.CurrentProcedureTime.ToString("N2"));
                EditorGUILayout.Separator();
            }

            Repaint();
        }
    }
}