using System;
using HDC.Ads.Ports;

namespace HDC.Ads.Infrastructure
{
    internal sealed class HDCNoConsentNeeded : IConsentProvider
    {
        public bool CanRequestAds => true;

        public HDCConsentStatus Status => HDCConsentStatus.NotRequired;

        public bool PrivacyOptionsRequired => false;

        public string PurposeConsents => string.Empty;

        public void Update(bool debugInEea, Action<string> onDone) => onDone?.Invoke(null);

        public void ShowFormIfRequired(Action<string> onDone) => onDone?.Invoke(null);

        public void ShowPrivacyOptions(Action<string> onDone) => onDone?.Invoke("no consent form on this platform");

        public void Reset()
        {
        }
    }
}
