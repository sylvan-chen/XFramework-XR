using UnityEditor;
using XGame.Core;

namespace XGame.Editor
{
    [CustomEditor(typeof(EventManagerDebugger))]
    internal sealed class EventManagerInspector : InspectorBase
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Available in play mode only.", MessageType.Info);
                return;
            }

            var targetObject = target as EventManagerDebugger;
            var targetComponent = targetObject.Component as EventManager;
            EditorGUILayout.LabelField("Subscribed Event Count", targetComponent.SubscribedEventCount.ToString());
            EditorGUILayout.LabelField("Delayed Event Count", targetComponent.DelayedEventCount.ToString());

            Repaint();
        }
    }
}