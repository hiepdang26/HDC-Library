using System;

namespace HDC.Ads
{
    public interface IAdsConsent
    {
        bool IsDone { get; }

        bool CanRequestAds { get; }

        HDCConsentStatus Status { get; }

        HDCTrackingStatus TrackingStatus { get; }

        bool PrivacyOptionsRequired { get; }

        event Action Done;

        void ShowPrivacyOptions(Action<string> onClosed = null);
    }
}
