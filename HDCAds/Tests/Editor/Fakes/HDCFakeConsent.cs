using System;
using HDC.Ads.Ports;

namespace HDC.Ads.Tests
{
    internal sealed class HDCFakeConsent : IConsentProvider
    {
        private Action<string> pendingUpdate;
        private Action<string> pendingForm;

        public bool CanRequestAds => Allows;

        public HDCConsentStatus Status => Allows ? HDCConsentStatus.Obtained : HDCConsentStatus.Required;

        public bool PrivacyOptionsRequired { get; set; }

        public string PurposeConsents { get; set; } = string.Empty;

        internal bool Allows { get; set; } = true;

        internal bool AnswersAtOnce { get; set; } = true;

        internal string UpdateError { get; set; }

        internal int Updates { get; private set; }

        internal bool? AskedInEea { get; private set; }

        internal int FormsShown { get; private set; }

        internal int PrivacyFormsShown { get; private set; }

        internal int Resets { get; private set; }

        public void Update(bool debugInEea, Action<string> onDone)
        {
            Updates++;
            AskedInEea = debugInEea;
            if (AnswersAtOnce)
                onDone?.Invoke(UpdateError);
            else
                pendingUpdate = onDone;
        }

        public void ShowFormIfRequired(Action<string> onDone)
        {
            FormsShown++;
            pendingForm = onDone;
        }

        public void ShowPrivacyOptions(Action<string> onDone)
        {
            PrivacyFormsShown++;
            onDone?.Invoke(null);
        }

        public void Reset() => Resets++;

        internal void AnswerUpdate(string error = null)
        {
            Action<string> done = pendingUpdate;
            pendingUpdate = null;
            done?.Invoke(error);
        }

        internal void CloseForm(bool allow, string error = null)
        {
            Allows = allow;
            Action<string> done = pendingForm;
            pendingForm = null;
            done?.Invoke(error);
        }
    }
}
