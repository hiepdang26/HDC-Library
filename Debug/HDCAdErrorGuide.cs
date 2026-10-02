using System;
using System.Collections.Generic;
using HDC.Ads.Diagnostics;

namespace HDC.Ads.DebugUI
{
    internal static class HDCAdErrorGuide
    {
        internal const int NoCode = -1;

        private sealed class Entry
        {
            internal Entry(string name, string meaning, string hint)
            {
                Name = name;
                Meaning = meaning;
                Hint = hint;
            }

            internal string Name { get; }
            internal string Meaning { get; }
            internal string Hint { get; }
        }

        private const string RetryHint = "Thường chỉ tạm thời; HDC tự load lại theo lịch retry. Nếu lặp lại liên tục, xem log của SDK trên máy.";
        private const string UnitHint = "Kiểm tra ad unit ID trong ad core config (Remote Config): đúng app, đúng loại quảng cáo.";
        private const string FillHint = "Không phải lỗi code. Hay gặp với ad unit mới (cần vài giờ), traffic thấp, máy bị giới hạn quảng cáo, hoặc chưa mạng nào trả giá. Dùng test device hoặc ad unit test để kiểm tra luồng.";
        private const string NetworkHint = "Kiểm tra kết nối mạng của máy, VPN, hoặc DNS chặn quảng cáo.";
        private const string MediationHint = "Xem mediation group của ad unit trên AdMob, adapter và cấu hình của từng mạng (Meta, Pangle...).";

        private static readonly Dictionary<int, Entry> AndroidLoad = new Dictionary<int, Entry>
        {
            { 0, new Entry("INTERNAL_ERROR", "Lỗi nội bộ: SDK gặp sự cố hoặc server quảng cáo trả về phản hồi không hợp lệ.", RetryHint) },
            { 1, new Entry("INVALID_REQUEST", "Request không hợp lệ: ad unit ID sai, không đúng loại quảng cáo (ví dụ unit banner dùng cho native), hoặc app chưa được AdMob duyệt.", UnitHint) },
            { 2, new Entry("NETWORK_ERROR", "Không kết nối được tới server quảng cáo.", NetworkHint) },
            { 3, new Entry("NO_FILL", "Request thành công nhưng không có quảng cáo nào phù hợp để trả về (thiếu inventory).", FillHint) },
            { 8, new Entry("APP_ID_MISSING", "Thiếu App ID của AdMob nên request không được gửi.", "Điền Android App ID trong Assets > Google Mobile Ads > Settings rồi build lại.") },
            { 9, new Entry("MEDIATION_NO_FILL", "Mọi mạng trong mediation đều không có quảng cáo để trả về.", MediationHint) },
            { 10, new Entry("REQUEST_ID_MISMATCH", "ID request trong ad string không khớp.", "Chỉ gặp khi load bằng ad string; báo lại nếu lặp lại.") },
            { 11, new Entry("INVALID_AD_STRING", "Ad string không hợp lệ.", "Chỉ gặp khi load bằng ad string; báo lại nếu lặp lại.") },
        };

        private static readonly Dictionary<int, Entry> AndroidShow = new Dictionary<int, Entry>
        {
            { 0, new Entry("INTERNAL_ERROR", "Lỗi nội bộ của SDK khi hiển thị quảng cáo.", RetryHint) },
            { 1, new Entry("AD_REUSED", "Quảng cáo này đã hiển thị rồi; mỗi quảng cáo chỉ show được một lần.", "HDC tự load quảng cáo mới sau mỗi lần show; nếu lặp lại, kiểm tra chỗ gọi show hai lần liền.") },
            { 2, new Entry("NOT_READY", "Quảng cáo chưa sẵn sàng để hiển thị.", "Chờ trạng thái READY rồi mới show.") },
            { 3, new Entry("APP_NOT_FOREGROUND", "App không ở foreground lúc gọi show.", "Chỉ gọi show khi app đang hiện trên màn hình.") },
            { 4, new Entry("MEDIATION_SHOW_ERROR", "Adapter của mạng mediation không hiển thị được quảng cáo.", "Xem thông điệp để biết mạng nào lỗi; kiểm tra adapter của mạng đó.") },
        };

        private static readonly Dictionary<int, Entry> IosLoad = new Dictionary<int, Entry>
        {
            { 0, new Entry("InvalidRequest", "Request không hợp lệ: ad unit ID sai hoặc không đúng loại quảng cáo.", UnitHint) },
            { 1, new Entry("NoFill", "Request thành công nhưng không có quảng cáo nào phù hợp để trả về (thiếu inventory).", FillHint) },
            { 2, new Entry("NetworkError", "Không kết nối được tới server quảng cáo.", NetworkHint) },
            { 3, new Entry("ServerError", "Server quảng cáo trả về lỗi.", RetryHint) },
            { 4, new Entry("OSVersionTooLow", "Phiên bản iOS của máy thấp hơn mức SDK hỗ trợ.", "Thử trên máy iOS mới hơn.") },
            { 5, new Entry("Timeout", "Request quá thời gian chờ.", "Mạng chậm hoặc mediation chờ quá lâu; HDC sẽ load lại.") },
            { 7, new Entry("MediationDataError", "Dữ liệu mediation trả về không hợp lệ.", MediationHint) },
            { 8, new Entry("MediationAdapterError", "Adapter của một mạng mediation gặp lỗi.", "Kiểm tra adapter (pod) của mạng đó: đúng phiên bản, đã cài và khởi tạo.") },
            { 9, new Entry("MediationNoFill", "Mọi mạng trong mediation đều không có quảng cáo để trả về.", MediationHint) },
            { 10, new Entry("MediationInvalidAdSize", "Kích thước quảng cáo không hợp lệ với mạng mediation.", MediationHint) },
            { 11, new Entry("InternalError", "Lỗi nội bộ của SDK.", RetryHint) },
            { 12, new Entry("InvalidArgument", "Tham số không hợp lệ khi gọi SDK.", "Báo lại kèm thông điệp lỗi.") },
            { 13, new Entry("ReceivedInvalidResponse", "Nhận phản hồi không hợp lệ từ server quảng cáo.", RetryHint) },
            { 19, new Entry("AdAlreadyUsed", "Quảng cáo đã được dùng rồi.", "HDC tự load quảng cáo mới sau mỗi lần show.") },
            { 20, new Entry("ApplicationIdentifierMissing", "Thiếu GADApplicationIdentifier (App ID) trong Info.plist.", "Điền iOS App ID trong Assets > Google Mobile Ads > Settings rồi export lại.") },
        };

        private static readonly Dictionary<int, Entry> IosShow = new Dictionary<int, Entry>
        {
            { 15, new Entry("AdNotReady", "Quảng cáo chưa sẵn sàng để hiển thị.", "Chờ trạng thái READY rồi mới show.") },
            { 16, new Entry("AdTooLarge", "Quảng cáo lớn hơn vùng hiển thị.", "Kiểm tra kích thước view chứa quảng cáo.") },
            { 17, new Entry("Internal", "Lỗi nội bộ của SDK khi hiển thị.", RetryHint) },
            { 18, new Entry("AdAlreadyUsed", "Quảng cáo này đã hiển thị rồi.", "HDC tự load quảng cáo mới sau mỗi lần show.") },
            { 21, new Entry("NotMainThread", "Gọi show ngoài main thread.", "Báo lại: HDC luôn gọi show trên main thread.") },
            { 22, new Entry("Mediation", "Adapter của mạng mediation không hiển thị được quảng cáo.", "Xem thông điệp để biết mạng nào lỗi.") },
        };

        internal static string Title(HDCAdError error, bool show)
        {
            if (error == null)
                return string.Empty;
            if (error.Code == NoCode)
                return NoCode + " · HDC";
            Entry entry = Find(error.Code, show);
            return entry != null ? error.Code + " · " + entry.Name : error.Code + " · UNKNOWN";
        }

        internal static string Explain(HDCAdError error, bool show)
        {
            if (error == null)
                return string.Empty;
            if (error.Code == NoCode)
                return ExplainUncoded(error.Message ?? string.Empty);

            Entry entry = Find(error.Code, show);
            if (entry == null)
                return $"Mã {error.Code} chưa có trong bảng của {Platform}. Xem thông điệp lỗi bên dưới.";
            return entry.Meaning + "\nGợi ý: " + entry.Hint;
        }

        private static string Platform => IsIos ? "Google Mobile Ads iOS" : "Google Mobile Ads Android";

        private static bool IsIos
        {
            get
            {
#if UNITY_IOS && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        private static Entry Find(int code, bool show)
        {
            Dictionary<int, Entry> table = show ? (IsIos ? IosShow : AndroidShow) : (IsIos ? IosLoad : AndroidLoad);
            if (table.TryGetValue(code, out Entry entry))
                return entry;
            Dictionary<int, Entry> other = show ? (IsIos ? IosLoad : AndroidLoad) : null;
            return other != null && other.TryGetValue(code, out entry) ? entry : null;
        }

        private static string ExplainUncoded(string message)
        {
            if (Has(message, "Not ready") || Has(message, "Not loaded"))
                return "Gọi show khi quảng cáo chưa load xong.\nGợi ý: chờ trạng thái READY; HDC tự load lại sau lần show hụt.";
            if (Has(message, "Popup not displayable"))
                return "Popup chưa ở trạng thái hiển thị được: chưa load xong, chưa đặt vị trí (Move), hoặc đang hiện.\nGợi ý: đặt vùng popup (UpdatePos) rồi show khi popup READY.";
            if (Has(message, "No ad loaded"))
                return "Native không load được quảng cáo nào từ các ad unit, và không gửi kèm mã lỗi của SDK.\nGợi ý: xem log native của máy để biết lý do cụ thể.";
            if (Has(message, "already taken"))
                return "Quảng cáo preload đã bị lấy ra trước đó.\nGợi ý: chờ preloader nạp quảng cáo mới.";
            if (Has(message, "Preload failed"))
                return "Bộ preload của plugin không nạp được quảng cáo.\nGợi ý: " + RetryHint;
            if (Has(message, "No ad returned") || Has(message, "Load failed"))
                return "Plugin Google Mobile Ads không trả quảng cáo và không kèm lỗi.\nGợi ý: " + RetryHint;
            if (Has(message, "stayed busy"))
                return "Màn hình đang bận (đang có màn hình khác chuyển cảnh hoặc trình bày) nên không mở được quảng cáo.\nGợi ý: show lại khi game không mở màn hình hay popup hệ thống nào.";
            if (Has(message, "Show failed"))
                return "Native không hiển thị được quảng cáo.\nGợi ý: quảng cáo có thể đã hết hạn; HDC sẽ load lại.";
            return "Lỗi ở tầng HDC hoặc native, không có mã của SDK.\nGợi ý: đọc thông điệp lỗi bên dưới.";
        }

        private static bool Has(string message, string part) => message.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
