using UnityEditor;
using UnityEditor.SceneManagement;

namespace XGame.Editor
{
    /// <summary>
    /// 在菜单栏快速打开场景的编辑器工具
    /// </summary>
    public static class OpenSceneQuickly
    {
        [MenuItem("Scenes/Launcher", priority = 1)]
        public static void OpenLauncher()
        {
            OpenScene("Assets/Res/Scenes/Launcher.unity");
        }

        [MenuItem("Scenes/Game01")]
        public static void OpenGame01()
        {
            OpenScene("Assets/Res/Scenes/Game01.unity");
        }

        private static void OpenScene(string scenePath)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(scenePath);
            }
        }
    }
}