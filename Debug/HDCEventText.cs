using System.Globalization;
using System.Text;

namespace HDC.Ads.DebugUI
{
    /// <summary>How the panel writes ad events: the channel each belongs to, one line each, and what it means.</summary>
    internal static class HDCEventText
    {
        internal const string AllOption = "All";

        internal static readonly string[] Channels = { "AL", "AR", "RW", "FA", "BN", "MREC", "PU", "SDK" };

        internal static readonly string[] Types =
        {
            HDCAdEventType.Loaded, HDCAdEventType.LoadFailed, HDCAdEventType.Shown, HDCAdEventType.ShowFailed,
            HDCAdEventType.Impression, HDCAdEventType.Clicked, HDCAdEventType.Paid, HDCAdEventType.Rewarded,
            HDCAdEventType.Closed, HDCAdEventType.Initialized,
        };

        /// <summary>
        /// The channel of an event's ad, from the instance ids the channels load with. An app launch that shows a
        /// force ad group counts as FA.
        /// </summary>
        internal static string Channel(HDCAdEvent adEvent)
        {
            if (adEvent.format == HDCAdFormat.Sdk)
                return "SDK";
            string id = adEvent.id ?? string.Empty;
            if (id.StartsWith("fa_"))
                return "FA";
            if (id.StartsWith("rw_"))
                return "RW";
            if (id.StartsWith("ao_"))
                return "AL";
            if (id == HDCAppResumeAds.DebugInstanceId)
                return "AR";
            if (id.StartsWith("bn_"))
                return "BN";
            if (id.StartsWith("mrec_"))
                return "MREC";
            return id.StartsWith("pu_") ? "PU" : "OTHER";
        }

        internal static bool Matches(HDCAdEvent adEvent, string channel, string type) =>
            (channel == AllOption || Channel(adEvent) == channel) && (type == AllOption || adEvent.type == type);

        /// <summary>One event: time, type, channel, instance id and its detail, then optionally what it means.</summary>
        internal static string Line(HDCTrackedEvent tracked, bool withChannel, bool explain)
        {
            HDCAdEvent adEvent = tracked.Event;
            var text = new StringBuilder()
                .Append(HDCDebugStyle.Colored(HDCDebugStyle.MutedHex, tracked.Clock.ToString("HH:mm:ss", CultureInfo.InvariantCulture))).Append("   ")
                .Append("<color=").Append(TypeHex(adEvent.type)).Append("><b>").Append(adEvent.type).Append("</b></color>   ");
            if (withChannel)
                text.Append(HDCDebugStyle.Colored(HDCDebugStyle.AccentHex, Channel(adEvent))).Append("   ");
            text.Append(string.IsNullOrEmpty(adEvent.id) ? "-" : adEvent.id);

            string detail = Detail(adEvent);
            if (detail.Length > 0)
                text.Append('\n').Append("          ").Append(detail);
            if (explain)
                text.Append('\n').Append("          ").Append(HDCDebugStyle.Colored(HDCDebugStyle.MutedHex, Explain(adEvent)));
            return text.ToString();
        }

        internal static string TypeHex(string type)
        {
            switch (type)
            {
                case HDCAdEventType.LoadFailed:
                case HDCAdEventType.ShowFailed:
                    return HDCDebugStyle.BadHex;
                case HDCAdEventType.Loaded:
                case HDCAdEventType.Rewarded:
                    return HDCDebugStyle.GoodHex;
                case HDCAdEventType.Shown:
                case HDCAdEventType.Impression:
                    return HDCDebugStyle.InfoHex;
                case HDCAdEventType.Paid:
                    return HDCDebugStyle.WarnHex;
                default:
                    return "#E2E8F0";
            }
        }

        /// <summary>What an event means, in Vietnamese.</summary>
        internal static string Explain(HDCAdEvent adEvent)
        {
            switch (adEvent.type)
            {
                case HDCAdEventType.Initialized:
                    return "SDK quảng cáo đã khởi tạo xong; các kênh bắt đầu load.";
                case HDCAdEventType.Loaded:
                    return "Đã load xong quảng cáo" + Source(adEvent) + ", sẵn sàng hiển thị.";
                case HDCAdEventType.LoadFailed:
                    return "Load lỗi " + HDCAdErrorGuide.Title(Error(adEvent), false) + ": " + Meaning(adEvent, false);
                case HDCAdEventType.Shown:
                    return "Quảng cáo đã hiện lên màn hình.";
                case HDCAdEventType.ShowFailed:
                    return "Không hiện được quảng cáo " + HDCAdErrorGuide.Title(Error(adEvent), true) + ": " + Meaning(adEvent, true);
                case HDCAdEventType.Impression:
                    return "Ghi nhận một lượt hiển thị (impression).";
                case HDCAdEventType.Clicked:
                    return "Người chơi đã bấm vào quảng cáo.";
                case HDCAdEventType.Paid:
                    return "Quảng cáo trả doanh thu " + Revenue(adEvent) + " (precision " + adEvent.precision + ").";
                case HDCAdEventType.Rewarded:
                    return "Người chơi đủ điều kiện nhận thưởng: " + Reward(adEvent) + ".";
                case HDCAdEventType.Closed:
                    return adEvent.format == HDCAdFormat.Banner || adEvent.format == HDCAdFormat.BannerView || adEvent.format == HDCAdFormat.Mrec
                        ? "Banner đã ẩn; quảng cáo vẫn giữ để hiện lại."
                        : "Quảng cáo đã đóng; kênh sẽ load quảng cáo mới.";
                default:
                    return "Sự kiện khác của SDK.";
            }
        }

        // The detail line: the error, the revenue, the reward or the ad source.
        private static string Detail(HDCAdEvent adEvent)
        {
            if (adEvent.type == HDCAdEventType.LoadFailed || adEvent.type == HDCAdEventType.ShowFailed)
                return HDCDebugStyle.Colored(HDCDebugStyle.BadHex,
                    HDCAdErrorGuide.Title(Error(adEvent), adEvent.type == HDCAdEventType.ShowFailed) + (string.IsNullOrEmpty(adEvent.message) ? string.Empty : " · " + adEvent.message));
            if (adEvent.type == HDCAdEventType.Paid)
                return HDCDebugStyle.Colored(HDCDebugStyle.WarnHex, Revenue(adEvent));
            if (adEvent.type == HDCAdEventType.Rewarded)
                return Reward(adEvent);
            return string.IsNullOrEmpty(adEvent.adSource) ? string.Empty : HDCDebugStyle.Colored(HDCDebugStyle.MutedHex, "Source: " + adEvent.adSource);
        }

        private static HDCAdError Error(HDCAdEvent adEvent) => new HDCAdError { Code = adEvent.code, Message = adEvent.message ?? string.Empty };

        // The first line of the error guide's explanation, without its hint.
        private static string Meaning(HDCAdEvent adEvent, bool show)
        {
            string explanation = HDCAdErrorGuide.Explain(Error(adEvent), show);
            int end = explanation.IndexOf('\n');
            return end < 0 ? explanation : explanation.Substring(0, end);
        }

        private static string Source(HDCAdEvent adEvent) => string.IsNullOrEmpty(adEvent.adSource) ? string.Empty : " từ " + adEvent.adSource;

        private static string Revenue(HDCAdEvent adEvent) =>
            adEvent.Revenue.ToString("0.######", CultureInfo.InvariantCulture) + " " + (string.IsNullOrEmpty(adEvent.currency) ? "USD" : adEvent.currency);

        private static string Reward(HDCAdEvent adEvent) =>
            adEvent.rewardAmount.ToString("0.##", CultureInfo.InvariantCulture) + " " + adEvent.rewardType;
    }
}
