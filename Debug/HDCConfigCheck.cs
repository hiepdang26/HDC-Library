using System;
using System.Collections.Generic;
using System.Linq;
using HDC.Ads.Diagnostics;
using HDC.Ads.Domain;
using HDC.Ads.Logic;
using UnityEngine;

namespace HDC.Ads.DebugUI
{
    /// <summary>
    /// Checks the configs HDCAds runs on: how the load went, the JSON, then each channel's rules, which say whether
    /// the channel has its ad units, groups, positions and layouts, and last the positions listed twice. Each
    /// finding is in Vietnamese, with what to fix.
    /// </summary>
    internal static class HDCConfigCheck
    {
        internal static List<HDCConfigFinding> Run()
        {
            var findings = new List<HDCConfigFinding>();
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

        private static void CheckLoad(List<HDCConfigFinding> findings)
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

        private static void CheckChannels(HDCAdsConfig ads, HDCAdCoreConfig core, List<HDCConfigFinding> findings)
        {
            IReadOnlyList<IAdChannel> channels = HDCAds.Channels.All;
            foreach (IAdChannel channel in channels)
                findings.AddRange(channel.ConfigRule.Check(ads, core));

            // Positions named twice, in a channel or by two of them.
            string[][] positions = channels.Select(channel => channel.ConfigRule.Positions(ads).ToArray()).ToArray();
            for (int i = 0; i < channels.Count; i++)
            {
                foreach (string position in Duplicates(positions[i]))
                    findings.Add(Error($"{channels[i].Key}: position '{position}' bị khai báo nhiều lần trong positionConfigs."));
            }

            for (int i = 0; i < channels.Count; i++)
            {
                for (int j = i + 1; j < channels.Count; j++)
                {
                    foreach (string position in positions[i].Intersect(positions[j]).Where(p => !string.IsNullOrEmpty(p)))
                        findings.Add(Warning($"Position '{position}' có ở cả {channels[i].Key} và {channels[j].Key}: dễ nhầm khi gọi Show."));
                }
            }
        }

        private static T Parse<T>(string json, string name, List<HDCConfigFinding> findings) where T : class
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

        private static bool IsEmptyJson(string json)
        {
            string compact = new string((json ?? string.Empty).Where(c => !char.IsWhiteSpace(c)).ToArray());
            return compact.Length == 0 || compact == "{}";
        }

        private static IEnumerable<string> Duplicates(IEnumerable<string> values) =>
            values.Where(v => !string.IsNullOrEmpty(v)).GroupBy(v => v).Where(g => g.Count() > 1).Select(g => g.Key);

        private static HDCConfigFinding Error(string text) => HDCConfigFinding.Error(text);

        private static HDCConfigFinding Warning(string text) => HDCConfigFinding.Warning(text);

        private static HDCConfigFinding Info(string text) => HDCConfigFinding.Info(text);
    }
}
