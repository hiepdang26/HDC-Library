using System;
using System.Collections.Generic;
using System.Linq;
using HDC.Ads.Diagnostics;
using HDC.Ads.Domain;
using HDC.Ads.Logic;
using UnityEngine;

namespace HDC.Ads.DebugUI
{
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
            if (!adsConfig.toggleConsent)
                findings.Add(Info("toggleConsent=false: không bao giờ hiện form consent UMP; quảng cáo được request như đã có consent."));
            if (!adsConfig.toggleAtt)
                findings.Add(Info("toggleAtt=false: không bao giờ hỏi ATT trên iOS; trạng thái tracking giữ Not Determined (không có IDFA)."));

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

            if (HDCConfigReport.Plan == HDCConfigPlan.EditorDefaults)
                findings.Add(Info("Trong Editor HDC dùng config mặc định, không gọi Firebase (bật HDCRemoteConfig.FetchInEditor để fetch thật)."));
            else if (HDCConfigReport.Plan == HDCConfigPlan.Default)
                findings.Add(Info("HDC Ads Settings chọn Default cho bản build này: dùng config mặc định, không fetch Remote Config."));
            else if (HDCConfigReport.Plan == HDCConfigPlan.NoFirebase)
                findings.Add(Info("Bản build không có Firebase (thiếu define HDC_FIREBASE): dùng config mặc định, không có Remote Config."));
            else if (HDCConfigReport.Outcome == "Firebase unavailable")
                findings.Add(Error($"Firebase không chạy được ({HDCConfigReport.Firebase}): dùng config mặc định. Kiểm tra google-services.json / GoogleService-Info.plist."));
            else if (HDCConfigReport.Outcome == "fetch failed" || HDCConfigReport.Outcome == "activation failed")
                findings.Add(Warning($"Fetch Remote Config lỗi ({Dash(HDCConfigReport.FetchStatus)}): dùng config mặc định. Kiểm tra mạng của máy."));
            else if (HDCConfigReport.Outcome == "timeout")
                findings.Add(Warning($"Remote Config chưa xong sau {HDCConfigReport.TimeoutSeconds:0} giây game chạy: dùng config mặc định."));

            if (HDCConfigReport.JoinedLoads > 0)
                findings.Add(Info($"Có {HDCConfigReport.JoinedLoads} lần gọi tải config khi đang tải (ví dụ bấm Init SDK): các lần này chờ lần tải đang chạy, không fetch thêm."));
            if (HDCConfigReport.Outcome != "fetched")
                return;
            foreach (HDCConfigEntry entry in HDCConfigReport.Entries)
            {
                if (entry.Source == HDCConfigSource.Default)
                    findings.Add(Warning($"Remote Config không có giá trị cho '{entry.Key}': dùng config mặc định của project."));
            }
        }

        private static string Dash(string value) => string.IsNullOrEmpty(value) ? "-" : value;

        private static void CheckChannels(HDCAdsConfig ads, HDCAdCoreConfig core, List<HDCConfigFinding> findings)
        {
            IReadOnlyList<IAdChannel> channels = HDCAds.Channels.All;
            foreach (IAdChannel channel in channels)
                findings.AddRange(channel.ConfigRule.Check(ads, core));

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
