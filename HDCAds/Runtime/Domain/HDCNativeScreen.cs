using UnityEngine;

namespace HDC.Ads.Domain
{
    internal readonly struct HDCNativeScreen
    {
        internal HDCNativeScreen(Vector2 unityPixels, Vector2 viewPoints, Vector2 originPoints)
        {
            UnityPixels = unityPixels;
            ViewPoints = viewPoints;
            OriginPoints = originPoints;
        }

        internal Vector2 UnityPixels { get; }

        internal Vector2 ViewPoints { get; }

        internal Vector2 OriginPoints { get; }

        internal bool IsValid => UnityPixels.x > 0f && UnityPixels.y > 0f && ViewPoints.x > 0f && ViewPoints.y > 0f;

        internal Vector2 PixelsPerPoint => IsValid ? new Vector2(UnityPixels.x / ViewPoints.x, UnityPixels.y / ViewPoints.y) : Vector2.one;

        internal Vector2 ToPixels(Vector2 points) => Vector2.Scale(points, PixelsPerPoint);

        internal Vector2 PointFromTop(Vector2 unityPixel)
        {
            Vector2 scale = PixelsPerPoint;
            return new Vector2(unityPixel.x / scale.x, (UnityPixels.y - unityPixel.y) / scale.y);
        }

        internal Vector2Int CustomPosition(Vector2 centerPixels, Vector2 sizePoints)
        {
            Vector2 center = PointFromTop(centerPixels);
            return new Vector2Int(
                Mathf.RoundToInt(center.x - sizePoints.x / 2f - OriginPoints.x),
                Mathf.RoundToInt(center.y - sizePoints.y / 2f - OriginPoints.y));
        }

        internal static HDCNativeScreen Scaled(Vector2 unityPixels, float pixelsPerPoint, Vector2 originPixelsFromTop)
        {
            float scale = pixelsPerPoint > 0f ? pixelsPerPoint : 1f;
            return new HDCNativeScreen(unityPixels, unityPixels / scale, originPixelsFromTop / scale);
        }
    }
}
