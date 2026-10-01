using System.Collections.Generic;
using UnityEngine;

namespace HDC.Ads
{
    /// <summary>The MREC (300x250) view, from the Google Mobile Ads plugin.</summary>
    public sealed class HDCMrecAds
    {
        private const string InstanceId = "mrec_plugin";

        private HDCRectGroup group;

        internal HDCMrecAds()
        {
        }

        private static HDCAdsConfig.MrecChannel Channel => HDCAds.Config.mrecChannel ?? new HDCAdsConfig.MrecChannel();

        private static bool IsEnabled => Channel.isEnabled && !HDCAds.IsAdsRemoved;

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
                .Gate("Ads Removed", HDCAds.IsAdsRemoved)
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
            HDCAdCoreConfig.FullscreenUnit unit = HDCAds.CoreConfig.mrecUnit ?? new HDCAdCoreConfig.FullscreenUnit();
            var sources = new List<HDCRectSource>();
            if ((unit.mediationPriority == HDCAds.PluginUnit || unit.useBackup) && !string.IsNullOrEmpty(unit.admobUnit?.id))
                sources.Add(new HDCPluginRectSource(InstanceId, unit.admobUnit.id, HDCBannerViewPlacement.Mrec));
            group = new HDCRectGroup(sources, false);
            return group;
        }
    }
}
