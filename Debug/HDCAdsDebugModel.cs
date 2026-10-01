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

    /// <summary>A group, slot or channel and its ad units in order.</summary>
    internal sealed class HDCDebugGroup
    {
        internal string Name;
        internal string Info;
        internal bool Selected;
        internal readonly List<HDCDebugUnit> Units = new List<HDCDebugUnit>();
    }

    /// <summary>The groups and ad units of each channel, from the configs and from what the channels made so far.</summary>
    internal static class HDCAdsDebugModel
    {
        internal static readonly string[] BannerSlots = Enum.GetNames(typeof(HDCBannerSlot));

        internal static List<HDCDebugGroup> Groups(string channel, string selectedGroup, string selectedPosition)
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
                    groups.Add(FullscreenGroup("rewarded", HDCAds.ExistingRewardedGroup, true, RewardedUnits(),
                        $"priority: {Priority(HDCAds.CoreConfig.rewardedUnit?.mediationPriority ?? 0)} · backup: {OnOff(HDCAds.CoreConfig.rewardedUnit?.useBackup ?? false)}"));
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
                    }, $"priority: {Priority(HDCAds.CoreConfig.mrecUnit?.mediationPriority ?? 0)}"));
                    break;
                case "PU":
                    foreach (HDCAdCoreConfig.PopupGroup config in HDCAds.CoreConfig.popupGroups ?? new HDCAdCoreConfig.PopupGroup[0])
                    {
                        if (config != null && !string.IsNullOrEmpty(config.groupName))
                            groups.Add(PopupGroup(config, config.groupName == selectedGroup));
                    }

                    break;
            }

            return groups;
        }

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

        private static HDCDebugGroup ForceAdGroup(HDCAdCoreConfig.ForceAdGroup config, bool selected)
        {
            HDCDebugGroup group = FullscreenGroup(config.groupName, HDCAds.ExistingForceAdGroup(config.groupName), selected,
                ForceAdUnits(config),
                $"positions: {Join(config.positionNames)} · priority: {Priority(config.mediationPriority)} · backup: {OnOff(config.useBackup)}"
                + (config.maxShowCount > 0 ? $" · max shows: {config.maxShowCount}" : string.Empty)
                + (config.disablePostInitReload ? " · no reload after init" : string.Empty));
            return group;
        }

        private static HDCDebugGroup LaunchGroup()
        {
            HDCAdCoreConfig.Comeback comeback = HDCAds.CoreConfig.comebackChannel ?? new HDCAdCoreConfig.Comeback();
            bool forceAd = comeback.launchAdType == 0;
            HDCAdCoreConfig.ForceAdGroup config = forceAd ? HDCAds.CoreConfig.ForceAdGroupNamed(comeback.launchForceAdGroupName) : null;
            IEnumerable<HDCDebugUnit> planned = forceAd
                ? config != null ? ForceAdUnits(config) : new HDCDebugUnit[0]
                : new[] { Planned(HDCAdFormat.AppOpen, "ao_plugin", HDCAds.CoreConfig.appOpenUnit?.admobUnit?.id) };
            string info = forceAd
                ? $"launch ad: force ad group \"{comeback.launchForceAdGroupName}\"" + (config == null ? " (missing in the ad core config)" : string.Empty)
                : "launch ad: app open";
            return FullscreenGroup(forceAd ? comeback.launchForceAdGroupName : "app_open", HDCAds.AppLaunch.ExistingGroup, true, planned, info);
        }

        private static HDCDebugGroup ResumeGroup()
        {
            var group = new HDCDebugGroup { Name = "app_resume", Info = "native full-screen ad of ads_config.appResumeChannel", Selected = true };
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

            string info = $"enabled: {OnOff(HDCBannerAds.IsSlotEnabled(slot))} · autoInit: {OnOff(config.autoInit)} · autoShowOnLoad: {OnOff(config.autoShowOnLoad)}"
                + $" · priority: {Priority(unit.mediationPriority)} · backup: {OnOff(unit.useBackup)}";
            return RectGroup(slot.ToString(), HDCAds.Banner.ExistingGroup(slot), selected, planned, info);
        }

        private static HDCDebugGroup PopupGroup(HDCAdCoreConfig.PopupGroup config, bool selected)
        {
            bool created = HDCAds.Popup.TryGetPopup(config.groupName, out string id, out bool requested, out bool placed);
            id = id ?? HDCPopupAds.PopupId(config.groupName);
            HDCAdRecord record = HDCAdsTracker.Find(HDCAdFormat.Popup, id);
            var group = new HDCDebugGroup
            {
                Name = config.groupName,
                Selected = selected,
                Info = $"positions: {Join(config.positionNames)} · placed (Move): {OnOff(placed)}"
                    + (config.disablePostInitReload ? " · no reload after init" : string.Empty),
            };
            string nativeState = requested ? HDCAdsSdk.GetPopupState(id) : null;
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

        private static HDCDebugGroup FullscreenGroup(string name, HDCFullscreenGroup made, bool selected, IEnumerable<HDCDebugUnit> planned, string info)
        {
            var group = new HDCDebugGroup { Name = name, Selected = selected, Info = info };
            if (made == null)
            {
                AddPlanned(group, planned);
                return group;
            }

            group.Info += $" · shows left: {(made.ShowsLeft < 0 ? "no limit" : made.ShowsLeft.ToString())} · units started: {made.StartedCount}/{made.Sources.Count}";
            for (int i = 0; i < made.Sources.Count; i++)
            {
                HDCFullscreenSource source = made.Sources[i];
                group.Units.Add(new HDCDebugUnit
                {
                    Index = i + 1,
                    Format = source.Format,
                    Id = source.Id,
                    AdUnitId = source.AdUnitId,
                    Created = true,
                    Started = i < made.StartedCount,
                    Ready = source.IsReady,
                    OnScreen = source.IsShowing,
                    Record = HDCAdsTracker.Find(source.Format, source.Id),
                });
            }

            return group;
        }

        private static HDCDebugGroup RectGroup(string name, HDCRectGroup made, bool selected, IEnumerable<HDCDebugUnit> planned, string info)
        {
            var group = new HDCDebugGroup { Name = name, Selected = selected, Info = info };
            if (made == null)
            {
                AddPlanned(group, planned);
                return group;
            }

            group.Info += $" · showing: {OnOff(made.IsShowing)} · units started: {made.StartedCount}/{made.Sources.Count}";
            for (int i = 0; i < made.Sources.Count; i++)
            {
                HDCRectSource source = made.Sources[i];
                group.Units.Add(new HDCDebugUnit
                {
                    Index = i + 1,
                    Format = source.Format,
                    Id = source.Id,
                    AdUnitId = source.AdUnitId,
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
                group.Info += " · no ad unit in the ad core config";
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

        internal static string FormatName(string format)
        {
            switch (format)
            {
                case HDCAdFormat.Interstitial: return "Interstitial";
                case HDCAdFormat.Fullscreen: return "Native full-screen";
                case HDCAdFormat.Rewarded: return "Rewarded (plugin)";
                case HDCAdFormat.AppOpen: return "App open (plugin)";
                case HDCAdFormat.Banner: return "Native banner";
                case HDCAdFormat.BannerView: return "Banner (plugin)";
                case HDCAdFormat.Mrec: return "MREC (plugin)";
                case HDCAdFormat.Popup: return "Native popup";
                default: return format;
            }
        }

        private static string Priority(int priority) =>
            priority == HDCAds.PluginUnit ? "0 plugin first" : priority == HDCAds.NativeUnit ? "1 native first" : priority.ToString();

        private static string OnOff(bool value) => value ? "on" : "off";

        private static string Join(IEnumerable<string> values)
        {
            string[] list = (values ?? new string[0]).Where(value => !string.IsNullOrEmpty(value)).ToArray();
            return list.Length == 0 ? "-" : string.Join(", ", list);
        }
    }
}
