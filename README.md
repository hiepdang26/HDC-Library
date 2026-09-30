# HDCLib

Thư viện quảng cáo cho Unity. HDCLib gọi thư viện Kotlin Multiplatform của dự án AdsMultiplatform (nhánh `migrate/HDC-Lib`) trên Android và iOS.

Toàn bộ logic SDK nằm trong KMP: load, waterfall ad unit, hiển thị, pause Unity khi quảng cáo fullscreen hiện, và bắn sự kiện. Phía Unity chỉ gọi một hàm lệnh và nhận một luồng sự kiện.

## Cấu trúc

```
Runtime/                       Assembly HDC.Ads, chỉ compile khi có define HDC_ADS
  HDCAdsSdk.cs                 API theo định dạng: interstitial, fullscreen, popup, banner, Meta test
  HDCAdOptions.cs              Tuỳ chọn fullscreen, popup, banner (tên field giống JSON config)
  HDCAdEvent.cs                Dữ liệu sự kiện, HDCAdEventType, HDCAdFormat
  Internal/                    Cầu nối iOS (DllImport), Android (JNI), Editor (giả lập), main thread
Plugins/iOS/                   HDCAds.xcframework, HDCAdsBridge.mm
Plugins/Android/Repository~/   Maven repo chứa thư viện Android (Unity bỏ qua thư mục có đuôi "~")
Editor/                        Bật/tắt HDC Ads, post-process Xcode, mẫu Dependencies.xml
Demo/                          HDCAdsDemo: các nút bấm để thử từng định dạng
```

## Bật / tắt

Mặc định HDCLib ở trạng thái **tắt**, và không có gì của HDCLib vào bản build.

- `Tools > HDC Ads > Enable`:
  - Thêm define `HDC_ADS` cho Android, iOS và Standalone.
  - Bật plugin iOS.
  - Tạo `Editor/HDCAdsDependencies.xml` cho External Dependency Manager.
- `Tools > HDC Ads > Disable` hoàn tác các bước trên.

Chỉ bật khi trong project không còn bản framework nào khác build từ cùng dự án KMP. Hai static framework Kotlin/Native (đều chứa Compose và Skia) không link chung được vào một app iOS.

## Sử dụng

```csharp
using HDC.Ads;

HDCAdsSdk.AdEvent += adEvent => Debug.Log(adEvent);
HDCAdsSdk.Initialize(() =>
{
    HDCAdsSdk.LoadInterstitial("inter_main", new[] { "ca-app-pub-xxx/yyy" });
    HDCAdsSdk.LoadFullscreen("fs_main", new[] { "ca-app-pub-xxx/zzz" });
    HDCAdsSdk.LoadPopup("popup_main", new[] { "ca-app-pub-xxx/zzz" }, new HDCPopupOptions { x = 0.5f, y = 0.5f });
    HDCAdsSdk.LoadBanner("banner_main", new[] { "ca-app-pub-xxx/zzz" });
});

if (HDCAdsSdk.IsFullscreenReady("fs_main"))
    HDCAdsSdk.ShowFullscreen("fs_main", new HDCFullscreenOptions { layoutNames = new[] { "fs_single_cls_01" } });

HDCAdsSdk.ShowBanner("banner_main");
```

- Gọi API từ main thread của Unity. Sự kiện cũng luôn tới trên main thread.
- Mỗi sự kiện có `id`, `format` và `type`:
  - `Loaded`, `LoadFailed`: `LoadFailed` chỉ báo khi đã thử hết các ad unit.
  - `Shown`, `ShowFailed`, `Closed`.
  - `Impression`, `Clicked`.
  - `Paid`: có `valueMicros`, `currency`, `precision`, `adSource`.
- Trên iOS, sự kiện tới ngay cả khi quảng cáo fullscreen đang pause Unity. Trên Android, sự kiện phát ra trong lúc quảng cáo fullscreen che game sẽ tới khi Unity chạy lại.
- Trong Editor, SDK được giả lập:
  - Load mất 0,5 giây.
  - Show bắn `Shown`, `Impression`, `Paid`.
  - Interstitial và fullscreen đóng sau 1 giây, popup sau 3 giây. Banner giữ đến khi ẩn.

## Cập nhật thư viện native

Chạy trong dự án AdsMultiplatform, nhánh `migrate/HDC-Lib`:

```bash
./gradlew :shared:exportUnityPlugins -PunityPluginsDir=<Unity project>/Assets/HDCLib/Plugins
```

Lệnh này build `HDCAds.xcframework` bản release (kèm Compose resources). Nó cũng publish maven repo Android `com.hdc.adsmultiplatform:shared-android` vào `Plugins/Android/Repository~`. Khi đổi version thư viện Android, sửa cả `Editor/Templates~/HDCAdsDependencies.xml`.

## Yêu cầu

- Plugin Google Mobile Ads của Unity vẫn được giữ trong project. Plugin này ghi App ID của AdMob vào `Info.plist` và `AndroidManifest.xml`.
- GMA Android 25.4.0 (khớp với plugin), GMA iOS 13.x, Meta adapter 6.22.0.0.
- iOS 15.0 trở lên. Post-process tự nâng deployment target của project Xcode và Podfile.
- Android minSdk 24, compileSdk 36, và Android Gradle Plugin 8.6 trở lên.
  - Unity 6 đáp ứng được. Bản thử kiểu Unity 6.6 (AGP 9, Gradle 9) đã build thành công.
  - Unity 2022.3 (AGP 7.4.2, Gradle 7.5.1) không build được Android. Các thư viện Compose/androidx mà HDCAds dùng yêu cầu AGP ≥ 8.6.

## Trạng thái

- Giai đoạn 1: interstitial.
- Giai đoạn 2:
  - Native fullscreen, popup và banner trên cả hai nền tảng.
  - Meta test mode.
  - Sự kiện impression/click/paid chung cho mọi định dạng.
- Sẽ bổ sung ở giai đoạn sau:
  - Các định dạng của plugin Google Mobile Ads: rewarded, app open, banner thường, MREC.
  - Logic kênh: capping, vị trí và config từ Firebase Remote Config.
