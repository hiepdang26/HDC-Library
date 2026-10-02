using System;
using System.Collections.Generic;
using HDC.Ads.Diagnostics;
using HDC.Ads.Domain;
using HDC.Ads.Ports;
using UnityEngine;

namespace HDC.Ads.Logic
{
    /// <summary>The popup channel behind <see cref="IPopupAds"/>.</summary>
    internal sealed class HDCPopupAds : IPopupAds
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

        /// <summary>The configs, a group's state and a position's gate, for the debug panel.</summary>
        internal HDCDebugInfo Describe(string groupName, string position)
        {
            HDCDebugInfo info = new HDCDebugInfo()
                .Section("Configs")
                .Needed("Enabled", Channel.isEnabled)
                .Line("Positions", Channel.positionConfigs?.Length ?? 0)
                .Line("Auto Init Groups", string.Join(", ", AutoInitGroups()))
                .Section("Gates")
                .Gate("Disabled", IsDisabled)
                .Gate("Ads Removed", context.IsAdsRemoved);

            if (!string.IsNullOrEmpty(position))
            {
                bool allowed = Allowed(position, out string reason);
                info.Section("Position " + position)
                    .Line("Group", context.CoreConfig.PopupGroupAt(position))
                    .Add("Can Show Here", allowed ? "Yes" : "No · " + reason, allowed ? HDCDebugTone.Good : HDCDebugTone.Bad);
            }

            info.Section("Group " + (string.IsNullOrEmpty(groupName) ? "-" : groupName));
            if (string.IsNullOrEmpty(groupName) || !popups.TryGetValue(groupName, out Popup popup))
                return info.Add("State", "Not started", HDCDebugTone.Muted);

            return info
                .Line("Instance ID", popup.Ad.Id)
                .Line("Requested", popup.Ad.IsRequested)
                .Needed("Placed (Move)", popup.Ad.IsPlaced)
                .Line("Native State", popup.Ad.State ?? "-");
        }

        /// <summary>A popup group's instance once the channel made it, for the debug panel.</summary>
        internal bool TryGetPopup(string groupName, out string id, out bool requested, out bool placed)
        {
            id = null;
            requested = placed = false;
            if (string.IsNullOrEmpty(groupName) || !popups.TryGetValue(groupName, out Popup popup))
                return false;
            id = popup.Ad.Id;
            requested = popup.Ad.IsRequested;
            placed = popup.Ad.IsPlaced;
            return true;
        }

        internal void OnSdkInitialized()
        {
            if (IsDisabled)
                return;
            foreach (string groupName in AutoInitGroups())
                PopupNamed(groupName)?.Ad.Load();
        }

        internal void HideAll()
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
