using HDC.Ads.Domain;
using HDC.Ads.Ports;

namespace HDC.Ads.Logic
{
    internal sealed partial class HDCAppResumeAds : IAppResumeAds, IAdChannel
    {
        private readonly HDCAdsContext context;
        private HDCFullscreenSource source;
        private bool blocked;
        private bool showing;
        private bool showWhenLoaded;

        internal HDCAppResumeAds(HDCAdsContext context)
        {
            this.context = context;
            context.FullscreenOpening += Block;
            context.BannerClicked += Block;
            context.Sdk.AdEvent += OnAdEvent;
        }

        public bool IgnoreAds { get; set; }

        public bool IsInitialized => source != null;

        private HDCAdsConfig.AppResumeChannel Channel => context.Config.appResumeChannel ?? new HDCAdsConfig.AppResumeChannel();

        private bool IsDisabled => !Channel.isEnabled || context.IsAdsRemoved;

        public void Initialize()
        {
            if (!Channel.autoInit)
                Start();
        }

        public void Block()
        {
            if (source == null)
                return;
            blocked = true;
#if UNITY_IOS
            pauseSeenWhileBlocked = false;
#endif
        }

        string IAdChannel.Key => "AR";

        string IAdChannel.Title => "AppResume";

        void IAdChannel.OnSdkInitialized()
        {
            if (Channel.autoInit)
                Start();
        }

        void IAdChannel.InitializeAll() => Initialize();

        void IAdChannel.OnAdsRemoved()
        {
        }

        private void Start()
        {
            if (source != null || IsDisabled)
                return;

            HDCAdPlan plan = context.Groups.ResumePlan();
            if (plan == null)
                return;
            source = new HDCFullscreenSource(plan.Network.CreateFullscreen(plan), plan.Network, context.Log);
            context.MainThread.ApplicationPaused += OnApplicationPaused;
        }

        private void OnApplicationPaused(bool paused)
        {
            if (blocked)
            {
#if UNITY_IOS
                if (paused)
                {
                    pauseSeenWhileBlocked = true;
                    return;
                }
#endif
                blocked = false;
#if UNITY_IOS
                pauseSeenWhileBlocked = false;
#endif
                return;
            }

            if (!paused || showing || IsDisabled || IgnoreAds || context.HasOverlayAd)
                return;

            showWhenLoaded = true;
            source.Load();
        }

        private void OnAdEvent(HDCAdEvent adEvent)
        {
            if (source == null)
                return;

            if (adEvent.id == source.Id && adEvent.format == source.Format)
            {
                if (adEvent.type == HDCAdEventType.Loaded && showWhenLoaded)
                {
                    showWhenLoaded = false;
                    context.Placements.Record(source.Id, HDCAdChannel.AppResume, "", source.Network.RevenueNetwork);
                    showing = source.Show(null, _ => showing = false);
                }

                return;
            }

#if UNITY_IOS
            UnblockIfStayed(adEvent);
#endif
        }

#if UNITY_IOS
        private bool pauseSeenWhileBlocked;

        private void UnblockIfStayed(HDCAdEvent adEvent)
        {
            bool fullscreen = adEvent.format == HDCAdFormat.Interstitial || adEvent.format == HDCAdFormat.Fullscreen
                || adEvent.format == HDCAdFormat.Rewarded || adEvent.format == HDCAdFormat.AppOpen;
            if (fullscreen && adEvent.type == HDCAdEventType.Closed && blocked && !pauseSeenWhileBlocked)
                blocked = false;
        }
#endif
    }
}
