using System;

namespace HDC.Ads.Ports
{
    internal interface IConsentProvider
    {
        bool CanRequestAds { get; }

        HDCConsentStatus Status { get; }

        bool PrivacyOptionsRequired { get; }

        string PurposeConsents { get; }

        void Update(bool debugInEea, Action<string> onDone);

        void ShowFormIfRequired(Action<string> onDone);

        void ShowPrivacyOptions(Action<string> onDone);

        void Reset();
    }
}
