using System.Collections.Generic;
using System.Text.RegularExpressions;
using HDC.Ads.Diagnostics;
using UnityEngine;
using UnityEngine.UI;

namespace HDC.Ads.DebugUI
{
    /// <summary>The panel's colors, and small helpers its pages share.</summary>
    internal static class HDCDebugStyle
    {
        internal static readonly Color TextColor = new Color(0.945f, 0.961f, 0.976f, 1f);
        internal static readonly Color MutedColor = new Color(0.58f, 0.639f, 0.722f, 1f);
        internal static readonly Color GoodColor = new Color(0.29f, 0.871f, 0.502f, 1f);
        internal static readonly Color WarnColor = new Color(0.984f, 0.749f, 0.141f, 1f);
        internal static readonly Color BadColor = new Color(0.973f, 0.443f, 0.443f, 1f);
        internal static readonly Color InfoColor = new Color(0.376f, 0.647f, 0.98f, 1f);

        internal static readonly Color ButtonColor = new Color(0.2f, 0.255f, 0.333f, 1f);
        internal static readonly Color SelectedColor = new Color(0.31f, 0.275f, 0.898f, 1f);
        internal static readonly Color PrimaryColor = new Color(0.31f, 0.275f, 0.898f, 1f);

        // Badge colors of the ad states.
        internal static readonly Color IdleBadge = new Color(0.278f, 0.333f, 0.412f, 1f);
        internal static readonly Color BusyBadge = new Color(0.851f, 0.467f, 0.024f, 1f);
        internal static readonly Color GoodBadge = new Color(0.086f, 0.639f, 0.290f, 1f);
        internal static readonly Color LiveBadge = new Color(0.145f, 0.388f, 0.922f, 1f);
        internal static readonly Color BadBadge = new Color(0.863f, 0.149f, 0.149f, 1f);

        internal const string MutedHex = "#94A3B8";
        internal const string GoodHex = "#4ADE80";
        internal const string WarnHex = "#FBBF24";
        internal const string BadHex = "#F87171";
        internal const string InfoHex = "#60A5FA";
        internal const string AccentHex = "#A5B4FC";

        private static readonly Regex Tags = new Regex("<.*?>");

        internal static Color ToneColor(HDCDebugTone tone)
        {
            switch (tone)
            {
                case HDCDebugTone.Muted: return MutedColor;
                case HDCDebugTone.Good: return GoodColor;
                case HDCDebugTone.Warn: return WarnColor;
                case HDCDebugTone.Bad: return BadColor;
                default: return TextColor;
            }
        }

        internal static Color BadgeColor(HDCUnitTone tone)
        {
            switch (tone)
            {
                case HDCUnitTone.Busy: return BusyBadge;
                case HDCUnitTone.Good: return GoodBadge;
                case HDCUnitTone.Live: return LiveBadge;
                case HDCUnitTone.Bad: return BadBadge;
                default: return IdleBadge;
            }
        }

        /// <summary>Fills a list from a channel's or group's state: a header per section, a row per value.</summary>
        internal static void Fill(HDCKeyValueList list, HDCDebugInfo info)
        {
            foreach (HDCDebugInfo.Part part in info.Parts)
            {
                if (!string.IsNullOrEmpty(part.Name))
                    list.Header(part.Name);
                foreach (HDCDebugInfo.Row row in part.Rows)
                    list.Row(row.Label, row.Value, ToneColor(row.Tone));
            }
        }

        internal static string Colored(string hex, string text) => "<color=" + hex + ">" + text + "</color>";

        internal static string StripTags(string text) => Tags.Replace(text ?? string.Empty, string.Empty);

        internal static void SetLabel(Button button, string label) => button.GetComponentInChildren<Text>(true).text = label;

        internal static void SetVisible(Component component, bool visible)
        {
            if (component != null && component.gameObject.activeSelf != visible)
                component.gameObject.SetActive(visible);
        }

        internal static void Highlight(Button button, bool selected) => button.image.color = selected ? SelectedColor : ButtonColor;

        /// <summary>Takes the next pooled copy of <paramref name="template"/>, making one when the pool runs out.</summary>
        internal static GameObject Take(List<GameObject> pool, GameObject template, ref int used)
        {
            if (pool.Count <= used)
            {
                GameObject made = Object.Instantiate(template, template.transform.parent, false);
                made.name = template.name.Replace(" Template", string.Empty) + " " + pool.Count;
                pool.Add(made);
            }

            GameObject picked = pool[used++];
            if (!picked.activeSelf)
                picked.SetActive(true);
            picked.transform.SetAsLastSibling();
            return picked;
        }

        /// <summary>Hides the pooled copies past the first <paramref name="used"/>.</summary>
        internal static void HideRest(List<GameObject> pool, int used)
        {
            for (int i = used; i < pool.Count; i++)
            {
                if (pool[i].activeSelf)
                    pool[i].SetActive(false);
            }
        }
    }
}
