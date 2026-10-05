# Changelog

Các thay đổi của HDCLib (package `com.hdc.ads`). Số phiên bản lấy theo `package.json`.

## Chưa phát hành (dự kiến 0.6.0)

Phiên bản này chưa gắn tag. Nó còn chờ kiểm thử trên máy Android và iOS thật.

### Thay đổi không tương thích

Game giờ chỉ thấy tầng API, namespace `HDC.Ads`, gồm:

- `HDCAds`;
- interface của 7 kênh;
- `HDCAdRevenue` và `HDCAdChannel`;
- `HDCAdFormat`, `HDCAdPosition` và `HDCBannerSlot`.

Những thứ sau đã đổi so với 0.5.0:

- `HDCAds.ForceAd`, `Rewarded`, `AppLaunch`, `AppResume`, `Banner`, `Mrec` và `Popup` giờ trả về interface (`IForceAds`…).
  - Các hàm giữ nguyên tên, nên lời gọi như `HDCAds.ForceAd.Show(...)` không phải sửa.
  - Code khai báo biến kiểu lớp cụ thể, như `HDCForceAds`, thì phải đổi sang interface.
- Các thành phần sau thành internal: `HDCAdsSdk`, các lớp kênh, `HDCAdEvent`, các lớp tùy chọn và config.
  - Công tắc test chuyển sang `HDCAds.Testing`: `DebugLog`, `EnableTestDevice`, `UseTestAdUnits` và Meta test mode.
  - Luồng sự kiện thô `HDCAdsSdk.AdEvent` không còn. Để nghe doanh thu, dùng `HDCAds.Revenue`.
- `HDCAds.Config`, `HDCAds.CoreConfig` và `HDCAds.LastFullscreenAdTime` không còn public.

### Thêm

- `HDCAds.Revenue` báo doanh thu của từng impression. Mỗi sự kiện có:
  - kênh, position và định dạng;
  - mạng, nguồn quảng cáo và ad unit;
  - giá trị, tiền tệ và precision.
- `HDCAds.Testing`, gom các công tắc test.
- Prefab `HDCAdsSetup` cho scene đầu tiên:
  - lấy config, khởi tạo ads, chạy app launch rồi mở scene tiếp theo;
  - có tùy chọn Google test device và Google test ad units.
- Menu `HDC`, cùng cửa sổ sửa config mặc định có kiểm tra JSON.
- Bảng debug `HDCAdsDebugPanel`, gồm bốn trang:
  - Ads: trạng thái từng ad unit, cùng mã lỗi của SDK kèm giải thích.
  - Remote Config: kiểm tra config và cho biết config đang áp dụng lấy từ nguồn nào.
  - Events: các sự kiện quảng cáo.
  - Device: có thẻ Mediation, cho biết trạng thái từng adapter và Meta test mode.
- Ad unit test của Google cho mọi định dạng (`UseTestAdUnits`).
- Prefab `HDCAdjust` (assembly `HDC.Ads.Adjust`), thay cho prefab Adjust của SDK:
  - Khởi động Adjust bằng cài đặt trên inspector, đủ các trường của prefab SDK. App token riêng cho Android và iOS. Môi trường `Auto` chạy Sandbox ở bản Development và Production ở bản release.
  - Tự gửi doanh thu của mọi quảng cáo HDC lên Adjust (source `admob_sdk`, network là nguồn mediation, unit là ad unit, placement là position), giống hệ thống cũ.
  - Đọc attribution, lưu vào PlayerPrefs `user_network`, `user_campaign`, `user_creative`, `user_cost`, và đưa cho game qua `HDCAdjust.Attribution`, `HDCAdjust.Network` (`time_out` sau 7 giây chờ) và sự kiện `HDCAdjust.AttributionChanged`.
  - `HDCAdjust.TrackPurchaseRevenue` gửi doanh thu mua hàng bằng event token của từng nền tảng.
  - Android: chuyển deep link mở app cho Adjust, như prefab của SDK.
  - Menu `HDC > Adjust > Add to open scene`. Define `HDC_ADJUST` tự bật khi project có Adjust SDK 5 và tự tắt khi SDK bị xoá.
  - Trang Device của bảng debug cho biết trạng thái của HDCAdjust.
- Trang Device của bảng debug có thêm:
  - Nút `Test Ad Units: On/Off`, bật tắt ad unit test của Google. Lựa chọn được lưu trên máy và áp dụng từ lúc mở app.
  - Nút `Restart App`, hiện ra sau khi đổi lựa chọn trên. Nó mở lại app trên Android và vào lại Play Mode trong Editor. Trên iOS nút thành `Quit App`, vì iOS không cho app tự mở lại.
  - Nút `Clear Data & Restart`, phải bấm hai lần. Nó xoá toàn bộ dữ liệu của app (PlayerPrefs, file, cache, dữ liệu SDK) nhưng giữ công tắc Test Ad Units, rồi mở lại app như lần cài đầu: trên Android tự mở lại, trong Editor vào lại Play Mode. Trên iOS nút thành `Clear Data & Quit`: xoá xong thì thoát, phải mở lại app bằng tay.
- Bộ test Edit Mode và Play Mode, kèm:
  - test hợp đồng API và test luật tầng;
  - script `Tools~/compile-matrix.sh` và `Tools~/run-tests.sh`.
- `ARCHITECTURE.md`, mô tả các tầng và luật phụ thuộc, kèm hướng dẫn thêm mạng, partner, kênh và nút debug.

### Thay đổi

- Mã nguồn chia thành các tầng Api, Logic, Domain, Diagnostics, Ports, Infrastructure và Composition.
  - Mạng quảng cáo (AdMob và native) là adapter sau `IAdNetwork`.
  - Partner mediation nằm sau `IMediationPartner`.
  - Mỗi kênh mang module debug và luật kiểm config của riêng nó.
- Khi SDK sẵn sàng, các kênh khởi động theo thứ tự AL, AR, RW, FA, BN, MREC, PU. Trước đây FA khởi động trước RW.
- Bảng debug bỏ tab CL (collapsible banner), vì HDCLib chưa có kênh này.
- Bảng debug không còn hỏi phía native mỗi giây.
- Mã nguồn C# của HDCLib không còn comment. Tài liệu nằm trong `README.md`, `ARCHITECTURE.md` và `CHANGELOG.md`.

### Sửa

- Android (thư viện `hdc-ads-android` 0.3.2):
  - Lệnh show của native full-screen và popup trả lời ngay sau khi kiểm tra ad sẵn sàng, không chờ ad hiện xong. Trước đây, trên máy chậm, việc hiện ad kéo dài quá 2 giây thì lệnh trả về "không hiện" trong khi ad vẫn hiện. Khi đó `ForceAd.Show` trả false, `onDone` chạy ngay, và capping không tính lượt đó.
  - Show không thực hiện được (ví dụ không còn ad đã load) giờ báo ShowFailed, thay vì LoadFailed.
- Popup hiện đúng layout mà config ghi.
  - Trước đây, tên cũ `mrec_single_manual_NN` (Remote Config đang dùng `mrec_single_manual_13`) không được nhận ra. Thư viện native âm thầm dùng layout mặc định `popup_single_manual_01`, có media lớn.
  - Giờ tên cũ hiện layout `popup_single_manual_NN` tương ứng, giống hệ thống cũ. Tên fullscreen có đuôi `_left`/`_right` cũng hiện layout gốc.
  - Tên layout không tồn tại báo lỗi ở Config Check. Dòng `Layout` của popup trên bảng debug cho biết layout đang dùng.
- Popup hiện lại được sau khi Hide hoặc sau khi bị đóng (thư viện `hdc-ads-android` 0.3.3 và framework iOS build lại):
  - Trước đây `Hide` thực chất là đóng: quảng cáo bị bỏ, và HDCLib không bao giờ load quảng cáo mới cho group đó. Mọi lần `Show` sau đều báo ShowFailed "Popup not displayable: Closed" tới khi mở lại app.
  - Giờ `Hide` chỉ ẩn popup. `Show` hiện lại đúng quảng cáo đó, không load thêm.
  - Quảng cáo bị đóng hẳn thì `Show` hoặc `Initialize` load quảng cáo mới; `Show` hiện ngay khi load xong.
  - Show bị chặn tạm thời (ví dụ màn hình đang chuyển cảnh) không còn làm mất quảng cáo đã load.
  - `Hide` trước khi popup hiện không còn làm mất quảng cáo đã load.
  - Thống kê Loaded và Shows của popup không còn bị đếm đôi cho một lần hiện.
- App launch kết thúc ngay nếu không có quảng cáo. Group không có ad unit nào không còn gây lỗi.
- iOS: view quảng cáo chạy được ở tần số khung hình cao, nên Compose không còn abort (`CADisableMinimumFrameDurationOnPhone`).
- iOS: không đưa framework KMP bị xung đột vào bản build.

## 0.5.0 — 2026-09-30

Đây là phiên bản đầu tiên có `package.json`. Nó gồm:

- Interstitial qua thư viện KMP.
- Native full-screen, popup và native banner, cùng Meta test mode.
- Qua plugin Google Mobile Ads:
  - rewarded, app open, banner view (6 vị trí) và MREC;
  - thử load lại khi load lỗi;
  - test device.
- Tầng kênh `HDCAds`, chạy theo config lấy từ Firebase Remote Config (schema cũ).
- Hỗ trợ Unity từ 2021.3 tới 6000.6:
  - thư viện Android `hdc-ads-android` không dùng Compose;
  - post-process tự thêm framework và bridge vào project Xcode;
  - cài được như package UPM;
  - chạy được khi tắt domain reload.
