using System;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using UnityEngine;

namespace HDC.Ads
{
    /// <summary>The popup channel behind <see cref="IPopupAds"/>.</summary>
    internal sealed class HDCPopupAds : IPopupAds
    {
        private readonly Dictionary<string, Popup> popups = new Dictionary<string, Popup>(StringComparer.Ordinal);

        internal HDCPopupAds()
        {
        }

        private static HDCAdsConfig.PopupChannel Channel => HDCAds.Config.popupChannel ?? new HDCAdsConfig.PopupChannel();

        private static bool IsDisabled => !Channel.isEnabled || HDCAds.IsAdsRemoved;

        /// <summary>Shows the popup of <paramref name="position"/>. False when it cannot show now.</summary>
        public bool Show(string position)
        {
            if (!Allowed(position, out string reason))
            {
                HDCAdsLog.Info($"popup {position} blocked: {reason}");
                return false;
            }

            Popup popup = PopupAt(position);
            if (popup == null)
                return false;
            if (!popup.Placed)
            {
                HDCAdsLog.Info($"popup {position} blocked: call Move first");
                return false;
            }

            popup.Load();
            HDCAdPlacements.Record(popup.Id, HDCAdChannel.Popup, position);
            return HDCAdsSdk.ShowPopup(popup.Id);
        }

        public void Hide(string position)
        {
            Popup popup = PopupAt(position);
            if (popup != null && popup.Requested)
                HDCAdsSdk.HidePopup(popup.Id);
        }

        /// <summary>Places the popup of <paramref name="position"/> over a rectangle in Unity screen pixels.</summary>
        public void Move(string position, Rect screenRect)
        {
            Popup popup = Allowed(position, out _) ? PopupAt(position) : null;
            popup?.Place(screenRect);
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
            return popup != null && popup.Placed && popup.Requested && HDCAdsSdk.IsPopupDisplayable(popup.Id);
        }

        public bool IsGroupReady(string groupName)
        {
            Popup popup = PopupNamed(groupName);
            return popup != null && popup.Requested && HDCAdsSdk.IsPopupReady(popup.Id);
        }

        /// <summary>Drops a group's popup and loads a new one. False while it shows.</summary>
        public bool Reinitialize(string groupName) => !IsDisabled && (PopupNamed(groupName)?.Reload() ?? false);

        /// <summary>Starts loading a group whose positions do not load it on their own (autoInit off).</summary>
        public void Initialize(string groupName)
        {
            if (!IsDisabled && !AutoInitGroups().Contains(groupName))
                PopupNamed(groupName)?.Load();
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
                .Gate("Ads Removed", HDCAds.IsAdsRemoved);

            if (!string.IsNullOrEmpty(position))
            {
                bool allowed = Allowed(position, out string reason);
                info.Section("Position " + position)
                    .Line("Group", HDCAds.CoreConfig.PopupGroupAt(position))
                    .Add("Can Show Here", allowed ? "Yes" : "No · " + reason, allowed ? HDCDebugTone.Good : HDCDebugTone.Bad);
            }

            info.Section("Group " + (string.IsNullOrEmpty(groupName) ? "-" : groupName));
            if (string.IsNullOrEmpty(groupName) || !popups.TryGetValue(groupName, out Popup popup))
                return info.Add("State", "Not started", HDCDebugTone.Muted);

            return info
                .Line("Instance ID", popup.Id)
                .Line("Requested", popup.Requested)
                .Needed("Placed (Move)", popup.Placed)
                .Line("Native State", popup.Requested ? HDCAdsSdk.GetPopupState(popup.Id) : "-");
        }

        /// <summary>A popup group's instance once the channel made it, for the debug panel.</summary>
        internal bool TryGetPopup(string groupName, out string id, out bool requested, out bool placed)
        {
            id = null;
            requested = placed = false;
            if (string.IsNullOrEmpty(groupName) || !popups.TryGetValue(groupName, out Popup popup))
                return false;
            id = popup.Id;
            requested = popup.Requested;
            placed = popup.Placed;
            return true;
        }

        /// <summary>The instance id a popup group's ads load with.</summary>
        internal static string PopupId(string groupName) => "pu_" + groupName;

        internal void OnSdkInitialized()
        {
            if (IsDisabled)
                return;
            foreach (string groupName in AutoInitGroups())
                PopupNamed(groupName)?.Load();
        }

        internal void HideAll()
        {
            foreach (Popup popup in popups.Values)
            {
                if (popup.Requested)
                    HDCAdsSdk.HidePopup(popup.Id);
            }
        }

        private static bool Allowed(string position, out string reason)
        {
            HDCAdsConfig.PopupPosition config = string.IsNullOrEmpty(position)
                ? null
                : Array.Find(Channel.positionConfigs ?? new HDCAdsConfig.PopupPosition[0], p => p != null && p.positionName == position);
            if (IsDisabled)
                reason = HDCAds.IsAdsRemoved ? "ads removed" : "channel disabled";
            else if (config == null || !config.isEnabled)
                reason = "position disabled";
            else
                reason = "";
            return reason.Length == 0;
        }

        private static HashSet<string> AutoInitGroups()
        {
            var groups = new HashSet<string>(StringComparer.Ordinal);
            foreach (HDCAdsConfig.PopupPosition position in Channel.positionConfigs ?? new HDCAdsConfig.PopupPosition[0])
            {
                if (position == null || !position.autoInit || !position.isEnabled)
                    continue;
                string groupName = HDCAds.CoreConfig.PopupGroupAt(position.positionName);
                if (!string.IsNullOrEmpty(groupName))
                    groups.Add(groupName);
            }

            return groups;
        }

        private Popup PopupAt(string position) => PopupNamed(HDCAds.CoreConfig.PopupGroupAt(position));

        private Popup PopupNamed(string groupName)
        {
            if (string.IsNullOrEmpty(groupName))
                return null;
            if (popups.TryGetValue(groupName, out Popup popup))
                return popup;

            HDCAdCoreConfig.PopupGroup config = HDCAds.CoreConfig.PopupGroupNamed(groupName);
            if (config == null || string.IsNullOrEmpty(config.androidUnit?.id))
                return null;
            popup = new Popup(PopupId(groupName), config);
            popups[groupName] = popup;
            return popup;
        }

        private sealed class Popup
        {
            private readonly HDCAdCoreConfig.PopupGroup config;
            private readonly HDCPopupOptions options;

            internal Popup(string id, HDCAdCoreConfig.PopupGroup config)
            {
                Id = id;
                this.config = config;
                options = new HDCPopupOptions
                {
                    timeShow = config.androidUnit.timeShow,
                    timeReload = config.disablePostInitReload ? 0 : config.androidUnit.reloadTime,
                };
                if (!string.IsNullOrEmpty(config.androidUnit.layout))
                    options.layout = config.androidUnit.layout;
            }

            internal string Id { get; }
            internal bool Requested { get; private set; }
            internal bool Placed { get; private set; }

            internal void Load()
            {
                if (Requested)
                    return;
                Requested = HDCAdsSdk.LoadPopup(Id, new[] { config.androidUnit.id }, options);
            }

            internal bool Reload()
            {
                if (Requested && HDCAdsSdk.GetPopupState(Id) == "Showing")
                    return false;
                if (Requested)
                    HDCAdsSdk.DestroyPopup(Id);
                Requested = false;
                Load();
                return Requested;
            }

            // The native side takes the popup's center relative to the screen (y from the bottom) and its size in dp.
            internal void Place(Rect screenRect)
            {
                if (Screen.width <= 0 || Screen.height <= 0 || screenRect.width <= 0f || screenRect.height <= 0f)
                    return;

                float scale = MobileAds.Utils.GetDeviceScale();
                if (scale <= 0f)
                    scale = 1f;
                options.x = Mathf.Clamp01(screenRect.center.x / Screen.width);
                options.y = Mathf.Clamp01(screenRect.center.y / Screen.height);
                options.width = screenRect.width / scale;
                options.height = screenRect.height / scale;
                Placed = true;
                if (Requested)
                    HDCAdsSdk.UpdatePopupPlacement(Id, options.x, options.y, options.width, options.height);
            }
        }
    }
}
