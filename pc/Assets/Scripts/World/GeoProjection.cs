using System;
using UnityEngine;

namespace GranivelCity
{
    public static class GeoProjection
    {
        public const double EarthRadius = 6378137.0;
        public const double OriginLat = 47.497913;
        public const double OriginLon = 19.054806;
        public const int TerrainZoom = 14;

        private static readonly double originMx = LonToMercatorX(OriginLon);
        private static readonly double originMy = LatToMercatorY(OriginLat);

        public static Vector3 GeoToWorld(double lat, double lon, float elevationMeters = 0f)
        {
            double mx = LonToMercatorX(lon);
            double my = LatToMercatorY(lat);
            return new Vector3((float)(mx - originMx), elevationMeters, (float)(my - originMy));
        }

        public static void WorldToGeo(Vector3 world, out double lat, out double lon)
        {
            lon = MercatorXToLon(originMx + world.x);
            lat = MercatorYToLat(originMy + world.z);
        }

        public static Vector2Int GeoToTile(double lat, double lon, int zoom = TerrainZoom)
        {
            double n = Math.Pow(2.0, zoom);
            int x = (int)Math.Floor((lon + 180.0) / 360.0 * n);
            double latRad = lat * Math.PI / 180.0;
            double mercator = Math.Log(Math.Tan(latRad) + 1.0 / Math.Cos(latRad));
            int y = (int)Math.Floor((1.0 - mercator / Math.PI) / 2.0 * n);
            return new Vector2Int(x, y);
        }

        public static void TileBounds(int x, int y, int zoom, out double south, out double west, out double north, out double east)
        {
            west = TileXToLon(x, zoom);
            east = TileXToLon(x + 1, zoom);
            north = TileYToLat(y, zoom);
            south = TileYToLat(y + 1, zoom);
        }

        public static double TileXToLon(int x, int zoom) => x / Math.Pow(2.0, zoom) * 360.0 - 180.0;

        public static double TileYToLat(int y, int zoom)
        {
            double n = Math.PI - 2.0 * Math.PI * y / Math.Pow(2.0, zoom);
            return 180.0 / Math.PI * Math.Atan(Math.Sinh(n));
        }

        private static double LonToMercatorX(double lon) => EarthRadius * lon * Math.PI / 180.0;
        private static double LatToMercatorY(double lat)
        {
            double clamped = Math.Max(-85.05112878, Math.Min(85.05112878, lat));
            double r = clamped * Math.PI / 180.0;
            return EarthRadius * Math.Log(Math.Tan(Math.PI * 0.25 + r * 0.5));
        }
        private static double MercatorXToLon(double x) => x / EarthRadius * 180.0 / Math.PI;
        private static double MercatorYToLat(double y) => (2.0 * Math.Atan(Math.Exp(y / EarthRadius)) - Math.PI * 0.5) * 180.0 / Math.PI;
    }
}
