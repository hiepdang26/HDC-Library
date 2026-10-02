using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace HDC.Ads.Domain
{
    internal static class HDCAdLayouts
    {
        internal const string PopupDefault = "popup_single_manual_01";
        internal const string FullscreenDefault = "fs_single_universal_01";

        private const string PopupPrefix = "popup_single_manual_";
        private const string OldPopupPrefix = "mrec_single_manual_";
        private const int PopupCount = 15;

        private static readonly HashSet<string> FullscreenNames = new HashSet<string>(
            Numbered("fs_single_universal_", 12)
                .Concat(Numbered("fs_single_transparent_", 7))
                .Concat(Numbered("fs_single_cls_", 11))
                .Concat(Numbered("fs_single_nav_", 3))
                .Concat(Numbered("fs_single_prgso_", 4))
                .Concat(new[]
                {
                    "fs_single_prgs_01", "fs_single_prgs_cls_01", "fs_single_loop_01", "fs_single_ctr_01",
                    "fs_single_ctr_transparent_01", "fs_multi_01", "fs_sequence_01",
                }));

        private static readonly Dictionary<string, string> FullscreenSides = new Dictionary<string, string>
        {
            { "fs_single_cls_03_left", "fs_single_cls_03" },
            { "fs_single_cls_03_right", "fs_single_cls_03" },
            { "fs_single_nav_01_left", "fs_single_nav_01" },
            { "fs_single_nav_01_right", "fs_single_nav_01" },
            { "fs_single_nav_02_left", "fs_single_nav_02" },
            { "fs_single_nav_02_right", "fs_single_nav_02" },
            { "fs_single_nav_03_left", "fs_single_nav_03" },
            { "fs_single_nav_03_right", "fs_single_nav_03" },
        };

        internal static string Popup(string configured) => PopupOrNull(configured) ?? PopupDefault;

        internal static bool IsPopup(string configured) => PopupOrNull(configured) != null;

        internal static string Fullscreen(string configured) => FullscreenOrNull(configured) ?? FullscreenDefault;

        internal static bool IsFullscreen(string configured) => FullscreenOrNull(configured) != null;

        private static string PopupOrNull(string configured)
        {
            string name = Clean(configured);
            if (IsNumbered(name, PopupPrefix, PopupCount))
                return name;
            if (IsNumbered(name, OldPopupPrefix, PopupCount))
                return PopupPrefix + name.Substring(OldPopupPrefix.Length);
            return null;
        }

        private static string FullscreenOrNull(string configured)
        {
            string name = Clean(configured);
            if (FullscreenNames.Contains(name))
                return name;
            return FullscreenSides.TryGetValue(name, out string side) ? side : null;
        }

        private static string Clean(string name) => name?.Trim().ToLowerInvariant() ?? string.Empty;

        private static bool IsNumbered(string name, string prefix, int count)
        {
            if (name.Length != prefix.Length + 2 || !name.StartsWith(prefix, StringComparison.Ordinal))
                return false;
            return int.TryParse(name.Substring(prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out int index)
                && index >= 1 && index <= count;
        }

        private static IEnumerable<string> Numbered(string prefix, int count) =>
            Enumerable.Range(1, count).Select(index => prefix + index.ToString("00", CultureInfo.InvariantCulture));
    }
}
