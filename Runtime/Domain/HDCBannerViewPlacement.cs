namespace HDC.Ads.Domain
{
    /// <summary>Size and starting position of a Google Mobile Ads banner view.</summary>
    internal enum HDCBannerViewPlacement
    {
        /// <summary>300x250 medium rectangle, at the bottom right until moved.</summary>
        Mrec = 0,

        /// <summary>Adaptive banner across the bottom of the screen.</summary>
        FullBottom = 1,

        /// <summary>Adaptive banner across the top of the screen.</summary>
        FullTop = 2,

        /// <summary>320x50 banner in the top left corner; the other corners follow.</summary>
        TopLeft = 3,
        TopRight = 4,
        BottomLeft = 5,
        BottomRight = 6,
    }
}
