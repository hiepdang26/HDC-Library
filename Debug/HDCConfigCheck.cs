using System;
using System.Collections.Generic;
using System.Linq;
using HDC.Ads.Diagnostics;
using HDC.Ads.Domain;
using UnityEngine;

namespace HDC.Ads.DebugUI
{
    /// <summary>
    /// Checks the configs HDCAds runs on: how the load went, the JSON, and whether every enabled channel has its
    /// ad units, groups, positions and layouts. Each finding is in Vietnamese, with what to fix.
    /// </summary>
    internal static class HDCConfigCheck
    {
        internal enum Level
        {
            Error,
            Warning,
            Info,
        }

        internal sealed class Finding
        {
            internal Finding(Level level, string text)
            {
                Level = level;
                Text = text;
            }

            internal Level Level { get; }
            internal string Text { get; }
        }

        internal static List<Finding> Run()
        {
            var findings = new List<Finding>();
            CheckLoad(findings);
            string ads = HDCConfigReport.AppliedAds;
            string core = HDCConfigReport.AppliedCore;
            if (ads == null)
                return findings;

            HDCAdsConfig adsConfig = Parse<HDCAdsConfig>(ads, "ads_config", findings);
            if (adsConfig == null)
                return findings;
            if (string.IsNullOrEmpty(adsConfig.selectedAdCoreName))
                findings.Add(Error("ads_config thiếu selectedAdCoreName, nên không có ad core config: không kênh nào có ad unit."));

            string coreName = string.IsNullOrEmpty(adsConfig.selectedAdCoreName) ? "ad core config" : adsConfig.selectedAdCoreName;
            if (IsEmptyJson(core))
            {
                findings.Add(Error($"Ad core config '{coreName}' rỗng ({(string.IsNullOrWhiteSpace(core) ? "trống" : core.Trim())}): không kênh nào có ad unit. Điền giá trị cho key này trên Remote Config, hoặc config mặc định trong HDC > Edit configs."));
                return findings;
            }

            HDCAdCoreConfig coreConfig = Parse<HDCAdCoreConfig>(core, coreName, findings);
            if (coreConfig != null)
                CheckChannels(adsConfig, coreConfig, findings);
            return findings;
        }

        private static void CheckLoad(List<Finding> findings)
        {
            if (!HDCConfigReport.Started)
            {
                findings.Add(HDCConfigReport.AppliedAds == null
                    ? Warning("HDCAds chưa khởi tạo nên chưa có config. Mở game từ scene có HDCAdsSetup, hoặc bấm Init SDK.")
                    : Info("Config được truyền thẳng vào HDCAds.Initialize, không qua Remote Config của HDC."));
                return;
            }

            if (!HDCConfigReport.Finished)
            {
                findings.Add(Info("Đang tải Remote Config..."));
                return;
            }

            if (HDCConfigReport.DefaultsOnly)
                findings.Add(Info("Trong Editor HDC dùng config mặc định, không gọi Firebase (bật HDCRemoteConfig.FetchInEditor để fetch thật)."));
            else if (HDCConfigReport.Outcome == "Firebase unavailable")
                findings.Add(Error($"Firebase không chạy được ({HDCConfigReport.Firebase}): config lấy từ giá trị đã lưu trên máy, hoặc mặc định. Kiểm tra google-services.json / GoogleService-Info.plist."));
            else if (HDCConfigReport.Outcome == "fetch failed")
                findings.Add(Warning($"Fetch Remote Config lỗi ({HDCConfigReport.FetchStatus}): dùng giá trị Firebase kích hoạt từ lần trước, giá trị đã lưu, hoặc mặc định. Kiểm tra mạng của máy."));
            else if (HDCConfigReport.Outcome == "timeout")
                findings.Add(Warning("Remote Config chưa xong sau 10 giây: dùng giá trị đã lưu, hoặc mặc định."));

            // Without Firebase every key falls back; the error above already says why.
            if (HDCConfigReport.DefaultsOnly || HDCConfigReport.Firebase != "Available")
                return;
            foreach (HDCConfigEntry entry in HDCConfigReport.Entries)
            {
                if (entry.Source == HDCConfigSource.Saved)
                    findings.Add(Warning($"Remote Config không có giá trị cho '{entry.Key}': dùng giá trị lần trước lưu trên máy."));
                else if (entry.Source == HDCConfigSource.Default)
                    findings.Add(Warning($"Remote Config không có giá trị cho '{entry.Key}' và máy chưa lưu giá trị nào: dùng config mặc định của project."));
            }
        }

        private static void CheckChannels(HDCAdsConfig ads, HDCAdCoreConfig core, List<Finding> findings)
        {
            HDCAdCoreConfig.ForceAdGroup[] faGroups = (core.forceAdGroups ?? new HDCAdCoreConfig.ForceAdGroup[0]).Where(g => g != null).ToArray();
            HDCAdCoreConfig.PopupGroup[] puGroups = (core.popupGroups ?? new HDCAdCoreConfig.PopupGroup[0]).Where(g => g != null).ToArray();
            string[] faPositions = (ads.forceAdChannel?.positionConfigs ?? new HDCAdsConfig.ForceAdPosition[0]).Where(p => p != null).Select(p => p.positionName).ToArray();
            string[] puPositions = (ads.popupChannel?.positionConfigs ?? new HDCAdsConfig.PopupPosition[0]).Where(p => p != null).Select(p => p.positionName).ToArray();

            // App launch
            HDCAdCoreConfig.Comeback comeback = core.comebackChannel ?? new HDCAdCoreConfig.Comeback();
            if (ads.appLaunchChannel?.isEnabled ?? false)
            {
                if (comeback.launchAdType == 0 && core.ForceAdGroupNamed(comeback.launchForceAdGroupName) == null)
                    findings.Add(Error($"AL: comebackChannel.launchForceAdGroupName '{comeback.launchForceAdGroupName}' không có trong forceAdGroups."));
                else if (comeback.launchAdType != 0 && string.IsNullOrEmpty(core.appOpenUnit?.admobUnit?.id))
                    findings.Add(Error("AL: launch dùng app open nhưng appOpenUnit.admobUnit.id trống."));
            }

            // App resume
            HDCAdsConfig.AppResumeChannel resume = ads.appResumeChannel ?? new HDCAdsConfig.AppResumeChannel();
            if (resume.isEnabled)
            {
                if (string.IsNullOrEmpty(resume.adUnitId))
                    findings.Add(Error("AR: appResumeChannel.adUnitId trống."));
                if (!string.IsNullOrEmpty(resume.layoutGroup) && core.LayoutGroupNamed(resume.layoutGroup) == null)
                    findings.Add(Warning($"AR: layoutGroup '{resume.layoutGroup}' không có trong forceAdLayoutConfig."));
            }

            // Rewarded
            if ((ads.rewardedChannel?.isEnabled ?? false) && !HasUnit(core.rewardedUnit))
                findings.Add(Error("RW: rewardedUnit không có ad unit nào (admobUnit.id và androidUnit.id đều trống)."));

            // Force ads
            if (ads.forceAdChannel?.isEnabled ?? false)
            {
                if (faGroups.Length == 0)
                    findings.Add(Error("FA: bật forceAdChannel nhưng ad core config không có forceAdGroups."));
                foreach (string position in faPositions.Where(p => !string.IsNullOrEmpty(p) && string.IsNullOrEmpty(core.ForceAdGroupAt(p))))
                    findings.Add(Error($"FA: position '{position}' không thuộc force ad group nào (thêm vào positionNames của một group)."));
                foreach (HDCAdCoreConfig.ForceAdGroup group in faGroups)
                {
                    if (string.IsNullOrEmpty(group.admobUnit?.id) && string.IsNullOrEmpty(group.androidUnit?.id))
                        findings.Add(Error($"FA: group '{group.groupName}' không có ad unit nào."));
                    bool nativeFullscreen = !string.IsNullOrEmpty(group.androidUnit?.id) && !(group.androidUnit.androidInterstitials?.switchToInterstitialAndroid ?? false);
                    if (nativeFullscreen && core.LayoutGroupNamed(group.androidUnit.layoutGroupName) == null)
                        findings.Add(Error($"FA: group '{group.groupName}' dùng layout group '{group.androidUnit.layoutGroupName}' không có trong forceAdLayoutConfig."));
                    foreach (string position in (group.positionNames ?? new string[0]).Where(p => !string.IsNullOrEmpty(p) && !faPositions.Contains(p)))
                        findings.Add(Warning($"FA: group '{group.groupName}' có position '{position}' không có trong forceAdChannel.positionConfigs."));
                }
            }

            // Banner
            if (ads.bannerChannel?.isEnabled ?? false)
            {
                foreach (HDCBannerSlot slot in (HDCBannerSlot[])Enum.GetValues(typeof(HDCBannerSlot)))
                {
                    if (!ads.bannerChannel.Slot(slot).isEnabled)
                        continue;
                    HDCAdCoreConfig.FullscreenUnit unit = core.bannerUnit?.Slot(slot);
                    bool native = slot == HDCBannerSlot.FullBottom && unit?.androidUnit != null
                        && (!string.IsNullOrEmpty(unit.androidUnit.id) || (unit.androidUnit.ids?.Length ?? 0) > 0);
                    if (string.IsNullOrEmpty(unit?.admobUnit?.id) && !native)
                        findings.Add(Error($"BN: slot {slot} đang bật nhưng bannerUnit không có ad unit cho slot này."));
                }
            }

            // MREC
            if ((ads.mrecChannel?.isEnabled ?? false) && string.IsNullOrEmpty(core.mrecUnit?.admobUnit?.id))
                findings.Add(Error("MREC: mrecUnit.admobUnit.id trống."));

            // Popups
            if (ads.popupChannel?.isEnabled ?? false)
            {
                if (puGroups.Length == 0)
                    findings.Add(Error("PU: bật popupChannel nhưng ad core config không có popupGroups."));
                foreach (string position in puPositions.Where(p => !string.IsNullOrEmpty(p) && string.IsNullOrEmpty(core.PopupGroupAt(p))))
                    findings.Add(Error($"PU: position '{position}' không thuộc popup group nào."));
                foreach (HDCAdCoreConfig.PopupGroup group in puGroups.Where(g => string.IsNullOrEmpty(g.androidUnit?.id)))
                    findings.Add(Error($"PU: group '{group.groupName}' không có androidUnit.id."));
            }

            // Positions named twice.
            foreach (string position in Duplicates(faPositions))
                findings.Add(Error($"FA: position '{position}' bị khai báo nhiều lần trong positionConfigs."));
            foreach (string position in Duplicates(puPositions))
                findings.Add(Error($"PU: position '{position}' bị khai báo nhiều lần trong positionConfigs."));
            foreach (string position in faPositions.Intersect(puPositions).Where(p => !string.IsNullOrEmpty(p)))
                findings.Add(Warning($"Position '{position}' có ở cả FA và PU: dễ nhầm khi gọi Show."));
        }

        private static T Parse<T>(string json, string name, List<Finding> findings) where T : class
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                findings.Add(Error($"{name} trống."));
                return null;
            }

            try
            {
                return JsonUtility.FromJson<T>(json);
            }
            catch (Exception exception)
            {
                findings.Add(Error($"{name} không phải JSON hợp lệ: {exception.Message}"));
                return null;
            }
        }

        private static bool HasUnit(HDCAdCoreConfig.FullscreenUnit unit) =>
            unit != null && (!string.IsNullOrEmpty(unit.admobUnit?.id) || !string.IsNullOrEmpty(unit.androidUnit?.id));

        private static bool IsEmptyJson(string json)
        {
            string compact = new string((json ?? string.Empty).Where(c => !char.IsWhiteSpace(c)).ToArray());
            return compact.Length == 0 || compact == "{}";
        }

        private static IEnumerable<string> Duplicates(IEnumerable<string> values) =>
            values.Where(v => !string.IsNullOrEmpty(v)).GroupBy(v => v).Where(g => g.Count() > 1).Select(g => g.Key);

        private static Finding Error(string text) => new Finding(Level.Error, text);

        private static Finding Warning(string text) => new Finding(Level.Warning, text);

        private static Finding Info(string text) => new Finding(Level.Info, text);
    }
}
