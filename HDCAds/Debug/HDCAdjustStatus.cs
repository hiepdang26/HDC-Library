using System.Collections.Generic;
using UnityEngine;

namespace HDC.Ads.DebugUI
{
    internal static class HDCAdjustStatus
    {
#if HDC_ADJUST
        internal static readonly bool HasSdk = true;
#else
        internal static readonly bool HasSdk = false;
#endif

        private static bool Running =>
            HDCAdjust.StartedBy == HDCAdjustStart.ByHdc || HDCAdjust.StartedBy == HDCAdjustStart.ByGame
            || HDCAdjust.StartedBy == HDCAdjustStart.BySdkPrefab;

        internal static IEnumerable<(string Label, string Value, Color Color)> Rows()
        {
            yield return ("HDCAdjust", StartText(), StartColor());
            if (Running)
                yield return ("HDCAdjust.Network", NetworkText(), NetworkColor());
            if (!HDCAdjust.InScene)
                yield break;
            yield return ("Ad Revenue", RevenueText(), RevenueColor());
            if (HDCAdjustRevenue.Last.HasValue)
                yield return ("Last Ad Revenue", HDCAdjustRevenue.Last.Value.ToString(), HDCDebugStyle.TextColor);
            yield return ("Purchase Token", HDCAdjust.HasPurchaseEventToken ? "Đã đặt" : "Trống: TrackPurchaseRevenue không gửi",
                HDCAdjust.HasPurchaseEventToken ? HDCDebugStyle.TextColor : HDCDebugStyle.MutedColor);
        }

        private static string StartText()
        {
            switch (HDCAdjust.StartedBy)
            {
                case HDCAdjustStart.ByHdc:
                    return "HDCAdjust đã khởi động Adjust (" + (HDCAdjustSdk.UsedSandbox ? "Sandbox" : "Production") + ")";
                case HDCAdjustStart.ByGame:
                    return "Game tự khởi động Adjust (Start Manually)";
                case HDCAdjustStart.BySdkPrefab:
                    return "Prefab Adjust của SDK khởi động Adjust, cài đặt của HDCAdjust bị bỏ qua";
                case HDCAdjustStart.NoAppToken:
                    return "App token trống: Adjust chưa khởi động";
                case HDCAdjustStart.NoSdk:
                    return "Không có Adjust SDK trong project";
                case HDCAdjustStart.Unsupported:
                    return "Adjust chỉ chạy trên Android và iOS";
                case HDCAdjustStart.Editor:
                    return "Editor: Adjust chỉ chạy trên máy Android và iOS";
                default:
                    return HasSdk
                        ? "Chưa có prefab HDCAdjust trong scene đầu: Adjust không khởi động, doanh thu quảng cáo không gửi"
                        : "Không dùng: project không có Adjust SDK";
            }
        }

        private static Color StartColor()
        {
            switch (HDCAdjust.StartedBy)
            {
                case HDCAdjustStart.ByHdc:
                    return HDCAdjustSdk.UsedSandbox && !Debug.isDebugBuild ? HDCDebugStyle.WarnColor : HDCDebugStyle.GoodColor;
                case HDCAdjustStart.ByGame:
                    return HDCDebugStyle.GoodColor;
                case HDCAdjustStart.BySdkPrefab:
                    return HDCDebugStyle.WarnColor;
                case HDCAdjustStart.NoAppToken:
                    return HDCDebugStyle.BadColor;
                case HDCAdjustStart.NotInScene:
                    return HasSdk ? HDCDebugStyle.WarnColor : HDCDebugStyle.MutedColor;
                default:
                    return HDCDebugStyle.MutedColor;
            }
        }

        private static string NetworkText()
        {
            if (HDCAdjust.IsAttributionReady)
                return HDCAdjust.Network.Length > 0 ? HDCAdjust.Network : "(trống)";
            return HDCAdjustAttributions.TimedOut ? HDCAdjust.TimedOutNetwork + ": chưa có attribution" : "Đang chờ attribution";
        }

        private static Color NetworkColor() =>
            HDCAdjust.IsAttributionReady ? HDCDebugStyle.GoodColor : HDCAdjustAttributions.TimedOut ? HDCDebugStyle.WarnColor : HDCDebugStyle.MutedColor;

        private static string RevenueText()
        {
            if (!HDCAdjust.SendsAdRevenue)
                return "Tắt (Send Ad Revenue)";
            if (HDCAdjust.StartedBy == HDCAdjustStart.NoAppToken)
                return "Không gửi: Adjust chưa khởi động";
            if (!Running)
                return HDCAdjustRevenue.Count + " lần, không gửi ngoài máy Android và iOS";
            return HDCAdjustRevenue.Count + " lần đã gửi";
        }

        private static Color RevenueColor()
        {
            if (!HDCAdjust.SendsAdRevenue || (!Running && HDCAdjust.StartedBy != HDCAdjustStart.NoAppToken))
                return HDCDebugStyle.MutedColor;
            if (HDCAdjust.StartedBy == HDCAdjustStart.NoAppToken)
                return HDCDebugStyle.BadColor;
            return HDCAdjustRevenue.Count > 0 ? HDCDebugStyle.GoodColor : HDCDebugStyle.TextColor;
        }
    }
}
