using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace GranivelCity.EditorTools
{
    [InitializeOnLoad]
    public static class FirstRunSetup
    {
        static FirstRunSetup()
        {
            EditorApplication.delayCall += EnsureScene;
        }

        private static void EnsureScene()
        {
            const string scenePath = "Assets/Scenes/Main.unity";
            if (!Directory.Exists("Assets/Scenes")) Directory.CreateDirectory("Assets/Scenes");

            if (!File.Exists(scenePath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, scenePath);
                AssetDatabase.Refresh();
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };

            if (!EditorApplication.isPlayingOrWillChangePlaymode && SceneManager.GetActiveScene().path != scenePath)
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }
    }
}
