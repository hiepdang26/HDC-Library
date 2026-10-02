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

### Sửa

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
