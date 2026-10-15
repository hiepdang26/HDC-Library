namespace HDC.Ads.Domain
{
    internal readonly struct HDCPrivacyToggles
    {
        internal const string ThisSession = "ads_config of this session";
        internal const string LastSession = "ads_config saved last session";

        private const int SavedFlag = 4;
        private const int ConsentFlag = 1;
        private const int AttFlag = 2;

        internal HDCPrivacyToggles(bool consent, bool att, string source)
        {
            Consent = consent;
            Att = att;
            Source = source;
        }

        internal bool Consent { get; }

        internal bool Att { get; }

        internal string Source { get; }

        internal int Saved => SavedFlag | (Consent ? ConsentFlag : 0) | (Att ? AttFlag : 0);

        internal static HDCPrivacyToggles Of(HDCAdsConfig config, string source) => new HDCPrivacyToggles(config.toggleConsent, config.toggleAtt, source);

        internal static bool TryRead(int saved, out HDCPrivacyToggles toggles)
        {
            if ((saved & SavedFlag) == 0)
            {
                toggles = default;
                return false;
            }

            toggles = new HDCPrivacyToggles((saved & ConsentFlag) != 0, (saved & AttFlag) != 0, LastSession);
            return true;
        }

        internal bool SameAs(HDCPrivacyToggles other) => Consent == other.Consent && Att == other.Att;

        public override string ToString() => $"toggleConsent={Consent} toggleAtt={Att} from {Source}";
    }
}
