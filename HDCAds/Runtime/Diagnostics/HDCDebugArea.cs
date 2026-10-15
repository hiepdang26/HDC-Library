using System;
using UnityEngine;

namespace HDC.Ads.Diagnostics
{
    internal sealed class HDCDebugArea
    {
        private readonly Func<Rect, string> place;

        internal HDCDebugArea(string label, Vector2 sizePoints, bool resizable, Func<Rect, string> place)
        {
            Label = label ?? string.Empty;
            SizePoints = sizePoints;
            Resizable = resizable;
            this.place = place;
        }

        internal string Label { get; }

        internal Vector2 SizePoints { get; }

        internal bool Resizable { get; }

        internal float? PixelsPerPoint { get; set; }

        internal Rect? Placed { get; set; }

        internal Vector2? Center { get; set; }

        internal HDCAdPosition? Preset { get; set; }

        internal string Place(Rect screenRect) => place?.Invoke(screenRect);

        internal Rect? Locate(float pixelsPerPoint, Rect safeArea)
        {
            Vector2 size = SizePoints * pixelsPerPoint;
            if (Placed.HasValue)
                return Placed.Value;
            if (Center.HasValue)
                return new Rect(Center.Value - size / 2f, size);
            return Preset.HasValue ? PresetRect(Preset.Value, size, safeArea) : (Rect?)null;
        }

        internal static Rect PresetRect(HDCAdPosition preset, Vector2 size, Rect safeArea)
        {
            float left = safeArea.xMin;
            float middle = safeArea.center.x - size.x / 2f;
            float right = safeArea.xMax - size.x;
            float bottom = safeArea.yMin;
            float top = safeArea.yMax - size.y;
            switch (preset)
            {
                case HDCAdPosition.Top:
                    return new Rect(middle, top, size.x, size.y);
                case HDCAdPosition.TopLeft:
                    return new Rect(left, top, size.x, size.y);
                case HDCAdPosition.TopRight:
                    return new Rect(right, top, size.x, size.y);
                case HDCAdPosition.BottomLeft:
                    return new Rect(left, bottom, size.x, size.y);
                case HDCAdPosition.BottomRight:
                    return new Rect(right, bottom, size.x, size.y);
                case HDCAdPosition.Center:
                    return new Rect(middle, safeArea.center.y - size.y / 2f, size.x, size.y);
                default:
                    return new Rect(middle, bottom, size.x, size.y);
            }
        }
    }
}
