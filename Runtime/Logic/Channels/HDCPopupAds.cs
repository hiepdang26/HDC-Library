using System;
using System.Collections.Generic;
using HDC.Ads.Domain;
using HDC.Ads.Ports;
using UnityEngine;

namespace HDC.Ads.Logic
{
    /// <summary>The popup channel behind <see cref="IPopupAds"/>.</summary>
    internal sealed partial class HDCPopupAds : IPopupAds, IAdChannel
    {
        private readonly HDCAdsContext context;
        private readonly Dictionary<string, Popup> popups = new Dictionary<string, Popup>(StringComparer.Ordinal);

        internal HDCPopupAds(HDCAdsContext context)
        {
            this.context = context;
        }

        private HDCAdsConfig.PopupChannel Channel => context.Config.popupChannel ?? new HDCAdsConfig.PopupChannel();

        private bool IsDisabled => !Channel.isEnabled || context.IsAdsRemoved;

        /// <summary>Shows the popup of <paramref name="position"/>. False when it cannot show now.</summary>
        public bool Show(string position)
        {
            if (!Allowed(position, out string reason))
            {
                context.Log.Info($"popup {position} blocked: {reason}");
                return false;
            }

            Popup popup = PopupAt(position);
            if (popup == null)
                return false;
            if (!popup.Ad.IsPlaced)
            {
                context.Log.Info($"popup {position} blocked: call Move first");
                return false;
            }

            popup.Ad.Load();
            context.Placements.Record(popup.Ad.Id, HDCAdChannel.Popup, position, popup.Network.RevenueNetwork);
            return popup.Ad.Show();
        }

        public void Hide(string position) => PopupAt(position)?.Ad.Hide();

        /// <summary>Places the popup of <paramref name="position"/> over a rectangle in Unity screen pixels.</summary>
        public void Move(string position, Rect screenRect)
        {
            Popup popup = Allowed(position, out _) ? PopupAt(position) : null;
            popup?.Ad.Place(screenRect);
        }

        /// <summary>Places the popup of <paramref name="position"/> over a UI element.</summary>
        public void Move(string position, RectTransform area, Camera camera = null)
        {
            if (area == null)
                return;
            var corners = new Vector3[4];
            area.GetWorldCorners(corners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            Move(position, Rect.MinMaxRect(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y), Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y)));
        }

        public bool CanShow(string position)
        {
            Popup popup = Allowed(position, out _) ? PopupAt(position) : null;
            return popup != null && popup.Ad.IsPlaced && popup.Ad.IsDisplayable;
        }

        public bool IsGroupReady(string groupName) => PopupNamed(groupName)?.Ad.IsReady ?? false;

        /// <summary>Drops a group's popup and loads a new one. False while it shows.</summary>
        public bool Reinitialize(string groupName) => !IsDisabled && (PopupNamed(groupName)?.Ad.Reload() ?? false);

        /// <summary>Starts loading a group whose positions do not load it on their own (autoInit off).</summary>
        public void Initialize(string groupName)
        {
            if (!IsDisabled && !AutoInitGroups().Contains(groupName))
                PopupNamed(groupName)?.Ad.Load();
        }

        string IAdChannel.Key => "PU";

        string IAdChannel.Title => "Popup";

        void IAdChannel.OnSdkInitialized()
        {
            if (IsDisabled)
                return;
            foreach (string groupName in AutoInitGroups())
                PopupNamed(groupName)?.Ad.Load();
        }

        void IAdChannel.InitializeAll()
        {
            foreach (HDCAdCoreConfig.PopupGroup group in context.CoreConfig.popupGroups ?? new HDCAdCoreConfig.PopupGroup[0])
            {
                if (group != null)
                    Initialize(group.groupName);
            }
        }

        void IAdChannel.OnAdsRemoved()
        {
            foreach (Popup popup in popups.Values)
                popup.Ad.Hide();
        }

        private bool Allowed(string position, out string reason)
        {
            HDCAdsConfig.PopupPosition config = string.IsNullOrEmpty(position)
                ? null
                : Array.Find(Channel.positionConfigs ?? new HDCAdsConfig.PopupPosition[0], p => p != null && p.positionName == position);
            if (IsDisabled)
                reason = context.IsAdsRemoved ? "ads removed" : "channel disabled";
            else if (config == null || !config.isEnabled)
                reason = "position disabled";
            else
                reason = "";
            return reason.Length == 0;
        }

        private HashSet<string> AutoInitGroups()
        {
            var groups = new HashSet<string>(StringComparer.Ordinal);
            foreach (HDCAdsConfig.PopupPosition position in Channel.positionConfigs ?? new HDCAdsConfig.PopupPosition[0])
            {
                if (position == null || !position.autoInit || !position.isEnabled)
                    continue;
                string groupName = context.CoreConfig.PopupGroupAt(position.positionName);
                if (!string.IsNullOrEmpty(groupName))
                    groups.Add(groupName);
            }

            return groups;
        }

        private Popup PopupAt(string position) => PopupNamed(context.CoreConfig.PopupGroupAt(position));

        private Popup PopupNamed(string groupName)
        {
            if (string.IsNullOrEmpty(groupName))
                return null;
            if (popups.TryGetValue(groupName, out Popup popup))
                return popup;

            HDCAdPlan plan = context.Groups.PopupPlan(groupName);
            if (plan == null)
                return null;
            popup = new Popup(plan.Network.CreatePopup(plan), plan.Network);
            popups[groupName] = popup;
            return popup;
        }

        // A group's popup, with the network it came from for its revenue.
        private sealed class Popup
        {
            internal Popup(IPopupAd ad, IAdNetwork network)
            {
                Ad = ad;
                Network = network;
            }

            internal IPopupAd Ad { get; }
            internal IAdNetwork Network { get; }
        }
    }
}
