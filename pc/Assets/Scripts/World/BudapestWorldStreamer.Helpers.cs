using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace GranivelCity
{
    public partial class BudapestWorldStreamer
    {
        public bool TryGetRoadSpawnNear(Vector3 around, float radius, out Vector3 position, out Quaternion rotation)
        {
            position = around; rotation = Quaternion.identity;
            if (roadAnchors.Count == 0) return false;
            float r2 = radius * radius;
            int start = UnityEngine.Random.Range(0, roadAnchors.Count);
            for (int k = 0; k < roadAnchors.Count; k++)
            {
                RoadAnchor a = roadAnchors[(start + k) % roadAnchors.Count];
                if ((a.position - around).sqrMagnitude > r2) continue;
                position = a.position + Vector3.up * 0.75f;
                rotation = Quaternion.LookRotation(a.forward.sqrMagnitude > 0.01f ? a.forward : Vector3.forward, Vector3.up);
                return true;
            }
            return false;
        }

        private void RegisterRoadAnchors(List<Vector3> world, string highway)
        {
            if (highway == "footway" || highway == "path" || highway == "cycleway" || world.Count < 2) return;
            for (int i = 0; i < world.Count - 1; i += 2)
            {
                Vector3 f = Vector3.ProjectOnPlane(world[i + 1] - world[i], Vector3.up).normalized;
                if (f.sqrMagnitude < 0.01f) continue;
                roadAnchors.Add(new RoadAnchor((world[i] + world[i + 1]) * 0.5f, f));
            }
            if (roadAnchors.Count > 20000) roadAnchors.RemoveRange(0, roadAnchors.Count - 20000);
        }

        private static float RoadWidth(string type, Dictionary<string,string> tags)
        {
            float parsed = ParseMeters(tags, "width"); if (parsed > 1f) return Mathf.Clamp(parsed, 2.5f, 32f);
            return type switch
            {
                "motorway" => 14f, "trunk" => 12f, "primary" => 10f, "secondary" => 8.5f,
                "tertiary" => 7.5f, "residential" => 6.2f, "living_street" => 5.2f,
                "service" => 4.5f, "footway" => 2.0f, "path" => 1.5f, "cycleway" => 2.2f,
                _ => 5.5f
            };
        }

        private static float ParseMeters(Dictionary<string,string> tags, string key)
        {
            if (!tags.TryGetValue(key, out string raw)) return 0f;
            raw = raw.Replace("m", "").Trim().Split(';')[0];
            return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : 0f;
        }

        private static Mesh BuildRibbon(List<Vector3> pts, float width)
        {
            if (pts.Count < 2) return null;
            var verts = new Vector3[pts.Count * 2]; var uv = new Vector2[verts.Length]; var tris = new int[(pts.Count - 1) * 6];
            float dist = 0f; int ti = 0;
            for (int i = 0; i < pts.Count; i++)
            {
                Vector3 tangent = i == 0 ? pts[1] - pts[0] : i == pts.Count - 1 ? pts[^1] - pts[^2] : pts[i + 1] - pts[i - 1];
                tangent.y = 0f; if (tangent.sqrMagnitude < 0.001f) tangent = Vector3.forward; tangent.Normalize();
                Vector3 side = Vector3.Cross(Vector3.up, tangent) * width * 0.5f;
                verts[i * 2] = pts[i] - side; verts[i * 2 + 1] = pts[i] + side;
                if (i > 0) dist += Vector3.Distance(pts[i - 1], pts[i]);
                uv[i * 2] = new Vector2(0f, dist / 8f); uv[i * 2 + 1] = new Vector2(1f, dist / 8f);
                if (i < pts.Count - 1)
                {
                    int a=i*2,b=a+1,c=a+2,d=a+3;
                    tris[ti++]=a;tris[ti++]=c;tris[ti++]=b; tris[ti++]=b;tris[ti++]=c;tris[ti++]=d;
                }
            }
            var mesh = new Mesh { name="RoadMesh", indexFormat=UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.vertices=verts; mesh.triangles=tris; mesh.uv=uv; mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }

        private static Mesh ExtrudePolygon(List<Vector3> poly, float baseY, float height)
        {
            if (poly.Count < 3) return null;
            // Fan roof is safe for the mostly simple OSM footprints; walls remain accurate even for concave shapes.
            int n=poly.Count; var verts=new List<Vector3>(n*4); var tris=new List<int>(n*6);
            for(int i=0;i<n;i++) { var p=poly[i]; verts.Add(new Vector3(p.x,baseY,p.z)); verts.Add(new Vector3(p.x,baseY+height,p.z)); }
            for(int i=0;i<n;i++) { int j=(i+1)%n; int a=i*2,b=a+1,c=j*2,d=c+1; tris.Add(a);tris.Add(b);tris.Add(c); tris.Add(c);tris.Add(b);tris.Add(d); }
            int roofCenter=verts.Count; Vector3 center=Vector3.zero; foreach(var p in poly) center+=p; center/=n; verts.Add(new Vector3(center.x,baseY+height,center.z));
            for(int i=0;i<n;i++){ int j=(i+1)%n; tris.Add(roofCenter);tris.Add(i*2+1);tris.Add(j*2+1); }
            var mesh=new Mesh{name="BuildingMesh",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32}; mesh.SetVertices(verts); mesh.SetTriangles(tris,0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }

        private static Mesh FlatPolygon(List<Vector3> poly)
        {
            if(poly.Count<3) return null; Vector3 center=Vector3.zero; foreach(var p in poly)center+=p; center/=poly.Count;
            var verts=new List<Vector3>{center}; verts.AddRange(poly); var tris=new List<int>();
            for(int i=0;i<poly.Count;i++){int j=(i+1)%poly.Count;tris.Add(0);tris.Add(i+1);tris.Add(j+1);} var m=new Mesh{name="AreaMesh"};m.SetVertices(verts);m.SetTriangles(tris,0);m.RecalculateNormals();m.RecalculateBounds();return m;
        }

        private static float Distance(GeoPoint a, GeoPoint b) => Vector3.Distance(GeoProjection.GeoToWorld(a.lat,a.lon), GeoProjection.GeoToWorld(b.lat,b.lon));
        private static string F(double v) => v.ToString("0.########", CultureInfo.InvariantCulture);

        private readonly struct GeoPoint { public readonly double lat,lon; public GeoPoint(double lat,double lon){this.lat=lat;this.lon=lon;} }
        private readonly struct RoadAnchor { public readonly Vector3 position, forward; public RoadAnchor(Vector3 position, Vector3 forward){this.position=position;this.forward=forward;} }
        private sealed class TileRuntime
        {
            public readonly Vector2Int key; public readonly double south,west,north,east; public GameObject root; public Texture2D terrainTexture; public Mesh terrainMesh; public string osmXml;
            public TileRuntime(Vector2Int key,double south,double west,double north,double east){this.key=key;this.south=south;this.west=west;this.north=north;this.east=east;}
        }
    }
}
