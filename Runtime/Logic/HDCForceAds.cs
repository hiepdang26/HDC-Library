using System;
using System.Collections.Generic;
using HDC.Ads.Internal;
using UnityEngine;

namespace HDC.Ads
{
    /// <summary>The force ad channel behind <see cref="IForceAds"/>.</summary>
    internal sealed class HDCForceAds : IForceAds
    {
        private const string TotalImpressionsKey = "fa_total_impression";

        private bool firstAd = true;
        private bool breakRunning;
        private bool breakAttempting;
        private bool breakNoticeSent;
        private float breakElapsed;
        private int breakResetFrame = -1;

        internal HDCForceAds()
        {
            HDCAds.FullscreenOpening += () =>
            {
                // Any full-screen ad starts the break over, at most once per frame.
                if (breakRunning && breakResetFrame != Time.frameCount)
                    ResetBreakCycle(false);
            };
        }

        /// <summary>Blocks every force ad while true.</summary>
        public bool IgnoreAds { get; set; }

        /// <summary>The break ad shows in <c>seconds</c>: (position, seconds).</summary>
        public event Action<string, int> BreakAdNotice;

        /// <summary>The break ad is about to show at the position.</summary>
        public event Action<string> BreakAdShown;

        /// <summary>The break ad at the position closed.</summary>
        public event Action<string> BreakAdClosed;

        /// <summary>The break ad could not show: (position, reason). Its timer starts over.</summary>
        public event Action<string, string> BreakAdShowFailed;

        public bool IsBreakAdRunning => breakRunning;

        private static HDCAdsConfig.ForceAdChannel Channel => HDCAds.Config.forceAdChannel ?? new HDCAdsConfig.ForceAdChannel();

        private static bool IsDisabled => !Channel.isEnabled || HDCAds.IsAdsRemoved;

        /// <summary>
        /// Shows the force ad of <paramref name="position"/> if the position may show one now.
        /// <paramref name="onDone"/> runs once the ad closes, or right away when none shows.
        /// </summary>
        public bool Show(string position, Action onDone = null)
        {
            if (!Allowed(position, false, out string reason))
            {
                HDCAdsLog.Info($"force ad {position} blocked: {reason}");
                Run(onDone);
                return false;
            }

            HDCFullscreenGroup group = HDCAds.ForceAdGroup(HDCAds.CoreConfig.ForceAdGroupAt(position));
            bool shown = group != null && group.Show(HDCAdChannel.ForceAd, position, null, () => CountImpression(position), _ => Run(onDone));
            if (!shown)
                Run(onDone);
            return shown;
        }

        /// <summary>True when the position may show an ad now and its group has one ready.</summary>
        public bool CanShow(string position) =>
            Allowed(position, false, out _) && (HDCAds.ForceAdGroup(HDCAds.CoreConfig.ForceAdGroupAt(position))?.IsReady ?? false);

        public bool IsGroupReady(string groupName) => HDCAds.ForceAdGroup(groupName)?.IsReady ?? false;

        /// <summary>Starts loading a group whose positions do not load it on their own (autoInit off).</summary>
        public void Initialize(string groupName)
        {
            if (IsDisabled || AutoInitGroups().Contains(groupName))
                return;
            HDCAds.ForceAdGroup(groupName)?.Initialize();
        }

        /// <summary>
        /// Drops a group's ads and show count and loads it again, for groups that load once
        /// (disablePostInitReload) or ran out of shows. False while the group shows an ad.
        /// </summary>
        public bool Reinitialize(string groupName) => !IsDisabled && HDCAds.ReinitializeForceAdGroup(groupName);

        /// <summary>The impressions shown at <paramref name="position"/>, across sessions.</summary>
        public int ImpressionCount(string position) => HDCAdsLog.GetInt("fa_count_" + position);

        public int TotalImpressionCount => HDCAdsLog.GetInt(TotalImpressionsKey);

        /// <summary>Starts the break ad timer, if the channel's break ad is enabled.</summary>
        public void StartBreakAd()
        {
            if (!BreakEnabled(out string reason))
            {
                HDCAdsLog.Info("break ad not started: " + reason);
                return;
            }

            HDCMainThread.EnsureCreated();
            if (!breakRunning)
                HDCMainThread.Ticked += TickBreak;
            breakRunning = true;
            ResetBreakCycle(true);
        }

        public void StopBreakAd()
        {
            if (breakRunning)
                HDCMainThread.Ticked -= TickBreak;
            breakRunning = false;
            ResetBreakCycle(true);
        }

        /// <summary>Starts the running break ad's timer over.</summary>
        public void ResetBreakAd()
        {
            if (breakRunning)
                ResetBreakCycle(true);
        }

        /// <summary>The configs, the capping of a position and the state of a group, for the debug panel.</summary>
        internal HDCDebugInfo Describe(string position, string groupName)
        {
            HDCAdsConfig.ForceAdChannel channel = Channel;
            HDCDebugInfo info = new HDCDebugInfo()
                .Section("Configs")
                .Needed("Enabled", channel.isEnabled)
                .Line("Launch Capping (s)", channel.launchCappingTime)
                .Line("Minimum Capping (s)", channel.minimumCappingTime)
                .Line("Capping Decrease Per Impression (s)", channel.cappingDecreasePerImpression)
                .Line("Positions", channel.positionConfigs?.Length ?? 0)
                .Section("Runtime")
                .Line("Ignore Ads", IgnoreAds)
                .Line("First Ad Of Session", firstAd)
                .Line("Total Impressions", TotalImpressionCount)
                .Line("Since Last Full-Screen Ad (s)", Time.realtimeSinceStartup - HDCAds.LastFullscreenAdTime)
                .Section("Gates")
                .Gate("Disabled", IsDisabled)
                .Gate("Ads Removed", HDCAds.IsAdsRemoved);

            HDCAdsConfig.ForceAdPosition config = PositionConfig(position);
            if (config != null)
            {
                bool allowed = Allowed(position, false, out string reason);
                info.Section("Position " + position)
                    .Line("Group", HDCAds.CoreConfig.ForceAdGroupAt(position))
                    .Line("Can Show (Config)", config.canShow)
                    .Line("Auto Init", config.autoInit)
                    .Line("Capping Now (s)", Capping(config))
                    .Line("Impressions Here", ImpressionCount(position))
                    .Add("Can Show Now", allowed ? "Yes" : "No · " + reason, allowed ? HDCDebugTone.Good : HDCDebugTone.Bad);
            }

            info.Section("Group " + (string.IsNullOrEmpty(groupName) ? "-" : groupName));
            HDCFullscreenGroup group = HDCAds.ExistingForceAdGroup(groupName);
            if (group != null)
                group.DescribeTo(info);
            else
                info.Add("State", "Not started: press Init", HDCDebugTone.Muted);
            return info;
        }

        internal void OnSdkInitialized()
        {
            if (IsDisabled)
                return;
            foreach (string groupName in AutoInitGroups())
                HDCAds.ForceAdGroup(groupName)?.Initialize();
        }

        internal void CountImpression(string position)
        {
            firstAd = false;
            HDCAdsLog.SetInt("fa_count_" + position, ImpressionCount(position) + 1);
            HDCAdsLog.SetInt(TotalImpressionsKey, TotalImpressionCount + 1);
        }

        private static HashSet<string> AutoInitGroups()
        {
            var groups = new HashSet<string>(StringComparer.Ordinal);
            foreach (HDCAdsConfig.ForceAdPosition position in Channel.positionConfigs ?? new HDCAdsConfig.ForceAdPosition[0])
            {
                if (position == null || !position.autoInit || !position.canShow)
                    continue;
                string groupName = HDCAds.CoreConfig.ForceAdGroupAt(position.positionName);
                if (!string.IsNullOrEmpty(groupName))
                    groups.Add(groupName);
            }

            return groups;
        }

        private bool Allowed(string position, bool ignoreCapping, out string reason)
        {
            if (IsDisabled)
            {
                reason = HDCAds.IsAdsRemoved ? "ads removed" : "channel disabled";
                return false;
            }

            HDCAdsConfig.ForceAdPosition config = PositionConfig(position);
            if (config == null || !config.canShow)
            {
                reason = "position disabled";
                return false;
            }

            if (!ignoreCapping)
            {
                float elapsed = Time.realtimeSinceStartup - HDCAds.LastFullscreenAdTime;
                float capping = Capping(config);
                if (elapsed < capping)
                {
                    reason = $"capping: {elapsed:0.#}s of {capping:0.#}s";
                    return false;
                }
            }

            if (IgnoreAds)
            {
                reason = "ignored";
                return false;
            }

            reason = "";
            return true;
        }

        private float Capping(HDCAdsConfig.ForceAdPosition position)
        {
            float decrease = position.cappingDecreasePerImpression != 0 ? position.cappingDecreasePerImpression : Channel.cappingDecreasePerImpression;
            float minimum = position.minimumCappingTime != 0 ? position.minimumCappingTime : Channel.minimumCappingTime;
            float capping = Mathf.Max(position.cappingTime - ImpressionCount(position.positionName) * decrease, minimum);
            return firstAd ? Mathf.Max(Channel.launchCappingTime, capping) : capping;
        }

        private static HDCAdsConfig.ForceAdPosition PositionConfig(string position) =>
            string.IsNullOrEmpty(position)
                ? null
                : Array.Find(Channel.positionConfigs ?? new HDCAdsConfig.ForceAdPosition[0], p => p != null && p.positionName == position);

        // Break ad

        private static string BreakPosition => Channel.breakAdConfig?.positionName ?? "";

        private bool BreakEnabled(out string reason)
        {
            HDCAdsConfig.BreakAd breakAd = Channel.breakAdConfig;
            if (!Channel.isEnabled || breakAd == null || !breakAd.isEnabled)
                reason = "disabled in the config";
            else if (HDCAds.IsAdsRemoved)
                reason = "ads removed";
            else if (PositionConfig(breakAd.positionName) == null)
                reason = $"position '{breakAd.positionName}' not in positionConfigs";
            else
                reason = "";
            return reason.Length == 0;
        }

        private void TickBreak()
        {
            if (!breakRunning || breakAttempting)
                return;

            string position = BreakPosition;
            HDCAdsConfig.ForceAdPosition config = PositionConfig(position);
            if (config == null || !config.canShow)
                return;

            float target = Capping(config);
            int lead = Mathf.Max(0, Channel.breakAdConfig?.notificationLeadTimeSeconds ?? 0);
            breakElapsed += Time.deltaTime;
            if (!breakNoticeSent && lead > 0 && target - breakElapsed <= lead)
            {
                breakNoticeSent = true;
                Raise(BreakAdNotice, position, Mathf.Max(0, Mathf.CeilToInt(target - breakElapsed)));
            }

            if (breakElapsed >= target)
                TryShowBreak(position);
        }

        private void TryShowBreak(string position)
        {
            if (!BreakEnabled(out string reason) || !Allowed(position, true, out reason))
            {
                BreakFailed(position, reason);
                return;
            }

            HDCFullscreenGroup group = HDCAds.ForceAdGroup(HDCAds.CoreConfig.ForceAdGroupAt(position));
            if (group == null || !group.IsReady)
            {
                BreakFailed(position, group == null ? "no group for the position" : "not ready");
                return;
            }

            breakAttempting = true;
            bool displayed = false;
            bool shown = group.Show(
                HDCAdChannel.ForceAd,
                position,
                () => Raise(BreakAdShown, position),
                () =>
                {
                    displayed = true;
                    CountImpression(position);
                },
                _ =>
                {
                    if (!breakAttempting)
                        return;
                    breakAttempting = false;
                    ResetBreakCycle(true);
                    if (displayed)
                        Raise(BreakAdClosed, position);
                    else
                        Raise(BreakAdShowFailed, position, "show failed");
                });
            if (!shown)
            {
                breakAttempting = false;
                BreakFailed(position, "show returned false");
            }
        }

        private void BreakFailed(string position, string reason)
        {
            HDCAdsLog.Info($"break ad at {position} failed: {reason}");
            Raise(BreakAdShowFailed, position, reason);
            ResetBreakCycle(true);
        }

        private void ResetBreakCycle(bool clearAttempt)
        {
            breakResetFrame = Time.frameCount;
            breakElapsed = 0f;
            breakNoticeSent = false;
            if (clearAttempt)
                breakAttempting = false;
        }

        private static void Run(Action action) => HDCAdsLog.Run(action);

        private static void Raise(Action<string> handler, string position) => HDCAdsLog.Run(() => handler?.Invoke(position));

        private static void Raise<T>(Action<string, T> handler, string position, T value) =>
            HDCAdsLog.Run(() => handler?.Invoke(position, value));
    }
}
