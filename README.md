# HDCLib

Thư viện quảng cáo cho Unity. HDCLib gọi thư viện Kotlin Multiplatform của dự án AdsMultiplatform (nhánh `migrate/HDC-Lib`) trên Android và iOS.

Toàn bộ logic SDK nằm trong KMP: load, waterfall ad unit, hiển thị, pause Unity khi quảng cáo fullscreen hiện, và bắn sự kiện. Phía Unity chỉ gọi một hàm lệnh và nhận một luồng sự kiện.

## Cấu trúc

```
Runtime/                       Assembly HDC.Ads, chỉ compile khi có define HDC_ADS
  HDCAdsSdk.cs                 API: Initialize, Load/Show/IsReady/Destroy interstitial, event AdEvent
  HDCAdEvent.cs                Dữ liệu sự kiện, HDCAdEventType, HDCAdFormat
  Internal/                    Cầu nối iOS (DllImport), Android (JNI), Editor (giả lập), main thread
Plugins/iOS/                   HDCAds.xcframework, HDCAdsBridge.mm
Plugins/Android/Repository~/   Maven repo chứa thư viện Android (Unity bỏ qua thư mục có đuôi "~")
Editor/                        Bật/tắt HDC Ads, post-process Xcode, mẫu Dependencies.xml
Demo/                          HDCAdsDemo: các nút bấm để thử quảng cáo
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
    HDCAdsSdk.LoadInterstitial("inter_main", new[] { "ca-app-pub-xxx/yyy" }));

if (HDCAdsSdk.IsInterstitialReady("inter_main"))
    HDCAdsSdk.ShowInterstitial("inter_main");
```

- Gọi API từ main thread của Unity. Sự kiện cũng luôn tới trên main thread.
- Trên iOS, sự kiện tới ngay cả khi quảng cáo fullscreen đang pause Unity.
- Trong Editor, SDK được giả lập: load mất 0,5 giây, show bắn `Shown`, `Impression`, `Paid`, rồi `Closed`.

## Cập nhật thư viện native

Chạy trong dự án AdsMultiplatform, nhánh `migrate/HDC-Lib`:

```bash
./gradlew :shared:exportUnityPlugins -PunityPluginsDir=<Unity project>/Assets/HDCLib/Plugins
```

Lệnh này build `HDCAds.xcframework` bản release (kèm Compose resources). Nó cũng publish maven repo Android `com.hdc.adsmultiplatform:shared-android` vào `Plugins/Android/Repository~`.

## Yêu cầu

- iOS 15.0 trở lên. Post-process tự nâng deployment target của project Xcode và Podfile.
- Android minSdk 24, compileSdk 36, và Android Gradle Plugin 8.6 trở lên.
  - Unity 6 đáp ứng được. Bản thử kiểu Unity 6.6 (AGP 9, Gradle 9) đã build thành công.
  - Unity 2022.3 (AGP 7.4.2, Gradle 7.5.1) không build được Android. Các thư viện Compose/androidx mà HDCAds dùng yêu cầu AGP ≥ 8.6.
- App ID của AdMob: hiện do plugin Google Mobile Ads của Unity ghi vào `Info.plist` và `AndroidManifest.xml`.

## Trạng thái

- Giai đoạn 1 (hiện tại): interstitial.
- Sẽ bổ sung ở các giai đoạn sau:
  - Native fullscreen, banner và popup.
  - Rewarded và app open.
  - Logic kênh: capping, vị trí và config từ Firebase Remote Config.
