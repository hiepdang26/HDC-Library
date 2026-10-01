using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HDC.Ads.DebugUI
{
    /// <summary>How an ad unit is doing, by the color the panel gives it.</summary>
    internal enum HDCUnitTone
    {
        Idle,
        Busy,
        Good,
        Live,
        Bad,
    }

    /// <summary>One ad unit of a group: what the configs say, and what it did so far.</summary>
    internal sealed class HDCDebugUnit
    {
        internal int Index;
        internal string Format;
        internal string Id;
        internal string AdUnitId;

        /// <summary>The channel made the unit's group.</summary>
        internal bool Created;

        /// <summary>The group started the unit: backups start only after the units before them failed.</summary>
        internal bool Started;

        internal bool Ready;
        internal bool OnScreen;
        internal HDCAdRecord Record;

        /// <summary>A state only the native side knows, such as a popup's.</summary>
        internal string NativeState;

        internal string Status(out HDCUnitTone tone)
        {
            HDCAdState state = Record?.State ?? HDCAdState.Idle;
            if (!Created)
            {
                tone = HDCUnitTone.Idle;
                return "NOT INITIALIZED";
            }

            if (!Started && state == HDCAdState.Idle)
            {
                tone = HDCUnitTone.Idle;
                return "BACKUP · NOT STARTED";
            }

            if (OnScreen || state == HDCAdState.Showing)
            {
                tone = HDCUnitTone.Live;
                return "SHOWING";
            }

            if (Ready)
            {
                tone = HDCUnitTone.Good;
                return "READY";
            }

            switch (state)
            {
                case HDCAdState.Loading:
                    tone = HDCUnitTone.Busy;
                    return "LOADING";
                case HDCAdState.Loaded:
                    tone = HDCUnitTone.Good;
                    return "LOADED";
                case HDCAdState.LoadFailed:
                    tone = HDCUnitTone.Bad;
                    float retryIn = Record.RetryAt - Time.realtimeSinceStartup;
                    return retryIn > 0f ? $"LOAD FAILED · RETRY IN {Mathf.CeilToInt(retryIn)}s" : "LOAD FAILED";
                case HDCAdState.ShowFailed:
                    tone = HDCUnitTone.Bad;
                    return "SHOW FAILED";
                case HDCAdState.Closed:
                    tone = HDCUnitTone.Busy;
                    return "CLOSED";
                case HDCAdState.Destroyed:
                    tone = HDCUnitTone.Idle;
                    return "DESTROYED";
                default:
                    tone = HDCUnitTone.Idle;
                    return "IDLE";
            }
        }
    }

    /// <summary>A group, slot or channel: its settings and state, and its ad units in order.</summary>
    internal sealed class HDCDebugGroup
    {
        /// <summary>What the panel calls it: Group, Placement or Channel.</summary>
        internal string Kind = "Group";

        internal string Name;
        internal bool Selected;
        internal readonly HDCDebugInfo Details = new HDCDebugInfo();
        internal readonly List<HDCDebugUnit> Units = new List<HDCDebugUnit>();
    }

    /// <summary>The groups and ad units of each channel, from the configs and from what the channels made so far.</summary>
    internal static class HDCAdsDebugModel
    {
        internal static readonly string[] BannerSlots = Enum.GetNames(typeof(HDCBannerSlot));

        /// <summary>
        /// The channel's groups. With <paramref name="askNative"/> the selected popup group also reads its state from
        /// the native side; without, popups go by their events only, which is enough for the tab colors.
        /// </summary>
        internal static List<HDCDebugGroup> Groups(string channel, string selectedGroup, string selectedPosition, bool askNative = true)
        {
            var groups = new List<HDCDebugGroup>();
            if (!HDCAds.IsInitialized)
                return groups;

            switch (channel)
            {
                case "FA":
                    foreach (HDCAdCoreConfig.ForceAdGroup config in HDCAds.CoreConfig.forceAdGroups ?? new HDCAdCoreConfig.ForceAdGroup[0])
                    {
                        if (config != null && !string.IsNullOrEmpty(config.groupName))
                            groups.Add(ForceAdGroup(config, config.groupName == selectedGroup));
                    }

                    break;
                case "RW":
                    HDCAdCoreConfig.FullscreenUnit rewarded = HDCAds.CoreConfig.rewardedUnit ?? new HDCAdCoreConfig.FullscreenUnit();
                    groups.Add(FullscreenGroup("rewarded", HDCAds.ExistingRewardedGroup, true, RewardedUnits(), details => details
                        .Line("Priority", Priority(rewarded.mediationPriority))
                        .Line("Backup", rewarded.useBackup)));
                    groups[groups.Count - 1].Kind = "Channel";
                    break;
                case "AL":
                    groups.Add(LaunchGroup());
                    break;
                case "AR":
                    groups.Add(ResumeGroup());
                    break;
                case "BN":
                    foreach (string slotName in BannerSlots)
                        groups.Add(BannerGroup((HDCBannerSlot)Enum.Parse(typeof(HDCBannerSlot), slotName), slotName == selectedPosition));
                    break;
                case "MREC":
                    groups.Add(RectGroup("mrec", HDCAds.Mrec.ExistingGroup, true, new[]
                    {
                        Planned(HDCAdFormat.Mrec, "mrec_plugin", HDCAds.CoreConfig.mrecUnit?.admobUnit?.id),
                    }, details => details.Line("Priority", Priority(HDCAds.CoreConfig.mrecUnit?.mediationPriority ?? 0))));
                    groups[groups.Count - 1].Kind = "Channel";
                    break;
                case "PU":
                    foreach (HDCAdCoreConfig.PopupGroup config in HDCAds.CoreConfig.popupGroups ?? new HDCAdCoreConfig.PopupGroup[0])
                    {
                        if (config != null && !string.IsNullOrEmpty(config.groupName))
                            groups.Add(PopupGroup(config, config.groupName == selectedGroup, askNative && config.groupName == selectedGroup));
                    }

                    break;
            }

            return groups;
        }

        /// <summary>The selected group of a channel: its group, its banner slot, or its only group.</summary>
        internal static HDCDebugGroup Selected(List<HDCDebugGroup> groups) =>
            groups.FirstOrDefault(group => group.Selected) ?? (groups.Count == 1 ? groups[0] : null);

        /// <summary>The instance ids of a channel's ad units, to pick its events.</summary>
        internal static HashSet<string> InstanceIds(IEnumerable<HDCDebugGroup> groups) =>
            new HashSet<string>(groups.SelectMany(group => group.Units).Select(unit => unit.Id).Where(id => !string.IsNullOrEmpty(id)));

        /// <summary>The channel's overall tone: live or ready if any unit is, then loading, then failing.</summary>
        internal static HDCUnitTone Tone(IEnumerable<HDCDebugGroup> groups)
        {
            var tones = new List<HDCUnitTone>();
            foreach (HDCDebugUnit unit in groups.SelectMany(group => group.Units))
            {
                unit.Status(out HDCUnitTone tone);
                tones.Add(tone);
            }

            if (tones.Contains(HDCUnitTone.Live))
                return HDCUnitTone.Live;
            if (tones.Contains(HDCUnitTone.Good))
                return HDCUnitTone.Good;
            if (tones.Contains(HDCUnitTone.Busy))
                return HDCUnitTone.Busy;
            return tones.Contains(HDCUnitTone.Bad) ? HDCUnitTone.Bad : HDCUnitTone.Idle;
        }

        private static HDCDebugGroup ForceAdGroup(HDCAdCoreConfig.ForceAdGroup config, bool selected) =>
            FullscreenGroup(config.groupName, HDCAds.ExistingForceAdGroup(config.groupName), selected, ForceAdUnits(config), details => details
                .Line("Positions", Join(config.positionNames))
                .Line("Priority", Priority(config.mediationPriority))
                .Line("Backup", config.useBackup)
                .Line("Max Shows", config.maxShowCount > 0 ? config.maxShowCount.ToString() : "No limit")
                .Line("Reload After Show", !config.disablePostInitReload));

        private static HDCDebugGroup LaunchGroup()
        {
            HDCAdCoreConfig.Comeback comeback = HDCAds.CoreConfig.comebackChannel ?? new HDCAdCoreConfig.Comeback();
            bool forceAd = comeback.launchAdType == 0;
            HDCAdCoreConfig.ForceAdGroup config = forceAd ? HDCAds.CoreConfig.ForceAdGroupNamed(comeback.launchForceAdGroupName) : null;
            IEnumerable<HDCDebugUnit> planned = forceAd
                ? config != null ? ForceAdUnits(config) : new HDCDebugUnit[0]
                : new[] { Planned(HDCAdFormat.AppOpen, "ao_plugin", HDCAds.CoreConfig.appOpenUnit?.admobUnit?.id) };
            HDCDebugGroup group = FullscreenGroup(forceAd ? comeback.launchForceAdGroupName : "app_open", HDCAds.AppLaunch.ExistingGroup, true, planned,
                details => details.Add("Launch Ad", forceAd ? "Force ad group " + comeback.launchForceAdGroupName + (config == null ? " (missing in the ad core config)" : string.Empty) : "App open",
                    forceAd && config == null ? HDCDebugTone.Bad : HDCDebugTone.Normal));
            group.Kind = forceAd ? "Group" : "Channel";
            return group;
        }

        private static HDCDebugGroup ResumeGroup()
        {
            var group = new HDCDebugGroup { Kind = "Channel", Name = "app_resume", Selected = true };
            group.Details
                .Line("Ad", "Native full-screen ad of appResumeChannel")
                .Line("Layout Group", HDCAds.Config.appResumeChannel?.layoutGroup);
            string id = HDCAppResumeAds.DebugInstanceId;
            HDCAdRecord record = HDCAdsTracker.Find(HDCAdFormat.Fullscreen, id);
            group.Units.Add(new HDCDebugUnit
            {
                Index = 1,
                Format = HDCAdFormat.Fullscreen,
                Id = id,
                AdUnitId = record?.AdUnitId ?? HDCAppResumeAds.DebugAdUnitId,
                Created = HDCAds.AppResume.IsStarted,
                Started = HDCAds.AppResume.IsStarted,
                Record = record,
            });
            return group;
        }

        private static HDCDebugGroup BannerGroup(HDCBannerSlot slot, bool selected)
        {
            HDCAdCoreConfig.FullscreenUnit unit = HDCAds.CoreConfig.bannerUnit?.Slot(slot) ?? new HDCAdCoreConfig.FullscreenUnit();
            HDCAdsConfig.BannerSlot config = (HDCAds.Config.bannerChannel ?? new HDCAdsConfig.BannerChannel()).Slot(slot);
            var planned = new List<HDCDebugUnit>();
            IEnumerable<int> order = slot == HDCBannerSlot.FullBottom
                ? HDCAds.Order(unit.mediationPriority, unit.useBackup)
                : unit.mediationPriority == HDCAds.PluginUnit || unit.useBackup ? new[] { HDCAds.PluginUnit } : new int[0];
            foreach (int priority in order)
            {
                if (priority == HDCAds.PluginUnit && !string.IsNullOrEmpty(unit.admobUnit?.id))
                    planned.Add(Planned(HDCAdFormat.BannerView, "bn_plugin_" + slot, unit.admobUnit.id));
                else if (priority == HDCAds.NativeUnit && slot == HDCBannerSlot.FullBottom)
                    planned.Add(Planned(HDCAdFormat.Banner, "bn_native", Join(new[] { unit.androidUnit?.id }.Concat(unit.androidUnit?.ids ?? new string[0]))));
            }

            HDCDebugGroup group = RectGroup(slot.ToString(), HDCAds.Banner.ExistingGroup(slot), selected, planned, details => details
                .Needed("Enabled", HDCBannerAds.IsSlotEnabled(slot))
                .Line("Auto Init", config.autoInit)
                .Line("Auto Show On Load", config.autoShowOnLoad)
                .Line("Priority", Priority(unit.mediationPriority))
                .Line("Backup", unit.useBackup));
            group.Kind = "Placement";
            return group;
        }

        private static HDCDebugGroup PopupGroup(HDCAdCoreConfig.PopupGroup config, bool selected, bool askNative)
        {
            bool created = HDCAds.Popup.TryGetPopup(config.groupName, out string id, out bool requested, out bool placed);
            id = id ?? HDCPopupAds.PopupId(config.groupName);
            HDCAdRecord record = HDCAdsTracker.Find(HDCAdFormat.Popup, id);
            var group = new HDCDebugGroup { Name = config.groupName, Selected = selected };
            string nativeState = requested && askNative ? HDCAdsSdk.GetPopupState(id) : null;
            group.Details
                .Line("Positions", Join(config.positionNames))
                .Needed("Placed (Move)", placed)
                .Line("Reload After Show", !config.disablePostInitReload)
                .Line("Layout", config.androidUnit?.layout)
                .Line("Native State", nativeState ?? "-");
            group.Units.Add(new HDCDebugUnit
            {
                Index = 1,
                Format = HDCAdFormat.Popup,
                Id = id,
                AdUnitId = record?.AdUnitId ?? config.androidUnit?.id,
                Created = created,
                Started = requested,
                Ready = requested && (nativeState == "Displayable" || nativeState == "Loaded"),
                OnScreen = nativeState == "Showing",
                NativeState = nativeState,
                Record = record,
            });
            return group;
        }

        private static HDCDebugGroup FullscreenGroup(string name, HDCFullscreenGroup made, bool selected, IEnumerable<HDCDebugUnit> planned, Action<HDCDebugInfo> details)
        {
            var group = new HDCDebugGroup { Name = name, Selected = selected };
            details(group.Details);
            if (made == null)
            {
                AddPlanned(group, planned);
                return group;
            }

            group.Details
                .Line("Ready", made.IsReady)
                .Line("Showing", made.IsShowing)
                .Line("Shows Left", made.ShowsLeft < 0 ? "No limit" : made.ShowsLeft.ToString())
                .Line("Units Started", made.StartedCount + " / " + made.Sources.Count);
            for (int i = 0; i < made.Sources.Count; i++)
            {
                HDCFullscreenSource source = made.Sources[i];
                group.Units.Add(new HDCDebugUnit
                {
                    Index = i + 1,
                    Format = source.Format,
                    Id = source.Id,
                    AdUnitId = HDCAdsTracker.Find(source.Format, source.Id)?.AdUnitId ?? source.AdUnitId,
                    Created = true,
                    Started = i < made.StartedCount,
                    Ready = source.IsReady,
                    OnScreen = source.IsShowing,
                    Record = HDCAdsTracker.Find(source.Format, source.Id),
                });
            }

            return group;
        }

        private static HDCDebugGroup RectGroup(string name, HDCRectGroup made, bool selected, IEnumerable<HDCDebugUnit> planned, Action<HDCDebugInfo> details)
        {
            var group = new HDCDebugGroup { Name = name, Selected = selected };
            details(group.Details);
            if (made == null)
            {
                AddPlanned(group, planned);
                return group;
            }

            group.Details
                .Line("Loaded", made.IsLoaded)
                .Line("Showing", made.IsShowing)
                .Line("Units Started", made.StartedCount + " / " + made.Sources.Count);
            for (int i = 0; i < made.Sources.Count; i++)
            {
                HDCRectSource source = made.Sources[i];
                group.Units.Add(new HDCDebugUnit
                {
                    Index = i + 1,
                    Format = source.Format,
                    Id = source.Id,
                    AdUnitId = HDCAdsTracker.Find(source.Format, source.Id)?.AdUnitId ?? source.AdUnitId,
                    Created = true,
                    Started = i < made.StartedCount,
                    Ready = source.IsLoaded,
                    OnScreen = made.IsShowing && made.Visible == source,
                    Record = HDCAdsTracker.Find(source.Format, source.Id),
                });
            }

            return group;
        }

        private static void AddPlanned(HDCDebugGroup group, IEnumerable<HDCDebugUnit> planned)
        {
            int index = 0;
            foreach (HDCDebugUnit unit in planned)
            {
                unit.Index = ++index;
                unit.Record = HDCAdsTracker.Find(unit.Format, unit.Id);
                group.Units.Add(unit);
            }

            if (group.Units.Count == 0)
                group.Details.Add("Ad Units", "None in the ad core config", HDCDebugTone.Bad);
        }

        private static IEnumerable<HDCDebugUnit> ForceAdUnits(HDCAdCoreConfig.ForceAdGroup config)
        {
            foreach (int priority in HDCAds.Order(config.mediationPriority, config.useBackup))
            {
                if (priority == HDCAds.PluginUnit && !string.IsNullOrEmpty(config.admobUnit?.id))
                {
                    yield return Planned(HDCAdFormat.Interstitial, "fa_plugin_" + config.groupName, config.admobUnit.id);
                }
                else if (priority == HDCAds.NativeUnit && !string.IsNullOrEmpty(config.androidUnit?.id))
                {
                    bool interstitial = config.androidUnit.androidInterstitials?.switchToInterstitialAndroid ?? false;
                    yield return interstitial
                        ? Planned(HDCAdFormat.Interstitial, "fa_interstitial_" + config.groupName, config.androidUnit.id)
                        : Planned(HDCAdFormat.Fullscreen, "fa_" + config.groupName, config.androidUnit.id);
                }
            }
        }

        private static IEnumerable<HDCDebugUnit> RewardedUnits()
        {
            HDCAdCoreConfig.FullscreenUnit config = HDCAds.CoreConfig.rewardedUnit ?? new HDCAdCoreConfig.FullscreenUnit();
            foreach (int priority in HDCAds.Order(config.mediationPriority, config.useBackup))
            {
                if (priority == HDCAds.PluginUnit && !string.IsNullOrEmpty(config.admobUnit?.id))
                    yield return Planned(HDCAdFormat.Rewarded, "rw_plugin", config.admobUnit.id);
                else if (priority == HDCAds.NativeUnit && !string.IsNullOrEmpty(config.androidUnit?.id))
                    yield return Planned(HDCAdFormat.Fullscreen, "rw_native", config.androidUnit.id);
            }
        }

        private static HDCDebugUnit Planned(string format, string id, string adUnitId) =>
            new HDCDebugUnit { Format = format, Id = id, AdUnitId = adUnitId };

        /// <summary>What serves a unit: the Google Mobile Ads plugin (AdMob) or the native library, and its format.</summary>
        internal static string UnitName(string format, string id)
        {
            id = id ?? string.Empty;
            switch (format)
            {
                case HDCAdFormat.Interstitial:
                    return id.StartsWith("fa_interstitial_") ? "Native Interstitial" : "AdMob Interstitial";
                case HDCAdFormat.Fullscreen:
                    return id == "rw_native" ? "Native Full-Screen (Rewarded)" : "Native Full-Screen";
                case HDCAdFormat.Rewarded: return "AdMob Rewarded";
                case HDCAdFormat.AppOpen: return "AdMob App Open";
                case HDCAdFormat.Banner: return "Native Banner";
                case HDCAdFormat.BannerView: return "AdMob Banner";
                case HDCAdFormat.Mrec: return "AdMob MREC";
                case HDCAdFormat.Popup: return "Native Popup";
                default: return format;
            }
        }

        private static string Priority(int priority) =>
            priority == HDCAds.PluginUnit ? "0 · AdMob first" : priority == HDCAds.NativeUnit ? "1 · Native first" : priority.ToString();

        private static string Join(IEnumerable<string> values)
        {
            string[] list = (values ?? new string[0]).Where(value => !string.IsNullOrEmpty(value)).ToArray();
            return list.Length == 0 ? "-" : string.Join(", ", list);
        }
    }
}
