using System;
using HDC.Ads.Diagnostics;
using HDC.Ads.Domain;
using HDC.Ads.Infrastructure;
using UnityEngine;

namespace HDC.Ads.Logic
{
    /// <summary>The app launch channel behind <see cref="IAppLaunchAds"/>.</summary>
    internal sealed class HDCAppLaunchAds : IAppLaunchAds
    {
        // Completes anyway if the ad never reports closing.
        private const float CloseFallbackSeconds = 15f;
        private const float ReadyCheckInterval = 0.25f;

        private bool startRequested;
        private bool clockStarted;
        private float clockStart;
        private bool ticking;
        private bool showing;
        private float showStart;
        private float nextReadyCheck;
        private HDCFullscreenGroup group;

        internal HDCAppLaunchAds()
        {
        }

        /// <summary>Right before the launch ad shows, or right before completing without one.</summary>
        public event Action BeforeShow;

        /// <summary>The launch is done: the ad closed, failed, timed out, or could not show.</summary>
        public event Action Completed;

        public bool IgnoreAds { get; set; }

        public bool IsCompleted { get; private set; }

        public bool IsBeforeShowRaised { get; private set; }

        public bool CanShow => !IsDisabled && !IgnoreAds && clockStarted && !TimedOut && MinimumWaitPassed && (Group()?.IsReady ?? false);

        private static HDCAdsConfig.AppLaunchChannel Channel => HDCAds.Config.appLaunchChannel ?? new HDCAdsConfig.AppLaunchChannel();

        private static bool IsDisabled => !Channel.isEnabled || HDCAds.IsAdsRemoved;

        private float Elapsed => clockStarted ? Time.unscaledTime - clockStart : 0f;

        private static float MinimumWait => Channel.minWaitSeconds > 0 ? Channel.minWaitSeconds : 5f;

        private static float Timeout =>
            Channel.timeoutSeconds <= 0 || Channel.timeoutSeconds < MinimumWait ? MinimumWait + 5f : Channel.timeoutSeconds;

        private bool MinimumWaitPassed => clockStarted && Elapsed >= MinimumWait;

        private bool TimedOut => clockStarted && Elapsed >= Timeout;

        /// <summary>
        /// Starts the launch clock and loading, when the channel does not on its own (autoInit off). Called
        /// before the SDK is ready, it starts once the SDK is.
        /// </summary>
        public void Initialize()
        {
            if (!HDCAds.IsInitialized)
            {
                startRequested = true;
                return;
            }

            if (!Channel.autoInit)
                Start();
        }

        /// <summary>Configs, clock and state, for the debug panel. It reads the group without making it.</summary>
        internal HDCDebugInfo Describe()
        {
            HDCAdCoreConfig.Comeback comeback = HDCAds.CoreConfig.comebackChannel ?? new HDCAdCoreConfig.Comeback();
            HDCDebugInfo info = new HDCDebugInfo()
                .Section("Configs")
                .Needed("Enabled", Channel.isEnabled)
                .Line("Auto Init", Channel.autoInit)
                .Line("Minimum Wait (s)", MinimumWait)
                .Line("Timeout (s)", Timeout)
                .Line("Launch Ad", comeback.launchAdType == 0 ? "Force ad group " + comeback.launchForceAdGroupName : "App open")
                .Section("Runtime")
                .Line("Clock Started", clockStarted)
                .Line("Elapsed (s)", Elapsed)
                .Line("Showing", showing)
                .Line("Completed", IsCompleted)
                .Line("Before Show Raised", IsBeforeShowRaised)
                .Line("Ignore Ads", IgnoreAds)
                .Section("Gates")
                .Gate("Disabled", IsDisabled)
                .Gate("Ads Removed", HDCAds.IsAdsRemoved)
                .Section("Group");
            if (group != null)
                group.DescribeTo(info);
            else
                info.Add("State", "Not started", HDCDebugTone.Muted);
            return info;
        }

        internal void OnSdkInitialized()
        {
            if (Channel.autoInit || startRequested)
                Start();
        }

        private void Start()
        {
            if (clockStarted)
                return;

            clockStarted = true;
            clockStart = Time.unscaledTime;
            if (!IsDisabled)
                Group()?.Initialize();
            if (!ticking)
            {
                ticking = true;
                HDCMainThread.EnsureCreated();
                HDCMainThread.Ticked += Tick;
            }
        }

        private void Tick()
        {
            if (IsCompleted)
                return;

            if (showing)
            {
                if (Time.unscaledTime - showStart >= CloseFallbackSeconds)
                    Complete("the ad did not report closing");
                return;
            }

            // Nothing to wait for.
            if (IsDisabled || IgnoreAds || Group() == null || Group().IsEmpty)
            {
                Complete("no launch ad");
                return;
            }

            if (!MinimumWaitPassed)
                return;
            if (TimedOut)
            {
                Complete("timed out");
                return;
            }

            if (Time.unscaledTime < nextReadyCheck)
                return;
            nextReadyCheck = Time.unscaledTime + ReadyCheckInterval;
            if (!Group().IsReady)
                return;

            RaiseBeforeShow();
            showing = true;
            showStart = Time.unscaledTime;
            bool forceAd = HDCAds.CoreConfig.comebackChannel?.launchAdType == 0;
            bool shown = Group().Show(
                HDCAdChannel.AppLaunch,
                "",
                null,
                () =>
                {
                    if (forceAd)
                        HDCAds.Channels.ForceAd.CountImpression("app_launch");
                },
                _ => Complete("closed"));
            if (!shown)
                Complete("show failed");
        }

        private void Complete(string reason)
        {
            if (IsCompleted)
                return;

            HDCAdsLog.Info("app launch complete: " + reason);
            RaiseBeforeShow();
            IsCompleted = true;
            showing = false;
            if (ticking)
            {
                ticking = false;
                HDCMainThread.Ticked -= Tick;
            }

            HDCAdsLog.Run(Completed);
        }

        private void RaiseBeforeShow()
        {
            if (IsBeforeShowRaised)
                return;
            IsBeforeShowRaised = true;
            HDCAdsLog.Run(BeforeShow);
        }

        /// <summary>The launch ad's group once the channel made it, for the debug panel.</summary>
        internal HDCFullscreenGroup ExistingGroup => group;

        private HDCFullscreenGroup Group()
        {
            if (group != null)
                return group;

            HDCAdCoreConfig.Comeback comeback = HDCAds.CoreConfig.comebackChannel ?? new HDCAdCoreConfig.Comeback();
            group = comeback.launchAdType == 0 ? HDCAds.ForceAdGroup(comeback.launchForceAdGroupName) : HDCAds.AppOpenGroup();
            return group;
        }
    }
}
