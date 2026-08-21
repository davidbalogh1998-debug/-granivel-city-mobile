using System.IO;
using UnityEditor;
using UnityEngine;

namespace GranivelCity.EditorTools
{
    public static class PCBuild
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("Granivel City/Build Windows x64")]
        public static void BuildWindows()
        {
            Directory.CreateDirectory("Builds/Windows");
            Configure();
            BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath }, locationPathName = "Builds/Windows/GranivelCityBudapestRP.exe",
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
            });
        }

        [MenuItem("Granivel City/Reset Save")]
        public static void ResetSave()
        {
            PlayerPrefs.DeleteAll(); PlayerPrefs.Save(); Debug.Log("Granivel City Budapest RP save reset.");
        }

        private static void Configure()
        {
            PlayerSettings.productName = "Granivel City — Budapest RP";
            PlayerSettings.companyName = "Granivel";
            PlayerSettings.bundleVersion = "0.2.0";
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.defaultScreenWidth = 1920; PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.runInBackground = true;
        }
    }
}
