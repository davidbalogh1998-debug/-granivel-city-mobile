using System.Collections.Generic;
using UnityEngine;

namespace GranivelCity
{
    public static class RuntimeMaterials
    {
        private static readonly Dictionary<string, Material> Cache = new();

        public static Material Get(string key, Color color, float metallic = 0f, float smoothness = 0.25f)
        {
            if (Cache.TryGetValue(key, out var existing) && existing != null) return existing;
            var shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var mat = new Material(shader) { color = color };
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
            Cache[key] = mat;
            return mat;
        }
    }
}
