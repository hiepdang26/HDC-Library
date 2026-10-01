using HDC.Ads.Internal;
using UnityEngine;

namespace HDC.Ads
{
    /// <summary>
    /// A native full-screen ad for players coming back to the app: it loads when the app goes to the
    /// background and shows once loaded. After a full-screen ad opened, a banner was tapped, or
    /// <see cref="Block"/>, the next trip to the background shows nothing: the player left because of an ad
    /// or of the game itself.
    /// </summary>
    public sealed class HDCAppResumeAds
    {
        private const string InstanceId = "native_resume";

        private HDCNativeFullscreenSource source;
        private bool blocked;
        private bool showing;
        private bool showWhenLoaded;

        internal HDCAppResumeAds()
        {
            HDCAds.FullscreenOpening += Block;
            HDCAds.BannerClicked += Block;
            HDCAdsSdk.AdEvent += OnAdEvent;
        }

        public bool IgnoreAds { get; set; }

        // For the debug panel: the instance the channel loads, once it started.
        internal static string DebugInstanceId => InstanceId;

        internal static string DebugAdUnitId => Channel.adUnitId;

        internal bool IsStarted => source != null;

        public bool IsInitialized => source != null;

        private static HDCAdsConfig.AppResumeChannel Channel => HDCAds.Config.appResumeChannel ?? new HDCAdsConfig.AppResumeChannel();

        private static bool IsDisabled => !Channel.isEnabled || HDCAds.IsAdsRemoved;

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
                .Gate("Ads Removed", HDCAds.IsAdsRemoved);

        internal void OnSdkInitialized()
        {
            if (Channel.autoInit)
                Start();
        }

        private void Start()
        {
            if (source != null || IsDisabled || string.IsNullOrEmpty(Channel.adUnitId))
                return;

            var layouts = new HDCLayoutPicker(HDCAds.CoreConfig, Channel.layoutGroup);
            source = new HDCNativeFullscreenSource(InstanceId, Channel.adUnitId, false, layouts);
            HDCMainThread.EnsureCreated();
            HDCMainThread.ApplicationPaused += OnApplicationPaused;
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

            if (adEvent.id == InstanceId && adEvent.format == HDCAdFormat.Fullscreen)
            {
                if (adEvent.type == HDCAdEventType.Loaded && showWhenLoaded)
                {
                    showWhenLoaded = false;
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
