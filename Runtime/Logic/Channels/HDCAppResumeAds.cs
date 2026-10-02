using HDC.Ads.Diagnostics;
using HDC.Ads.Domain;
using HDC.Ads.Ports;
using UnityEngine;

namespace HDC.Ads.Logic
{
    /// <summary>The app resume channel behind <see cref="IAppResumeAds"/>.</summary>
    internal sealed class HDCAppResumeAds : IAppResumeAds
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

        internal string DebugAdUnitId => Channel.adUnitId;

        internal bool IsStarted => source != null;

        public bool IsInitialized => source != null;

        private HDCAdsConfig.AppResumeChannel Channel => context.Config.appResumeChannel ?? new HDCAdsConfig.AppResumeChannel();

        private bool IsDisabled => !Channel.isEnabled || context.IsAdsRemoved;

        /// <summary>Starts the channel when it does not start on its own (autoInit off).</summary>
        public void Initialize()
        {
            if (!Channel.autoInit)
                Start();
        }

        /// <summary>Skips the next resume ad, before the game sends the player out of the app.</summary>
        public void Block()
        {
            if (source == null)
                return;
            blocked = true;
#if UNITY_IOS
            pauseSeenWhileBlocked = false;
#endif
        }

        /// <summary>Configs and state, for the debug panel.</summary>
        internal HDCDebugInfo Describe() =>
            new HDCDebugInfo()
                .Section("Configs")
                .Needed("Enabled", Channel.isEnabled)
                .Line("Auto Init", Channel.autoInit)
                .Line("Ad Unit ID", Channel.adUnitId)
                .Line("Layout Group", Channel.layoutGroup)
                .Section("Runtime")
                .Line("Started", IsInitialized)
                .Line("Blocked For Next Resume", blocked)
                .Line("Showing", showing)
                .Line("Shows Once Loaded", showWhenLoaded)
                .Line("Ignore Ads", IgnoreAds)
                .Section("Gates")
                .Gate("Disabled", IsDisabled)
                .Gate("Ads Removed", context.IsAdsRemoved);

        internal void OnSdkInitialized()
        {
            if (Channel.autoInit)
                Start();
        }

        private void Start()
        {
            if (source != null || IsDisabled)
                return;

            HDCAdPlan plan = context.Groups.ResumePlan();
            if (plan == null)
                return;
            source = new HDCFullscreenSource(plan.Network.CreateFullscreen(plan), plan.Network);
            context.MainThread.ApplicationPaused += OnApplicationPaused;
        }

        private void OnApplicationPaused(bool paused)
        {
            if (blocked)
            {
#if UNITY_IOS
                // iOS pauses Unity while its own full-screen ads show; the block lasts until the player comes
                // back from a real trip out of the app.
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

            if (!paused || showing || IsDisabled || IgnoreAds)
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

        // A full-screen ad closed without the app leaving: nothing to skip anymore.
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
