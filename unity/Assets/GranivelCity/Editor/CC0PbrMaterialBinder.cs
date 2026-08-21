#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GranivelCity.Editor
{
    public class CC0PbrMaterialBinder : AssetPostprocessor
    {
        const string Root = "Assets/ThirdParty/CC0/polyhaven";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root, StringComparison.OrdinalIgnoreCase)) return;
            string lower = assetPath.ToLowerInvariant();
            var importer = (TextureImporter)assetImporter;
            importer.maxTextureSize = 8192;
            if (lower.Contains("nor_gl") || lower.Contains("normal"))
                importer.textureType = TextureImporterType.NormalMap;
        }

        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (!imported.Any(p => p.StartsWith("Assets/GranivelCity/Generated/", StringComparison.OrdinalIgnoreCase))) return;
            ApplyAll();
        }

        [MenuItem("Granivel City/Apply CC0 PBR Materials")]
        public static void ApplyAll()
        {
            Apply("Asphalt", "asphalt_01");
            Apply("Concrete", "concrete_pavement");
            string[] walls = { "plastered_wall", "painted_plaster_wall", "brick_wall_08", "concrete_wall_003", "brick_wall_001", "grey_plaster", "mixed_brick_wall" };
            for (int i = 0; i < walls.Length; i++) Apply("Facade" + i, walls[i]);
            AssetDatabase.SaveAssets();
        }

        static void Apply(string materialName, string assetSlug)
        {
            string materialPath = $"Assets/GranivelCity/Generated/{materialName}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null) return;
            string folder = Root + "/" + assetSlug;
            if (!AssetDatabase.IsValidFolder(folder)) return;

            var paths = AssetDatabase.FindAssets("t:Texture2D", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath).ToArray();
            Texture2D diffuse = Find(paths, "diff", "albedo");
            Texture2D normal = Find(paths, "nor_gl", "normal");
            Texture2D ao = Find(paths, "_ao", "ambient_occlusion");
            Texture2D displacement = Find(paths, "disp", "displacement");

            if (diffuse != null)
            {
                SetTexture(material, "_MainTex", diffuse);
                SetTexture(material, "_BaseMap", diffuse);
            }
            if (normal != null)
            {
                SetTexture(material, "_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
            }
            if (ao != null)
            {
                SetTexture(material, "_OcclusionMap", ao);
                if (material.HasProperty("_OcclusionStrength")) material.SetFloat("_OcclusionStrength", 1f);
            }
            if (displacement != null)
            {
                SetTexture(material, "_ParallaxMap", displacement);
                if (material.HasProperty("_Parallax")) material.SetFloat("_Parallax", .018f);
            }
            material.mainTextureScale = materialName == "Asphalt" ? new Vector2(10,10) : new Vector2(4,4);
            EditorUtility.SetDirty(material);
        }

        static Texture2D Find(string[] paths, params string[] terms)
        {
            string path = paths.FirstOrDefault(p => terms.Any(t => p.ToLowerInvariant().Contains(t)) && !p.ToLowerInvariant().Contains("preview") && !p.ToLowerInvariant().Contains("thumb"));
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static void SetTexture(Material material, string property, Texture texture)
        {
            if (material.HasProperty(property)) material.SetTexture(property, texture);
        }
    }
}
#endif
