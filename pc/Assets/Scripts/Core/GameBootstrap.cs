using UnityEngine;

namespace GranivelCity
{
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (Object.FindFirstObjectByType<GameRuntime>() != null) return;
            var root = new GameObject("GRANIVEL CITY RUNTIME");
            Object.DontDestroyOnLoad(root);
            root.AddComponent<GameRuntime>();
        }
    }
}
