using UnityEditor;

namespace HDC.Tool.Editor
{
    internal enum HDCOrientation
    {
        Portrait,
        Landscape,
        Other,
    }

    internal static class HDCOrientations
    {
        internal static HDCOrientation Current =>
            Of(PlayerSettings.defaultInterfaceOrientation, PlayerSettings.allowedAutorotateToPortrait, PlayerSettings.allowedAutorotateToPortraitUpsideDown,
                PlayerSettings.allowedAutorotateToLandscapeLeft, PlayerSettings.allowedAutorotateToLandscapeRight);

        internal static string Title(this HDCOrientation orientation) =>
            orientation == HDCOrientation.Portrait ? "Portrait" : orientation == HDCOrientation.Landscape ? "Landscape" : "Auto-rotation, every side";

        internal static HDCOrientation Of(UIOrientation start, bool portrait, bool upsideDown, bool left, bool right)
        {
            switch (start)
            {
                case UIOrientation.Portrait:
                case UIOrientation.PortraitUpsideDown:
                    return HDCOrientation.Portrait;
                case UIOrientation.LandscapeLeft:
                case UIOrientation.LandscapeRight:
                    return HDCOrientation.Landscape;
            }

            bool tall = portrait || upsideDown;
            bool wide = left || right;
            return tall && !wide ? HDCOrientation.Portrait : wide && !tall ? HDCOrientation.Landscape : HDCOrientation.Other;
        }

        internal static void Apply(HDCOrientation orientation)
        {
            if (orientation == HDCOrientation.Other)
                return;
            bool landscape = orientation == HDCOrientation.Landscape;
            if (landscape)
            {
                PlayerSettings.allowedAutorotateToLandscapeLeft = true;
                PlayerSettings.allowedAutorotateToLandscapeRight = true;
                PlayerSettings.allowedAutorotateToPortrait = false;
            }
            else
            {
                PlayerSettings.allowedAutorotateToPortrait = true;
                PlayerSettings.allowedAutorotateToLandscapeLeft = false;
                PlayerSettings.allowedAutorotateToLandscapeRight = false;
            }

            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.defaultInterfaceOrientation = landscape ? UIOrientation.AutoRotation : UIOrientation.Portrait;
        }
    }
}
