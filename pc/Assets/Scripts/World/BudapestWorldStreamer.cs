using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace GranivelCity
{
    public partial class BudapestWorldStreamer : MonoBehaviour
    {
        public Transform target;
        public int streamRadius = 1;
        public int keepRadius = 2;
        public float terrainBaseElevation = 80f;
        public bool WorldReady { get; private set; }
        public string Status { get; private set; } = "Budapest inicializálása…";

        private readonly Dictionary<Vector2Int, TileRuntime> loaded = new();
        private readonly HashSet<Vector2Int> loading = new();
        private readonly List<RoadAnchor> roadAnchors = new();
        private Vector2Int currentCenter = new(int.MinValue, int.MinValue);
        private string cacheRoot;
        private string packagedDataRoot;

        // Budapest administrative extent, deliberately slightly padded.
        private const double SouthLimit = 47.33;
        private const double NorthLimit = 47.62;
        private const double WestLimit = 18.83;
        private const double EastLimit = 19.36;

        private void Awake()
        {
            cacheRoot = Path.Combine(Application.persistentDataPath, "BudapestOSMCache");
            Directory.CreateDirectory(cacheRoot);

            string external = Environment.GetEnvironmentVariable("GRANIVEL_BUDAPEST_DATA");
            packagedDataRoot = !string.IsNullOrWhiteSpace(external)
                ? external
                : Path.Combine(Application.streamingAssetsPath, "BudapestData");
        }

        public void Begin(Transform player)
        {
            target = player;
            StartCoroutine(LoadInitial());
        }

        private IEnumerator LoadInitial()
        {
            Vector2Int tile = GeoProjection.GeoToTile(GeoProjection.OriginLat, GeoProjection.OriginLon);
            yield return StartCoroutine(LoadTile(tile, true));
            WorldReady = true;
            Status = "Budapest betöltve";
            if (target != null)
            {
                float y = SampleHeightAtWorld(Vector3.zero) + 1.25f;
                target.GetComponent<PlayerController>()?.Teleport(new Vector3(0f, y, 0f));
            }
            RefreshStreaming(true);
        }

        private void Update()
        {
            if (target == null || !WorldReady) return;
            RefreshStreaming(false);
        }

        private void RefreshStreaming(bool force)
        {
            GeoProjection.WorldToGeo(target.position, out double lat, out double lon);
            var center = GeoProjection.GeoToTile(lat, lon);
            if (!force && center == currentCenter) return;
            currentCenter = center;

            for (int dy = -streamRadius; dy <= streamRadius; dy++)
            for (int dx = -streamRadius; dx <= streamRadius; dx++)
            {
                Vector2Int key = center + new Vector2Int(dx, dy);
                if (!loaded.ContainsKey(key) && !loading.Contains(key) && TileIntersectsBudapest(key))
                    StartCoroutine(LoadTile(key, false));
            }

            foreach (var key in loaded.Keys.ToList())
            {
                if (Mathf.Abs(key.x - center.x) > keepRadius || Mathf.Abs(key.y - center.y) > keepRadius)
                {
                    if (loaded[key].root != null) Destroy(loaded[key].root);
                    loaded.Remove(key);
                }
            }
        }

        private bool TileIntersectsBudapest(Vector2Int tile)
        {
            GeoProjection.TileBounds(tile.x, tile.y, GeoProjection.TerrainZoom, out double s, out double w, out double n, out double e);
            return n >= SouthLimit && s <= NorthLimit && e >= WestLimit && w <= EastLimit;
        }

        private IEnumerator LoadTile(Vector2Int key, bool initial)
        {
            loading.Add(key);
            GeoProjection.TileBounds(key.x, key.y, GeoProjection.TerrainZoom, out double south, out double west, out double north, out double east);
            var tile = new TileRuntime(key, south, west, north, east);
            var root = new GameObject($"Budapest Tile {key.x}_{key.y}");
            root.transform.SetParent(transform);
            tile.root = root;

            Status = initial ? "Budapest domborzat betöltése…" : $"Térképcella {key.x}/{key.y}";
            yield return StartCoroutine(LoadTerrain(tile));
            BuildTerrain(tile);

            Status = initial ? "Valós utcák és épületek betöltése…" : Status;
            yield return StartCoroutine(LoadOsm(tile));
            BuildOsm(tile);

            loaded[key] = tile;
            loading.Remove(key);
        }

        private IEnumerator LoadTerrain(TileRuntime tile)
        {
            string path = Path.Combine(cacheRoot, $"terrain_{tile.key.x}_{tile.key.y}.png");
            string packaged = Path.Combine(packagedDataRoot, "terrain", GeoProjection.TerrainZoom.ToString(), $"{tile.key.x}_{tile.key.y}.png");
            byte[] bytes = null;
            if (File.Exists(packaged)) bytes = File.ReadAllBytes(packaged);
            else if (File.Exists(path)) bytes = File.ReadAllBytes(path);
            else
            {
                string url = $"https://s3.amazonaws.com/elevation-tiles-prod/terrarium/{GeoProjection.TerrainZoom}/{tile.key.x}/{tile.key.y}.png";
                using var req = UnityWebRequest.Get(url);
                req.timeout = 30;
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    bytes = req.downloadHandler.data;
                    try { File.WriteAllBytes(path, bytes); } catch { }
                }
            }

            if (bytes != null && bytes.Length > 0)
            {
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
                if (tex.LoadImage(bytes, false)) tile.terrainTexture = tex;
                else Destroy(tex);
            }
        }

        private IEnumerator LoadOsm(TileRuntime tile)
        {
            string path = Path.Combine(cacheRoot, $"osm_{tile.key.x}_{tile.key.y}.xml");
            string packaged = Path.Combine(packagedDataRoot, "osm", GeoProjection.TerrainZoom.ToString(), $"{tile.key.x}_{tile.key.y}.xml");
            if (File.Exists(packaged))
            {
                tile.osmXml = File.ReadAllText(packaged);
                yield break;
            }
            if (File.Exists(path))
            {
                tile.osmXml = File.ReadAllText(path);
                yield break;
            }

            string bbox = string.Join(",", F(tile.south), F(tile.west), F(tile.north), F(tile.east));
            string query = "[out:xml][timeout:45];(" +
                           $"way[highway]({bbox});" +
                           $"way[building]({bbox});" +
                           $"way[natural=water]({bbox});" +
                           $"way[waterway=riverbank]({bbox});" +
                           $"way[leisure=park]({bbox});" +
                           $"node[tourism]({bbox});node[historic]({bbox});node[amenity]({bbox});" +
                           ");out geom;";

            var form = new WWWForm();
            form.AddField("data", query);
            using var req = UnityWebRequest.Post("https://overpass-api.de/api/interpreter", form);
            req.timeout = 60;
            req.SetRequestHeader("User-Agent", "GranivelCity-BudapestRP/0.2");
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
            {
                tile.osmXml = req.downloadHandler.text;
                try { File.WriteAllText(path, tile.osmXml); } catch { }
            }
            else
            {
                Debug.LogWarning($"OSM tile download failed {tile.key}: {req.error}");
                tile.osmXml = string.Empty;
            }
        }

    }
}
