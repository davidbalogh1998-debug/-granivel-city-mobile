using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace GranivelCity
{
    public partial class BudapestWorldStreamer
    {
        private void BuildTerrain(TileRuntime tile)
        {
            const int resolution = 49;
            var verts = new Vector3[resolution * resolution];
            var uvs = new Vector2[verts.Length];
            var tris = new int[(resolution - 1) * (resolution - 1) * 6];
            int ti = 0;

            for (int y = 0; y < resolution; y++)
            for (int x = 0; x < resolution; x++)
            {
                float u = x / (float)(resolution - 1);
                float v = y / (float)(resolution - 1);
                double lon = Mathf.Lerp((float)tile.west, (float)tile.east, u);
                double lat = Mathf.Lerp((float)tile.south, (float)tile.north, v);
                float elevation = DecodeElevation(tile.terrainTexture, u, v);
                Vector3 p = GeoProjection.GeoToWorld(lat, lon, elevation - terrainBaseElevation);
                int i = y * resolution + x;
                verts[i] = p;
                uvs[i] = new Vector2(u * 8f, v * 8f);
                if (x < resolution - 1 && y < resolution - 1)
                {
                    int a = i, b = i + 1, c = i + resolution, d = c + 1;
                    tris[ti++] = a; tris[ti++] = c; tris[ti++] = b;
                    tris[ti++] = b; tris[ti++] = c; tris[ti++] = d;
                }
            }

            var mesh = new Mesh { name = $"Terrain {tile.key}" };
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = verts; mesh.triangles = tris; mesh.uv = uvs;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var go = new GameObject("Terrain"); go.transform.SetParent(tile.root.transform);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = BudapestMaterials.Grass;
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            tile.terrainMesh = mesh;
        }

        private void BuildOsm(TileRuntime tile)
        {
            if (string.IsNullOrWhiteSpace(tile.osmXml)) return;
            XDocument doc;
            try { doc = XDocument.Parse(tile.osmXml); }
            catch (Exception e) { Debug.LogWarning("OSM parse failed: " + e.Message); return; }

            foreach (var way in doc.Descendants("way"))
            {
                var tags = way.Elements("tag").Where(t => t.Attribute("k") != null && t.Attribute("v") != null)
                    .ToDictionary(t => (string)t.Attribute("k"), t => (string)t.Attribute("v"));
                var points = new List<GeoPoint>();
                foreach (var nd in way.Elements("nd"))
                {
                    if (double.TryParse((string)nd.Attribute("lat"), NumberStyles.Float, CultureInfo.InvariantCulture, out double lat) &&
                        double.TryParse((string)nd.Attribute("lon"), NumberStyles.Float, CultureInfo.InvariantCulture, out double lon))
                        points.Add(new GeoPoint(lat, lon));
                }
                if (points.Count < 2) continue;

                if (tags.TryGetValue("highway", out string highway)) BuildRoad(tile, points, tags, highway);
                else if (tags.ContainsKey("building") && points.Count >= 4) BuildBuilding(tile, points, tags);
                else if ((tags.TryGetValue("natural", out string natural) && natural == "water") || tags.ContainsKey("waterway")) BuildArea(tile, points, BudapestMaterials.Water, -0.35f, "Water");
                else if (tags.TryGetValue("leisure", out string leisure) && leisure == "park") BuildArea(tile, points, BudapestMaterials.Grass, 0.05f, "Park");
            }
        }

        private void BuildRoad(TileRuntime tile, List<GeoPoint> points, Dictionary<string,string> tags, string highway)
        {
            float width = RoadWidth(highway, tags);
            bool bridge = tags.TryGetValue("bridge", out string bridgeValue) && bridgeValue != "no";
            float extraY = bridge ? 5.5f : 0.15f;
            var world = points.Select(p => WorldOnTerrain(tile, p.lat, p.lon, extraY)).ToList();
            RegisterRoadAnchors(world, highway);
            string roadName = tags.TryGetValue("name", out string n) ? n : highway;
            var mesh = BuildRibbon(world, width);
            if (mesh == null) return;
            var go = new GameObject("Road - " + roadName); go.transform.SetParent(tile.root.transform);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = BudapestMaterials.Asphalt;
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        private void BuildBuilding(TileRuntime tile, List<GeoPoint> points, Dictionary<string,string> tags)
        {
            if (points.Count > 180) return;
            if (Distance(points[0], points[^1]) > 2.5f) return;
            var poly = points.Take(points.Count - 1).Select(p => WorldOnTerrain(tile, p.lat, p.lon, 0f)).ToList();
            if (poly.Count < 3) return;
            float baseY = poly.Average(p => p.y);
            float height = ParseMeters(tags, "height");
            if (height <= 1f && tags.TryGetValue("building:levels", out string levels) && float.TryParse(levels, NumberStyles.Float, CultureInfo.InvariantCulture, out float lv)) height = Mathf.Clamp(lv * 3.05f, 3f, 120f);
            if (height <= 1f) height = Mathf.Lerp(8f, 22f, Mathf.Abs((float)Math.Sin(points[0].lat * 913.7 + points[0].lon * 337.1)));
            string name = tags.TryGetValue("name", out string n) ? n : tags.TryGetValue("building", out string b) ? b : "Building";
            var mesh = ExtrudePolygon(poly, baseY, height);
            if (mesh == null) return;
            var go = new GameObject("Building - " + name); go.transform.SetParent(tile.root.transform);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = new[] { BudapestMaterials.Building(name), BudapestMaterials.Roof };
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        private void BuildArea(TileRuntime tile, List<GeoPoint> points, Material material, float yOffset, string name)
        {
            if (points.Count < 4 || points.Count > 450 || Distance(points[0], points[^1]) > 3f) return;
            var poly = points.Take(points.Count - 1).Select(p => WorldOnTerrain(tile, p.lat, p.lon, yOffset)).ToList();
            var mesh = FlatPolygon(poly);
            if (mesh == null) return;
            var go = new GameObject(name); go.transform.SetParent(tile.root.transform);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        private Vector3 WorldOnTerrain(TileRuntime tile, double lat, double lon, float offset)
        {
            float u = Mathf.InverseLerp((float)tile.west, (float)tile.east, (float)lon);
            float v = Mathf.InverseLerp((float)tile.south, (float)tile.north, (float)lat);
            float elev = DecodeElevation(tile.terrainTexture, u, v) - terrainBaseElevation + offset;
            return GeoProjection.GeoToWorld(lat, lon, elev);
        }

        public float SampleHeightAtWorld(Vector3 world)
        {
            GeoProjection.WorldToGeo(world, out double lat, out double lon);
            var key = GeoProjection.GeoToTile(lat, lon);
            if (!loaded.TryGetValue(key, out var tile) || tile.terrainTexture == null) return 8f;
            float u = Mathf.InverseLerp((float)tile.west, (float)tile.east, (float)lon);
            float v = Mathf.InverseLerp((float)tile.south, (float)tile.north, (float)lat);
            return DecodeElevation(tile.terrainTexture, u, v) - terrainBaseElevation;
        }

        private static float DecodeElevation(Texture2D tex, float u, float v)
        {
            if (tex == null) return 100f;
            Color32 c = tex.GetPixelBilinear(Mathf.Clamp01(u), Mathf.Clamp01(v));
            return c.r * 256f + c.g + c.b / 256f - 32768f;
        }
    }
}
