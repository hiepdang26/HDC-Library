using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor.Compilation;
using UnityEngine;

namespace HDC.Tool.Editor
{
    internal static class HDCSdkVersions
    {
        internal const string HdcGroup = "HDC";
        internal const string GoogleGroup = "Google";
        internal const string MediationGroup = "Mediation";
        internal const string AnalyticsGroup = "Analytics & attribution";
        internal const string None = "-";

        private const string MediationPrefix = "com.google.ads.mediation:";
        private const string MediationPod = "GoogleMobileAdsMediation";

        private static readonly Regex AndroidSpec = new Regex("spec=\"(?<group>[^:\"]+):(?<artifact>[^:\"]+):(?<version>[^\"]+)\"");
        private static readonly Regex IosPod = new Regex("iosPod\\s+name=\"(?<name>[^\"]+)\"\\s+version=\"(?<version>[^\"]+)\"");
        private static readonly Regex PackageVersion = new Regex("\"version\"\\s*:\\s*\"(?<version>[^\"]+)\"");
        private static readonly Regex GmaManifest = new Regex("GoogleMobileAds_version-(?<version>.+)_manifest\\.txt$");
        private static readonly Regex LockedPod = new Regex(@"^  - (?<name>[^ ]+) \((?<version>[^)]+)\)");
        private static readonly Regex Pin = new Regex("\"identity\"\\s*:\\s*\"(?<identity>[^\"]+)\"[^}]*?\"version\"\\s*:\\s*\"(?<version>[^\"]+)\"", RegexOptions.Singleline);

        private static readonly Dictionary<string, (string Name, string IosSdkPod)> Networks = new Dictionary<string, (string, string)>
        {
            { "facebook", ("Meta Audience Network", "FBAudienceNetwork") },
            { "applovin", ("AppLovin", "AppLovinSDK") },
            { "pangle", ("Pangle", "Ads-Global") },
            { "unity", ("Unity Ads", "UnityAds") },
            { "ironsource", ("ironSource", "IronSourceSDK") },
            { "mintegral", ("Mintegral", "MintegralAdSDK") },
            { "vungle", ("Liftoff Monetize", "VungleAds") },
            { "inmobi", ("InMobi", "InMobiSDK") },
            { "chartboost", ("Chartboost", "ChartboostSDK") },
            { "fyber", ("DT Exchange", "Fyber_Marketplace_SDK") },
            { "moloco", ("Moloco", "MolocoSDKiOS") },
            { "line", ("LINE", "FiveAd") },
            { "maio", ("maio", "Maio") },
            { "mytarget", ("myTarget", "myTargetSDK") },
            { "imobile", ("i-mobile", "i-mobile") },
            { "adcolony", ("AdColony", "AdColony") },
            { "bidmachine", ("BidMachine", "BidMachine") },
            { "verve", ("Verve", "HyBid") },
        };

        internal sealed class Entry
        {
            internal string Group = "";
            internal string Name = "";
            internal bool Network;
            internal bool Sub;
            internal string Android = None;
            internal string Ios = None;
            internal bool IosDeclared;
            internal string AndroidTip = "";
            internal string IosTip = "";
        }

        internal sealed class Sources
        {
            internal readonly Dictionary<string, string> Android = new Dictionary<string, string>(StringComparer.Ordinal);
            internal readonly Dictionary<string, string> Ios = new Dictionary<string, string>(StringComparer.Ordinal);
            internal readonly Dictionary<string, string> IosResolved = new Dictionary<string, string>(StringComparer.Ordinal);
            internal string LibraryVersion = None;
            internal string NativeIos = None;
            internal string GmaPlugin = None;
        }

        internal static List<Entry> Read(string iosExportFolder) => Read(iosExportFolder, Application.dataPath, HDCLibRoot());

        internal static List<Entry> Read(string iosExportFolder, string dataPath, string libRoot)
        {
            var sources = new Sources();
            foreach (string file in DependencyFiles(dataPath))
                Collect(Safe(() => File.ReadAllText(file)) ?? "", sources.Android, sources.Ios);
            string lockFile = Path.Combine(iosExportFolder ?? "", "Podfile.lock");
            foreach (KeyValuePair<string, string> pod in LockedPods(Safe(() => File.ReadAllText(lockFile)) ?? ""))
                sources.IosResolved[pod.Key] = pod.Value;
            string resolved = Path.Combine(iosExportFolder ?? "", "Unity-iPhone.xcworkspace", "xcshareddata", "swiftpm", "Package.resolved");
            foreach (KeyValuePair<string, string> pin in Pins(Safe(() => File.ReadAllText(resolved)) ?? ""))
                sources.IosResolved["spm:" + pin.Key] = pin.Value;
            sources.LibraryVersion = PackageJsonVersion(libRoot);
            sources.NativeIos = XcframeworkDate(libRoot);
            sources.GmaPlugin = GmaPluginVersion(dataPath);
            return Build(sources);
        }

        internal static List<Entry> Build(Sources sources)
        {
            const string package = "HDCLib/package.json";
            string firebaseUnity = Value(sources.Android, "com.google.firebase:firebase-app-unity");
            var entries = new List<Entry>
            {
                new Entry { Group = HdcGroup, Name = "HDCLib", Android = sources.LibraryVersion, Ios = sources.LibraryVersion, AndroidTip = package, IosTip = package },
                Maven(HdcGroup, "hdc-ads-android", sources, "com.hdc.adsmultiplatform:hdc-ads-android"),
                new Entry { Group = HdcGroup, Name = "HDCAds.xcframework", Ios = sources.NativeIos, IosTip = "HDCAds/Plugins/iOS/HDCAds.xcframework has no version number, so its date is shown." },
                new Entry { Group = GoogleGroup, Name = "Mobile Ads Unity plugin", Android = sources.GmaPlugin, Ios = sources.GmaPlugin },
                Both(GoogleGroup, "Mobile Ads SDK", sources, "com.google.android.gms:play-services-ads", "Google-Mobile-Ads-SDK"),
                Both(GoogleGroup, "User Messaging Platform", sources, "com.google.android.ump:user-messaging-platform", "GoogleUserMessagingPlatform"),
            };

            List<Entry> mediation = Mediation(sources);
            entries.AddRange(mediation.Count > 0 ? mediation : new List<Entry> { new Entry { Group = MediationGroup, Name = "No mediation adapter" } });

            entries.Add(Both(AnalyticsGroup, "Adjust", sources, "com.adjust.sdk:adjust-android", "Adjust"));
            entries.Add(new Entry
            {
                Group = AnalyticsGroup,
                Name = "Firebase Unity SDK",
                Android = firebaseUnity,
                Ios = firebaseUnity,
                AndroidTip = "com.google.firebase:firebase-app-unity",
                IosTip = "One Firebase Unity SDK version for both platforms.",
            });
            entries.Add(Firebase(sources, "Firebase Analytics", "com.google.firebase:firebase-analytics", "FirebaseAnalytics", "Firebase/Analytics"));
            entries.Add(Firebase(sources, "Firebase Remote Config", "com.google.firebase:firebase-config", "FirebaseRemoteConfig", "Firebase/RemoteConfig"));
            entries.RemoveAll(entry => entry.Group != MediationGroup && entry.Android == None && entry.Ios == None && entry.Name != "HDCLib");
            return entries;
        }

        internal static List<Entry> ForPlatform(IReadOnlyList<Entry> entries, bool ios)
        {
            var result = new List<Entry>();
            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                if (entry.Network)
                {
                    List<Entry> rows = entries.Skip(i + 1).TakeWhile(row => row.Sub).ToList();
                    List<Entry> used = rows.Where(row => Has(row, ios)).ToList();
                    if (used.Count > 0)
                    {
                        result.Add(entry);
                        result.AddRange(used);
                    }

                    i += rows.Count;
                    continue;
                }

                if (Has(entry, ios) || entry.Name == "HDCLib")
                    result.Add(entry);
            }

            if (result.All(entry => entry.Group != MediationGroup))
            {
                int analytics = result.FindIndex(entry => entry.Group == AnalyticsGroup);
                result.Insert(analytics < 0 ? result.Count : analytics, new Entry { Group = MediationGroup, Name = "No mediation adapter" });
            }

            return result;
        }

        internal static string Value(Entry entry, bool ios) => ios ? entry.Ios : entry.Android;

        internal static List<Entry> Mediation(Sources sources)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (string spec in sources.Android.Keys.Where(key => key.StartsWith(MediationPrefix, StringComparison.Ordinal)))
                keys.Add(Key(spec.Substring(MediationPrefix.Length)));
            foreach (string pod in sources.Ios.Keys.Concat(sources.IosResolved.Keys).Where(key => key.StartsWith(MediationPod, StringComparison.Ordinal)))
                keys.Add(Key(pod.Substring(MediationPod.Length)));

            var entries = new List<Entry>();
            foreach (string key in keys.OrderBy(NetworkName, StringComparer.OrdinalIgnoreCase))
            {
                string sdkPod = Networks.TryGetValue(key, out (string Name, string IosSdkPod) known) ? known.IosSdkPod : "";
                string spec = sources.Android.Keys.FirstOrDefault(name => name.StartsWith(MediationPrefix, StringComparison.Ordinal) && Key(name.Substring(MediationPrefix.Length)) == key);
                string pod = sources.Ios.Keys.Concat(sources.IosResolved.Keys)
                    .FirstOrDefault(name => name.StartsWith(MediationPod, StringComparison.Ordinal) && Key(name.Substring(MediationPod.Length)) == key);

                var adapter = new Entry { Group = MediationGroup, Name = "Adapter", Sub = true, Android = spec == null ? None : sources.Android[spec], AndroidTip = spec ?? "" };
                if (pod != null)
                    Ios(adapter, sources, pod);

                var sdk = new Entry
                {
                    Group = MediationGroup,
                    Name = "SDK",
                    Sub = true,
                    Android = NetworkSdk(adapter.Android),
                    AndroidTip = adapter.Android == None ? "" : "The adapter version without its last number.",
                };
                if (sdkPod.Length > 0 && sources.IosResolved.TryGetValue(sdkPod, out string resolved))
                {
                    sdk.Ios = resolved;
                    sdk.IosTip = sdkPod + ", resolved in the last export.";
                }
                else
                {
                    sdk.Ios = NetworkSdk(adapter.Ios);
                    sdk.IosDeclared = adapter.IosDeclared && sdk.Ios != None;
                    sdk.IosTip = sdk.Ios == None ? "" : "The adapter version without its last number.";
                }

                entries.Add(new Entry { Group = MediationGroup, Name = NetworkName(key), Network = true, Android = "", Ios = "" });
                entries.Add(adapter);
                entries.Add(sdk);
            }

            return entries;
        }

        internal static string NetworkSdk(string adapter)
        {
            if (string.IsNullOrEmpty(adapter) || adapter == None)
                return None;
            string[] parts = Regex.Replace(adapter, "^[^0-9]*", "").Split('.');
            return parts.Length >= 4 ? string.Join(".", parts.Take(parts.Length - 1)) : None;
        }

        internal static Dictionary<string, string> LockedPods(string podfileLock)
        {
            var pods = new Dictionary<string, string>(StringComparer.Ordinal);
            bool inPods = false;
            foreach (string line in (podfileLock ?? "").Split('\n'))
            {
                if (line.StartsWith("PODS:", StringComparison.Ordinal))
                {
                    inPods = true;
                    continue;
                }

                if (inPods && line.Length > 0 && !line.StartsWith(" ", StringComparison.Ordinal))
                    break;
                Match match = LockedPod.Match(line);
                if (inPods && match.Success && !match.Groups["version"].Value.Contains("="))
                    pods[match.Groups["name"].Value] = match.Groups["version"].Value;
            }

            return pods;
        }

        internal static Dictionary<string, string> Pins(string packageResolved)
        {
            var pins = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (Match match in Pin.Matches(packageResolved ?? ""))
                pins[match.Groups["identity"].Value] = match.Groups["version"].Value;
            return pins;
        }

        internal static void Collect(string text, Dictionary<string, string> android, Dictionary<string, string> ios)
        {
            foreach (Match match in AndroidSpec.Matches(text ?? ""))
                Keep(android, match.Groups["group"].Value + ":" + match.Groups["artifact"].Value, match.Groups["version"].Value);
            foreach (Match match in IosPod.Matches(text ?? ""))
                Keep(ios, match.Groups["name"].Value, match.Groups["version"].Value);
        }

        internal static int Compare(string left, string right)
        {
            int[] a = Numbers(left);
            int[] b = Numbers(right);
            for (int i = 0; i < Math.Max(a.Length, b.Length); i++)
            {
                int x = i < a.Length ? a[i] : 0;
                int y = i < b.Length ? b[i] : 0;
                if (x != y)
                    return x.CompareTo(y);
            }

            return 0;
        }

        internal static bool IsIgnored(string relativePath) =>
            (relativePath ?? "").Split('/', '\\').Any(part => part.EndsWith("~", StringComparison.Ordinal) || part.StartsWith(".", StringComparison.Ordinal));

        internal static string HDCLibRoot()
        {
            string asmdef = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName("HDC.Tool.Editor");
            string root = string.IsNullOrEmpty(asmdef) ? null : Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(asmdef)));
            return Path.GetFullPath(string.IsNullOrEmpty(root) ? "Assets/HDCLib" : root);
        }

        private static bool Has(Entry entry, bool ios) => Value(entry, ios) != None;

        private static Entry Maven(string group, string name, Sources sources, string spec) =>
            new Entry { Group = group, Name = name, Android = Value(sources.Android, spec), AndroidTip = spec };

        private static Entry Both(string group, string name, Sources sources, string spec, string pod)
        {
            Entry entry = Maven(group, name, sources, spec);
            Ios(entry, sources, pod);
            return entry;
        }

        private static void Ios(Entry entry, Sources sources, string pod)
        {
            string declared = Value(sources.Ios, pod);
            if (sources.IosResolved.TryGetValue(pod, out string resolved))
            {
                entry.Ios = resolved;
                entry.IosTip = pod + ", resolved in the last export" + (declared != None ? " (declared " + declared + ")." : ".");
                return;
            }

            entry.Ios = declared;
            entry.IosDeclared = declared != None;
            entry.IosTip = entry.IosDeclared ? pod + ", declared " + declared + ". Export once to see the version CocoaPods resolves." : "";
        }

        private static Entry Firebase(Sources sources, string name, string spec, string resolvedPod, string declaredPod)
        {
            Entry entry = Maven(AnalyticsGroup, name, sources, spec);
            if (sources.IosResolved.TryGetValue("spm:firebase-ios-sdk", out string package))
            {
                entry.Ios = package;
                entry.IosTip = "firebase-ios-sdk, resolved by Swift Package Manager in the last export.";
            }
            else if (sources.IosResolved.ContainsKey(resolvedPod))
            {
                Ios(entry, sources, resolvedPod);
            }
            else
            {
                Ios(entry, sources, declaredPod);
            }

            return entry;
        }

        private static string Key(string name) => Regex.Replace((name ?? "").ToLowerInvariant(), "[^a-z0-9]", "");

        private static string NetworkName(string key) =>
            Networks.TryGetValue(key, out (string Name, string IosSdkPod) known) ? known.Name : key.Length == 0 ? key : char.ToUpperInvariant(key[0]) + key.Substring(1);

        private static IEnumerable<string> DependencyFiles(string dataPath) =>
            Safe(() => Directory.EnumerateFiles(dataPath, "*Dependencies.xml", SearchOption.AllDirectories)
                .Where(file => !IsIgnored(file.Substring(dataPath.Length)))
                .ToList()) ?? new List<string>();

        private static void Keep(Dictionary<string, string> values, string key, string version)
        {
            if (!values.TryGetValue(key, out string current) || Compare(version, current) > 0)
                values[key] = version;
        }

        private static int[] Numbers(string version) =>
            Regex.Matches(version ?? "", "[0-9]+").Cast<Match>().Select(match => int.TryParse(match.Value, out int number) ? number : 0).ToArray();

        private static string Value(Dictionary<string, string> values, string key) => values.TryGetValue(key, out string value) ? value : None;

        private static string PackageJsonVersion(string root)
        {
            string text = Safe(() => File.ReadAllText(Path.Combine(root, "package.json")));
            Match match = text == null ? Match.Empty : PackageVersion.Match(text);
            return match.Success ? match.Groups["version"].Value : None;
        }

        private static string XcframeworkDate(string root)
        {
            string plist = Path.Combine(root, "HDCAds/Plugins/iOS/HDCAds.xcframework/Info.plist");
            return File.Exists(plist) ? "built " + File.GetLastWriteTime(plist).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : None;
        }

        private static string GmaPluginVersion(string dataPath)
        {
            string folder = Path.Combine(dataPath, "GoogleMobileAds");
            string manifest = Directory.Exists(folder) ? Directory.EnumerateFiles(folder, "GoogleMobileAds_version-*_manifest.txt").FirstOrDefault() : null;
            Match match = manifest == null ? Match.Empty : GmaManifest.Match(Path.GetFileName(manifest));
            return match.Success ? match.Groups["version"].Value : None;
        }

        private static T Safe<T>(Func<T> read) where T : class
        {
            try
            {
                return read();
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
