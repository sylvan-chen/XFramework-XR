using UnityEngine;
using UnityEditor;
using Cysharp.Threading.Tasks;

namespace XGame.Modules.SimpleDressup.Editor
{
    [CustomEditor(typeof(SimpleDressupController))]
    public class SimpleDressupControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var controller = (SimpleDressupController)target;

            // 在 Inspector 中显示合并后的材质
            if (Application.isPlaying)
            {
                EditorGUILayout.Space();

                if (GUILayout.Button("Apply Outlook"))
                {
                    controller.ApplyOutlookAsync().Forget();
                }

                if (controller.CombinedMaterial != null)
                {

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Combined Material Preview", EditorStyles.boldLabel);

                    using (new EditorGUI.DisabledGroupScope(true))
                    {
                        EditorGUILayout.ObjectField("Combined Material", controller.CombinedMaterial, typeof(Material), false);
                    }

                    DrawMaterialPreview(controller.CombinedMaterial);
                }
            }
        }

        private void DrawMaterialPreview(Material material)
        {
            if (material == null) return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Shader", material.shader.name);

                var mainTexture = material.mainTexture;
                if (mainTexture != null)
                {
                    EditorGUILayout.ObjectField("Main Texture", mainTexture, typeof(Texture), false);
                }
                else
                {
                    EditorGUILayout.LabelField("Main Texture", "None");
                }
            }
        }
    }
}
