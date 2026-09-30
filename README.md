# HDCLib

Thư viện quảng cáo cho Unity. HDCLib gọi thư viện Kotlin Multiplatform của dự án AdsMultiplatform (nhánh `migrate/HDC-Lib`) trên Android và iOS.

Có hai nguồn quảng cáo:

- **Thư viện KMP** (native): interstitial, native fullscreen, popup, native banner. Logic load, waterfall ad unit, hiển thị, pause Unity và bắn sự kiện nằm trong KMP. Phía Unity chỉ gọi một hàm lệnh và nhận một luồng sự kiện.
- **Plugin Google Mobile Ads của Unity**: rewarded, app open, banner view (banner thường và MREC). HDCLib tự quản lý load, retry, show và sự kiện cho các định dạng này.

Sự kiện của cả hai nguồn đi chung qua `HDCAdsSdk.AdEvent`.

Có hai tầng API:

- `HDCAds` (tầng kênh): force ad theo vị trí có capping, rewarded, app launch, app resume, banner, MREC, popup. Tầng này chạy theo config lấy từ Firebase Remote Config. Game thường chỉ cần dùng tầng này.
- `HDCAdsSdk` (tầng SDK): load/show trực tiếp theo instance id.

## Cấu trúc

```
Runtime/                       Assembly HDC.Ads, chỉ compile khi có define HDC_ADS
  Logic/                       Tầng kênh HDCAds: config, group fallback, các kênh
  Firebase/                    HDCRemoteConfig (assembly HDC.Ads.Firebase, cần define HDC_FIREBASE)
  HDCAdsSdk.cs                 API các định dạng KMP: interstitial, fullscreen, popup, banner, Meta test
  HDCAdsSdk.Gma.cs             API các định dạng qua plugin GMA: rewarded, app open, banner view, test device
  HDCAdOptions.cs              Tuỳ chọn fullscreen, popup, banner (tên field giống JSON config), vị trí banner view
  HDCAdEvent.cs                Dữ liệu sự kiện, HDCAdEventType, HDCAdFormat
  Internal/                    Cầu nối iOS (DllImport), Android (JNI), Editor (giả lập), main thread, retry
  Internal/Gma/                Rewarded, app open, banner view qua plugin GMA
Plugins/iOS/                   HDCAds.xcframework, HDCAdsBridge.mm
Plugins/Android/Repository~/   Maven repo chứa thư viện Android (Unity bỏ qua thư mục có đuôi "~")
Editor/                        Bật/tắt HDC Ads, post-process Xcode, mẫu Dependencies.xml
Demo/                          HDCAdsDemo: các nút bấm để thử từng định dạng
```

## Bật / tắt

Mặc định HDCLib ở trạng thái **tắt**, và không có gì của HDCLib vào bản build.

- `Tools > HDC Ads > Enable`:
  - Thêm define `HDC_ADS` cho Android, iOS và Standalone. Nếu project có Firebase Remote Config thì thêm cả `HDC_FIREBASE`.
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
    HDCAdsSdk.LoadRewarded("rw_main", "ca-app-pub-xxx/rrr");
    HDCAdsSdk.LoadBannerView("bn_bottom", "ca-app-pub-xxx/bbb", HDCBannerViewPlacement.FullBottom);
});

if (HDCAdsSdk.IsFullscreenReady("fs_main"))
    HDCAdsSdk.ShowFullscreen("fs_main", new HDCFullscreenOptions { layoutNames = new[] { "fs_single_cls_01" } });

HDCAdsSdk.ShowRewarded("rw_main", rewarded => { if (rewarded) GiveCoins(); });
HDCAdsSdk.ShowBannerView("bn_bottom");
```

- Gọi API từ main thread của Unity. Sự kiện cũng luôn tới trên main thread.
- Mỗi sự kiện có `id`, `format` và `type`:
  - `Loaded`, `LoadFailed`: `LoadFailed` chỉ báo khi đã thử hết các ad unit.
  - `Shown`, `ShowFailed`, `Closed`.
  - `Impression`, `Clicked`.
  - `Paid`: có `valueMicros`, `currency`, `precision`, `adSource`.
  - `Rewarded`: có `rewardType`, `rewardAmount`.
- Với banner view, `Shown` báo khi view hiện và đã có ad, `Closed` báo khi ẩn.
- Load lỗi được thử lại sau 2, 4, 8, 16, 32 rồi 64 giây, và reset khi load được:
  - Rewarded và app open: sau mỗi lần show sẽ load ad mới. Show lúc chưa có ad thì load ngay, trừ khi đang chờ retry.
  - Interstitial: KMP load mọi ad unit cùng lúc và dừng khi tất cả lỗi, nên HDCLib gọi load lại.
  - Banner view: chỉ retry lần load đầu. Sau khi có ad, view tự refresh theo cấu hình ad unit.
- Rewarded và app open có chế độ `preload`: plugin tự giữ sẵn 1–5 ad và tự load lại.
- App open quá 4 giờ kể từ lúc load thì coi là chưa sẵn sàng.
- `HDCAdsSdk.EnableTestDevice()` đăng ký máy đang chạy là test device của Google Mobile Ads, áp dụng cho mọi định dạng.
- Trên iOS, sự kiện tới ngay cả khi quảng cáo fullscreen đang pause Unity. Trên Android, sự kiện phát ra trong lúc quảng cáo fullscreen che game sẽ tới khi Unity chạy lại.
- Trong Editor, SDK được giả lập:
  - Load mất 0,5 giây.
  - Show bắn `Shown`, `Impression`, `Paid`.
  - Interstitial và fullscreen đóng sau 1 giây, popup sau 3 giây. Banner giữ đến khi ẩn.
  - Rewarded, app open và banner view dùng quảng cáo mẫu có sẵn của plugin GMA trong Editor.

## Kênh quảng cáo (HDCAds)

```csharp
using HDC.Ads;

// Config mặc định dùng khi Remote Config và máy chưa có giá trị (ví dụ TextAsset trong project).
HDCRemoteConfig.FetchAndInitialize(
    defaultAdsConfig.text,
    new Dictionary<string, string> { { "adcore_main_ios", defaultCoreConfig.text } },
    () => Debug.Log("ads ready"));

HDCAds.AppLaunch.Completed += () => LoadMainScene();

HDCAds.ForceAd.Show("native_gameplay", onDone: ResumeGame);
HDCAds.Rewarded.Show("shop", onRewarded: GiveCoins);
HDCAds.Banner.Show();                               // slot FullBottom
HDCAds.Popup.Move("popup", popupArea);              // RectTransform; popup chỉ show sau khi đặt vị trí
HDCAds.Popup.Show("popup");
HDCAds.SetAdsRemoved(true);                         // mua gỡ quảng cáo: chặn mọi kênh trừ rewarded
```

Config giữ nguyên key và schema JSON của hệ thống cũ, nên dùng lại được giá trị Remote Config sẵn có:

- `ads_config` (`HDCAdsConfig`) quy định từng kênh:
  - Có bật không, có tự load không (`autoInit`).
  - Force ad: vị trí, capping, `launchCappingTime`, mức giảm capping theo lượt hiển thị, break ad.
  - Banner: 6 slot, `autoShowOnLoad`.
  - Popup: vị trí.
- Config core (`HDCAdCoreConfig`) nằm dưới key do `selectedAdCoreName` chỉ định:
  - Group force ad theo vị trí, ad unit theo thứ tự ưu tiên (`mediationPriority`, `useBackup`), `maxShowCount`, `disablePostInitReload`.
  - Layout group của native fullscreen, asset config.
  - `comebackChannel`: launch dùng force ad hay app open.
- `admobUnit` chạy qua plugin Google Mobile Ads. `androidUnit` chạy qua thư viện native trên cả Android và iOS:
  - Native fullscreen dùng layout ngẫu nhiên trong layout group, không lặp cho tới khi dùng hết.
  - Nếu bật `switchToInterstitialAndroid` thì dùng interstitial.
- `HDCRemoteConfig` lấy giá trị theo thứ tự: Remote Config, giá trị máy lưu từ lần trước, rồi config mặc định. Nó luôn fetch mới, timeout mặc định 10 giây.

Hành vi các kênh:

- Force ad:
  - Chỉ show khi thời gian từ lúc đóng fullscreen gần nhất đạt capping của vị trí. Capping là `cappingTime` trừ số lượt đã hiển thị × mức giảm, không thấp hơn mức tối thiểu. Force ad đầu phiên phải chờ thêm `launchCappingTime`.
  - Số lượt hiển thị theo vị trí lưu trong PlayerPrefs (`fa_count_<vị trí>`).
  - `StartBreakAd()` chạy đồng hồ break ad. Đồng hồ reset mỗi khi có fullscreen mở. Có các event `BreakAdNotice`, `BreakAdShown`, `BreakAdClosed`, `BreakAdShowFailed`.
- Group có `useBackup`: ban đầu chỉ load unit đầu. Unit đang dùng mà lỗi load hoặc lỗi show thì load unit kế tiếp. Show lấy unit sẵn sàng đầu tiên.
- App launch:
  - Đồng hồ chạy từ lúc SDK sẵn sàng (hoặc từ `AppLaunch.Initialize()` khi `autoInit` tắt).
  - Ad hiện khi đã qua `minWaitSeconds` (mặc định 5 giây) và ad đã sẵn sàng.
  - Quá `timeoutSeconds` thì bỏ qua ad.
  - `Completed` luôn được gọi đúng một lần.
- App resume:
  - Load native fullscreen khi app xuống nền và hiện khi load xong.
  - Lần xuống nền kế tiếp bị bỏ qua nếu vừa có fullscreen mở, vừa bấm banner, hoặc game đã gọi `AppResume.Block()`.
- Rewarded vẫn hiện khi đã gỡ quảng cáo. Các kênh còn lại đều bị chặn.
- Chưa hỗ trợ: collapsible banner, tracking doanh thu lên Firebase/Adjust, config theo quốc gia.

## Cập nhật thư viện native

Chạy trong dự án AdsMultiplatform, nhánh `migrate/HDC-Lib`:

```bash
./gradlew :shared:exportUnityPlugins -PunityPluginsDir=<Unity project>/Assets/HDCLib/Plugins
```

Lệnh này build `HDCAds.xcframework` bản release (kèm Compose resources). Nó cũng publish maven repo Android `com.hdc.adsmultiplatform:shared-android` vào `Plugins/Android/Repository~`. Khi đổi version thư viện Android, sửa cả `Editor/Templates~/HDCAdsDependencies.xml`.

## Yêu cầu

- Plugin Google Mobile Ads của Unity (bản 11.x) phải có trong project. Plugin này ghi App ID của AdMob vào `Info.plist` và `AndroidManifest.xml`. Ngoài ra nó phục vụ rewarded, app open và banner view. Assembly `HDC.Ads` dùng trực tiếp các DLL của plugin.
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
- Giai đoạn 3:
  - Rewarded, app open, banner view (6 vị trí và MREC) qua plugin Google Mobile Ads.
  - Retry khi load lỗi, test device.
- Giai đoạn 4: tầng kênh `HDCAds` với config từ Firebase Remote Config (schema cũ).
