using System.Collections.Generic;
using HDC.Ads.Domain;
using UnityEngine;

namespace HDC.Ads.Logic
{
    /// <summary>The MREC channel behind <see cref="IMrecAds"/>.</summary>
    internal sealed partial class HDCMrecAds : IMrecAds, IAdChannel
    {
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
        public Vector2 SizeInPixels => group != null && group.Sources.Count > 0 ? group.Sources[0].SizeInPixels : Vector2.zero;

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
            if (!IsEnabled)
                return;
            foreach (HDCRectSource source in Group().Sources)
                source.Move(position);
        }

        /// <summary>Centers the MREC on a point in Unity screen pixels.</summary>
        public void Move(Vector2 screenPoint)
        {
            if (!IsEnabled)
                return;
            foreach (HDCRectSource source in Group().Sources)
                source.Move(screenPoint);
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

        string IAdChannel.Key => "MREC";

        string IAdChannel.Title => "Mrec";

        void IAdChannel.OnSdkInitialized()
        {
            if (IsEnabled && Channel.autoInit)
                Group().Initialize();
        }

        void IAdChannel.InitializeAll() => Initialize();

        void IAdChannel.OnAdsRemoved() => Hide();

        private HDCRectGroup Group()
        {
            if (group != null)
                return group;

            List<HDCRectSource> sources = HDCAdGroups.ViewSources(context.Groups.MrecPlans());
            foreach (HDCRectSource source in sources)
                context.Placements.Record(source.Id, HDCAdChannel.Mrec, "", source.Network.RevenueNetwork);
            group = new HDCRectGroup(sources, false);
            return group;
        }
    }
}
