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
Plugins/iOS/                   HDCAds.xcframework, HDCAdsBridge.mm (post-process Xcode tự thêm vào project)
Plugins/Android/Repository~/   Maven repo chứa thư viện Android hdc-ads-android (Unity bỏ qua thư mục có đuôi "~")
Editor/                        Bật/tắt HDC Ads, post-process Xcode, mẫu Dependencies.xml
Demo/                          HDCAdsDemo: các nút bấm để thử từng định dạng
package.json                   Để cài HDCLib như một package (UPM)
```

## Cài đặt

HDCLib chạy được ở bất kỳ thư mục nào. Script Editor tự tìm vị trí của nó.

- Copy hoặc clone repo vào một thư mục trong `Assets`, ví dụ `Assets/HDCLib`.
- Hoặc cài như package:
  - Package Manager > Add package from git URL: `https://github.com/hiepdang26/HDC-Library.git`. Máy cần có git-lfs, vì file nhị phân của framework iOS nằm trong LFS.
  - Hoặc đặt thư mục vào `Packages/` (embedded package).

## Bật / tắt

Mặc định HDCLib ở trạng thái **tắt**, và không có gì của HDCLib vào bản build.

- `Tools > HDC Ads > Enable`:
  - Thêm define `HDC_ADS` cho Android, iOS và Standalone. Nếu project có Firebase Remote Config thì thêm cả `HDC_FIREBASE`.
  - Tạo file `HDCAdsDependencies.xml` cho External Dependency Manager, với đường dẫn repo Maven theo vị trí thật của HDCLib. File nằm ở `<HDCLib>/Editor/`, hoặc ở `Assets/HDCAds/Editor/` khi HDCLib là package.
- `Tools > HDC Ads > Disable` hoàn tác các bước trên.
- Plugin iOS (`HDCAds.xcframework`, `HDCAdsBridge.mm`) luôn để tắt trong importer. Khi build iOS có `HDC_ADS`, post-process copy framework vào `Frameworks/HDCAds/` và file bridge vào `Libraries/HDCAds/` của project Xcode, rồi thêm cả hai vào target UnityFramework.
  - Cách này chạy giống nhau trên mọi bản Unity, kể cả khi HDCLib là package chỉ đọc.
  - Nếu build không có `HDC_ADS` đè lên (Append) một bản export cũ, post-process gỡ hai file đó ra.

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
- Hỗ trợ tắt domain reload (Enter Play Mode Options, mặc định của project Unity 6.6 mới). Mọi state tĩnh của HDCLib (ad, callback, subscriber, kênh) được reset mỗi lần vào Play Mode.
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
./gradlew :shared:exportUnityPlugins -PunityPluginsDir=<HDCLib>/Plugins
```

Lệnh này làm hai việc:

- Build `HDCAds.xcframework` bản release (kèm Compose resources) từ module `:shared`.
- Publish thư viện Android `com.hdc.adsmultiplatform:hdc-ads-android` từ module `:unityAndroid` vào `Plugins/Android/Repository~`.
  - Module này dùng lại logic của `:shared` và vẽ quảng cáo bằng View/XML, không dùng Compose, để mọi bản Unity build được.
  - Khi đổi version, sửa cả `unityAndroid/build.gradle.kts` và `Editor/Templates~/HDCAdsDependencies.xml`.

## Yêu cầu

- Unity 2021.3 trở lên, tới Unity 6000.6. Xem bảng bên dưới.
- Plugin Google Mobile Ads của Unity, bản 11.x (đã thử 11.4.0). Plugin này ghi App ID của AdMob vào `Info.plist` và `AndroidManifest.xml`. Nó cũng phục vụ rewarded, app open và banner view. Assembly `HDC.Ads` dùng trực tiếp các DLL của plugin.
- External Dependency Manager 1.2.151 trở lên. Bản này mới đọc được repo Maven nằm trong package.
- iOS 15.0 trở lên. Post-process tự nâng deployment target của project Xcode và Podfile. Pod: GMA iOS 13.9, Meta adapter 6.22.0.0.
- Android minSdk 24.

### Android theo từng bản Unity

Thư viện Android `hdc-ads-android` không dùng Compose:

- Bytecode Java 8, minSdk 24. Không bắt compileSdk mới (`minCompileSdk` = 30).
- POM chỉ kéo `kotlin-stdlib` 2.0.21, `constraintlayout` 2.1.4 và `cardview` 1.0.0.
- GMA và Meta không nằm trong POM, nên version do game quyết định:
  - GMA đến từ plugin GMA Unity. Thư viện cần GMA Android SDK 24.2.0 trở lên.
  - Meta đến từ gói mediation Meta, và là tùy chọn. Không có Meta thì Meta test mode báo không dùng được.
  - Thư viện được compile với GMA 24.2.0 và Meta 6.17.0. Mọi lời gọi GMA/Meta trong AAR đã được kiểm tra là có đủ trong GMA 25.4.0 và Meta 6.22.0.

Đã build thử bằng project Gradle giống Unity (`unityLibrary` + `launcher`) có GMA 25.4.0, UMP và HDC, cả debug lẫn release có R8, với đúng Gradle, AGP và JDK của từng bản Unity:

| Unity | Gradle / AGP / JDK | Kết quả |
|---|---|---|
| 2021.3.0 – 2021.3.36 | 6.1.1 / 4.0.1 / 11 | Được khi tắt Jetifier (xem lưu ý) |
| 2021.3.37 – 2021.3.40 | 6.7.1 / 4.2.2 / 11 | Được khi tắt Jetifier |
| 2022.3.0 – 2022.3.37 | 7.2 / 7.1.2 / 11 | Được |
| 2021.3.41+, 2022.3.38+ | 7.5.1 / 7.4.2 / 11 | Được. Đã build APK thật bằng Unity 2022.3.62, qua External Dependency Manager |
| 6000.0.1 – 6000.0.44 | 8.4 / 8.3.0 / 17 | Được (compileSdk 35) |
| 6000.0.45 – 6000.0.60, 6000.1 | 8.11 / 8.7.2 / 17 | Được |
| 6000.0.61 – 6000.0.78, 6000.2, 6000.3.1 – 6000.3.16, 6000.4.1 – 6000.4.3 | 8.13 / 8.10.0 / 17 | Được |
| 6000.0.79 – 6000.0.84, 6000.3.17 – 6000.3.25, 6000.4.4+, 6000.5, 6000.6.1 – 6000.6.2 | 9.1.0 hoặc 9.3.1 / 9.0.0 / 17 | Được |
| 6000.0.85+, 6000.3.26+, 6000.6.3+ | 9.3.1 / 9.1.1 / 17 | Được |

Các giới hạn sau đến từ GMA, Meta và AndroidX, không phải từ HDC:

- Unity 2021.3 trước bản .41 (AGP 4.x): Jetifier đời cũ không đọc được class Java 17 trong AndroidX mà GMA kéo theo (`activity` 1.8.1, `annotation-experimental` 1.4.0). Cần đặt `android.enableJetifier=false` trong `gradleTemplate.properties`, hoặc nâng Unity lên 2021.3.41+.
- Meta Audience Network (adapter `com.google.ads.mediation:facebook`):
  - 6.22.0.0 kéo `androidx.browser` 1.9.0, yêu cầu AGP 8.9.1 và compileSdk 36. Chỉ dùng được từ Unity 6000.0.61 / 6000.2 trở lên.
  - 6.21.0.0: D8 của AGP 7.4.2 lỗi NullPointerException khi dex Meta SDK 6.21.0. Dùng được từ Unity 6000.0 trở lên.
  - Unity 2021.3 và 2022.3 dùng tối đa 6.20.0.0.
- GMA 25.x tham chiếu API Android 35. Từ AGP 8 trở lên cần compileSdk ≥ 35 (Target API Level 35+), nếu không R8 báo thiếu class `android.media.LoudnessCodecController`.

### iOS

- Post-process tự thêm `HDCAds.xcframework` và `HDCAdsBridge.mm` vào project Xcode, nên không phụ thuộc cách từng bản Unity xử lý plugin `.xcframework`.
- Build cho simulator:
  - Unity link UnityFramework bằng `-all_load`, để engine (ở simulator là thư viện động) tìm được IL2CPP theo tên. Nhưng `-all_load` nạp mọi phần của HDCAds, và các thư viện Skia bên trong lặp object nên link lỗi trùng symbol (HarfBuzz).
  - Post-process thay cờ đó bằng `-force_load` cho riêng `libil2cpp.a`, `libGameAssembly.a` và `baselib.a`.
  - HDCAds chỉ có slice simulator arm64. Từ Unity 2022.3, đặt Player Settings > iOS > Simulator Architecture = ARM64. Unity 2021.3 chỉ build simulator x86_64, nên ở bản này HDC chỉ chạy trên máy thật.
- Đã kiểm chứng bằng Unity 2022.3.62, với External Dependency Manager chạy `pod install`:
  - Simulator SDK: Xcode build xong. Trên simulator, SDK khởi tạo, lấy được hash test device của Meta và load được interstitial test của Google.
  - Device SDK: Xcode build bản Release cho máy thật (không ký) xong, không còn symbol nào của HDCAds chưa link.

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
- Giai đoạn 5: chạy trên mọi bản Unity từ 2021.3 đến 6000.6.
  - Thư viện Android mới `hdc-ads-android` không dùng Compose. Version GMA và Meta do game quyết định.
  - iOS: post-process tự thêm framework và bridge vào project Xcode.
  - Cài được như package (UPM).
  - Hỗ trợ tắt domain reload.
