using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GB.Tools.Editor
{
    public static class SceneSwitchingMenu
    {
        [MenuItem("GB Tools/场景切换工具/快速切换/场景1", priority = 100)]
        private static void SwitchTo_0()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene("Assets/Scenes/test1.unity");
            }
        }

        [MenuItem("GB Tools/场景切换工具/快速切换/场景2", priority = 101)]
        private static void SwitchTo_1()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene("Assets/Scenes/test2.unity");
            }
        }

    }
}
