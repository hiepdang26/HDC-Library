using System;
using System.Collections.Generic;
using HDC.Ads.Domain;
using UnityEngine;

namespace HDC.Ads.Logic
{
    internal sealed partial class HDCForceAds : IForceAds, IAdChannel
    {
        private readonly HDCAdsContext context;
        private bool firstAd = true;

        internal HDCForceAds(HDCAdsContext context)
        {
            this.context = context;
        }

        public bool IgnoreAds { get; set; }

        private HDCAdsConfig.ForceAdChannel Channel => context.Config.forceAdChannel ?? new HDCAdsConfig.ForceAdChannel();

        private bool IsDisabled => !Channel.isEnabled || context.IsAdsRemoved;

        public bool Show(string position, Action onDone = null)
        {
            if (!Allowed(position, out string reason))
            {
                context.Log.Info($"force ad {position} blocked: {reason}");
                Run(onDone);
                return false;
            }

            HDCFullscreenGroup group = context.Groups.ForceAdGroup(context.CoreConfig.ForceAdGroupAt(position));
            bool shown = group != null && group.Show(HDCAdChannel.ForceAd, position, null, () => CountImpression(position), _ => Run(onDone));
            if (!shown)
                Run(onDone);
            return shown;
        }

        public bool CanShow(string position) =>
            Allowed(position, out _) && (context.Groups.ForceAdGroup(context.CoreConfig.ForceAdGroupAt(position))?.IsReady ?? false);

        public bool IsGroupReady(string groupName) => context.Groups.ForceAdGroup(groupName)?.IsReady ?? false;

        public void Initialize(string groupName)
        {
            if (IsDisabled || AutoInitGroups().Contains(groupName))
                return;
            context.Groups.ForceAdGroup(groupName)?.Initialize();
        }

        public bool Reinitialize(string groupName) => !IsDisabled && context.Groups.ReinitializeForceAdGroup(groupName);

        public int ImpressionCount(string position) => context.Count(HDCAdNames.ForceAdCountKey(position));

        public int TotalImpressionCount => context.Count(HDCAdNames.ForceAdTotalKey);

        string IAdChannel.Key => "FA";

        string IAdChannel.Title => "ForceAd";

        void IAdChannel.OnSdkInitialized()
        {
            if (IsDisabled)
                return;
            foreach (string groupName in AutoInitGroups())
                context.Groups.ForceAdGroup(groupName)?.Initialize();
        }

        void IAdChannel.InitializeAll()
        {
            foreach (HDCAdCoreConfig.ForceAdGroup group in context.CoreConfig.forceAdGroups ?? new HDCAdCoreConfig.ForceAdGroup[0])
            {
                if (group != null)
                    Initialize(group.groupName);
            }
        }

        void IAdChannel.OnAdsRemoved()
        {
        }

        internal void CountImpression(string position)
        {
            firstAd = false;
            context.Store.SetInt(HDCAdNames.ForceAdCountKey(position), ImpressionCount(position) + 1);
            context.Store.SetInt(HDCAdNames.ForceAdTotalKey, TotalImpressionCount + 1);
        }

        private HashSet<string> AutoInitGroups()
        {
            var groups = new HashSet<string>(StringComparer.Ordinal);
            foreach (HDCAdsConfig.ForceAdPosition position in Channel.positionConfigs ?? new HDCAdsConfig.ForceAdPosition[0])
            {
                if (position == null || !position.autoInit || !position.canShow)
                    continue;
                string groupName = context.CoreConfig.ForceAdGroupAt(position.positionName);
                if (!string.IsNullOrEmpty(groupName))
                    groups.Add(groupName);
            }

            return groups;
        }

        private bool Allowed(string position, out string reason)
        {
            if (IsDisabled)
            {
                reason = context.IsAdsRemoved ? "ads removed" : "channel disabled";
                return false;
            }

            HDCAdsConfig.ForceAdPosition config = PositionConfig(position);
            if (config == null || !config.canShow)
            {
                reason = "position disabled";
                return false;
            }

            float elapsed = context.Clock.RealTime - context.LastFullscreenAdTime;
            float capping = Capping(config);
            if (elapsed < capping)
            {
                reason = $"capping: {elapsed:0.#}s of {capping:0.#}s";
                return false;
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

        private HDCAdsConfig.ForceAdPosition PositionConfig(string position) =>
            string.IsNullOrEmpty(position)
                ? null
                : Array.Find(Channel.positionConfigs ?? new HDCAdsConfig.ForceAdPosition[0], p => p != null && p.positionName == position);

        private void Run(Action action) => HDCCallbacks.Run(action, context.Log);
    }
}
