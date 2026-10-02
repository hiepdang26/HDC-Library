using System.Collections.Generic;
using HDC.Ads.Diagnostics;
using HDC.Ads.Domain;
using HDC.Ads.Infrastructure;
using UnityEngine;

namespace HDC.Ads.Logic
{
    /// <summary>The MREC channel behind <see cref="IMrecAds"/>.</summary>
    internal sealed class HDCMrecAds : IMrecAds
    {
        private const string InstanceId = "mrec_plugin";

        private readonly HDCAdsContext context;
        private HDCRectGroup group;

        internal HDCMrecAds(HDCAdsContext context)
        {
            this.context = context;
        }

        private HDCAdsConfig.MrecChannel Channel => context.Config.mrecChannel ?? new HDCAdsConfig.MrecChannel();

        private bool IsEnabled => Channel.isEnabled && !context.IsAdsRemoved;

        public bool CanShow => IsEnabled && Group().IsLoaded;

        /// <summary>The view's size in screen pixels, zero until it exists.</summary>
        public Vector2 SizeInPixels => HDCAdsSdk.GetBannerViewSizeInPixels(InstanceId);

        /// <summary>Shows the MREC now, or as soon as it loads. False when the channel is off.</summary>
        public bool Show()
        {
            if (!IsEnabled || Group().IsEmpty)
                return false;
            Group().Show();
            return true;
        }

        public void Hide() => group?.Hide();

        public void Move(HDCAdPosition position)
        {
            if (IsEnabled && !Group().IsEmpty)
                HDCAdsSdk.MoveBannerView(InstanceId, position);
        }

        /// <summary>Centers the MREC on a point in Unity screen pixels.</summary>
        public void Move(Vector2 screenPoint)
        {
            if (IsEnabled && !Group().IsEmpty)
                HDCAdsSdk.MoveBannerView(InstanceId, screenPoint);
        }

        /// <summary>Centers the MREC on a scene or UI object.</summary>
        public void Move(GameObject target, Camera camera = null)
        {
            if (target == null)
                return;
            Vector3 position = target.transform.position;
            Move(camera != null ? (Vector2)camera.WorldToScreenPoint(position) : RectTransformUtility.WorldToScreenPoint(null, position));
        }

        /// <summary>Starts loading when the channel does not load on its own (autoInit off).</summary>
        public void Initialize()
        {
            if (IsEnabled && !Channel.autoInit)
                Group().Initialize();
        }

        /// <summary>Configs, size and state, for the debug panel. It reads the group without making it.</summary>
        internal HDCDebugInfo Describe()
        {
            Vector2 size = SizeInPixels;
            HDCDebugInfo info = new HDCDebugInfo()
                .Section("Configs")
                .Needed("Enabled", Channel.isEnabled)
                .Line("Auto Init", Channel.autoInit)
                .Section("Runtime")
                .Line("Can Show", IsEnabled && group != null && group.IsLoaded)
                .Line("Size In Pixels", size == Vector2.zero ? "-" : $"{size.x:0} x {size.y:0}")
                .Section("Gates")
                .Gate("Disabled", !IsEnabled)
                .Gate("Ads Removed", context.IsAdsRemoved)
                .Section("Group");
            if (group != null)
                group.DescribeTo(info);
            else
                info.Add("State", "Not started", HDCDebugTone.Muted);
            return info;
        }

        /// <summary>The MREC group once the channel made it, for the debug panel.</summary>
        internal HDCRectGroup ExistingGroup => group;

        internal void OnSdkInitialized()
        {
            if (IsEnabled && Channel.autoInit)
                Group().Initialize();
        }

        private HDCRectGroup Group()
        {
            if (group != null)
                return group;

            // Priority 0 is the plugin; 1 is a network no longer served, used only as a backup order.
            HDCAdCoreConfig.FullscreenUnit unit = context.CoreConfig.mrecUnit ?? new HDCAdCoreConfig.FullscreenUnit();
            var sources = new List<HDCRectSource>();
            if ((unit.mediationPriority == HDCAdGroups.PluginUnit || unit.useBackup) && !string.IsNullOrEmpty(unit.admobUnit?.id))
                sources.Add(new HDCPluginRectSource(InstanceId, unit.admobUnit.id, HDCBannerViewPlacement.Mrec));
            context.Placements.Record(InstanceId, HDCAdChannel.Mrec);
            group = new HDCRectGroup(sources, false);
            return group;
        }
    }
}
