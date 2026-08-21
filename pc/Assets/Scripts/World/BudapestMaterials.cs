using System.Collections.Generic;
using UnityEngine;

namespace GranivelCity
{
    public static class BudapestMaterials
    {
        private static readonly Dictionary<string, Material> Cache = new();

        public static Material Asphalt => GetNoise("Budapest_Asphalt", new Color(0.075f,0.08f,0.085f), 0.20f, 0.78f, 0.05f);
        public static Material Sidewalk => GetNoise("Budapest_Sidewalk", new Color(0.39f,0.38f,0.36f), 0.14f, 0.68f, 0.06f);
        public static Material Water => Get("Budapest_Danube", new Color(0.08f,0.20f,0.25f), 0.05f, 0.40f);
        public static Material Grass => GetNoise("Budapest_Grass", new Color(0.18f,0.27f,0.14f), 0.18f, 0.85f, 0.08f);
        public static Material Roof => GetNoise("Budapest_Roof", new Color(0.19f,0.16f,0.14f), 0.18f, 0.72f, 0.08f);
        public static Material Glass => Get("Budapest_Glass", new Color(0.08f,0.12f,0.15f), 0.55f, 0.12f);

        public static Material Building(string key)
        {
            int hash = Mathf.Abs(key.GetHashCode());
            Color[] palette =
            {
                new(0.55f,0.50f,0.43f), new(0.67f,0.62f,0.53f), new(0.48f,0.46f,0.43f),
                new(0.72f,0.68f,0.59f), new(0.56f,0.53f,0.49f), new(0.42f,0.43f,0.44f)
            };
            return GetFacade("Facade_" + (hash % 12), palette[hash % palette.Length], hash);
        }

        private static Material Get(string name, Color color, float metallic, float smoothness)
        {
            if (Cache.TryGetValue(name, out var existing)) return existing;
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
            var mat = new Material(shader) { name = name, color = color };
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
            Cache[name] = mat;
            return mat;
        }

        private static Material GetNoise(string name, Color baseColor, float variation, float roughness, float metallic)
        {
            if (Cache.TryGetValue(name, out var existing)) return existing;
            var mat = Get(name, Color.white, metallic, 1f - roughness);
            var tex = new Texture2D(256, 256, TextureFormat.RGBA32, true) { name = name + "_Albedo", wrapMode = TextureWrapMode.Repeat };
            var pixels = new Color[256 * 256];
            int seed = name.GetHashCode();
            Random.State old = Random.state; Random.InitState(seed);
            float ox = Random.Range(0f, 100f), oy = Random.Range(0f, 100f); Random.state = old;
            for (int y = 0; y < 256; y++)
            for (int x = 0; x < 256; x++)
            {
                float n = Mathf.PerlinNoise(ox + x * 0.055f, oy + y * 0.055f);
                float v = (n - 0.5f) * variation;
                pixels[y * 256 + x] = new Color(Mathf.Clamp01(baseColor.r + v), Mathf.Clamp01(baseColor.g + v), Mathf.Clamp01(baseColor.b + v), 1f);
            }
            tex.SetPixels(pixels); tex.Apply(true, false); mat.mainTexture = tex; mat.mainTextureScale = new Vector2(5f, 5f);
            return mat;
        }

        private static Material GetFacade(string name, Color wall, int seed)
        {
            if (Cache.TryGetValue(name, out var existing)) return existing;
            var mat = Get(name, Color.white, 0.02f, 0.28f);
            const int s = 256;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, true) { name = name + "_Facade", wrapMode = TextureWrapMode.Repeat };
            var pixels = new Color[s * s];
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                int cellX = x % 64, cellY = y % 64;
                bool window = cellX > 12 && cellX < 51 && cellY > 15 && cellY < 49;
                float grime = Mathf.PerlinNoise((x + seed % 31) * 0.035f, (y + seed % 17) * 0.035f) * 0.06f;
                Color c = window ? new Color(0.07f + grime, 0.10f + grime, 0.12f + grime) : wall * (0.92f + grime);
                if (cellY < 3 || cellX < 2) c *= 0.78f;
                pixels[y * s + x] = c;
            }
            tex.SetPixels(pixels); tex.Apply(true, false); mat.mainTexture = tex; mat.mainTextureScale = new Vector2(1f, 1f);
            return mat;
        }
    }
}
