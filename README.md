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
  Settings/                    HDCAdsSettings: config mặc định (assembly HDC.Ads.Settings, luôn được biên dịch)
  HDCAdsSdk.cs                 API các định dạng KMP: interstitial, fullscreen, popup, banner, Meta test
  HDCAdsSdk.Gma.cs             API các định dạng qua plugin GMA: rewarded, app open, banner view, test device
  HDCAdOptions.cs              Tuỳ chọn fullscreen, popup, banner (tên field giống JSON config), vị trí banner view
  HDCAdEvent.cs                Dữ liệu sự kiện, HDCAdEventType, HDCAdFormat
  Internal/                    Cầu nối iOS (DllImport), Android (JNI), Editor (giả lập), main thread, retry
  Internal/Gma/                Rewarded, app open, banner view qua plugin GMA
Plugins/iOS/                   HDCAds.xcframework, HDCAdsBridge.mm (post-process Xcode tự thêm vào project)
Plugins/Android/Repository~/   Maven repo chứa thư viện Android hdc-ads-android (Unity bỏ qua thư mục có đuôi "~")
Editor/                        Menu HDC (bật/tắt, sửa config), post-process Xcode, mẫu Dependencies.xml
Setup/                         HDCAdsSetup.prefab: khởi động ads ở scene đầu (assembly HDC.Ads.Setup, luôn được biên dịch)
Debug/                         Bảng debug HDCAdsDebugPanel.prefab (assembly HDC.Ads.Debug, cần define HDC_ADS)
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

- `HDC > Ads > Enable`:
  - Thêm define `HDC_ADS` cho Android, iOS và Standalone. Nếu project có Firebase Remote Config thì thêm cả `HDC_FIREBASE`.
  - Tạo file `HDCAdsDependencies.xml` cho External Dependency Manager, với đường dẫn repo Maven theo vị trí thật của HDCLib. File nằm ở `<HDCLib>/Editor/`, hoặc ở `Assets/HDCAds/Editor/` khi HDCLib là package. Khi HDC đang bật, file này tự cập nhật theo template mỗi lần Unity nạp lại script, nên bản HDCLib mới đổi version thư viện Android thì project nhận ngay.
- `HDC > Ads > Disable` hoàn tác các bước trên.
- Plugin iOS (`HDCAds.xcframework`, `HDCAdsBridge.mm`) luôn để tắt trong importer. Khi build iOS có `HDC_ADS`, post-process copy framework vào `Frameworks/HDCAds/` và file bridge vào `Libraries/HDCAds/` của project Xcode, rồi thêm cả hai vào target UnityFramework.
  - Cách này chạy giống nhau trên mọi bản Unity, kể cả khi HDCLib là package chỉ đọc.
  - Nếu build không có `HDC_ADS` đè lên (Append) một bản export cũ, post-process gỡ hai file đó ra.

Hai static framework build từ cùng dự án KMP không link chung được vào một app iOS: chúng trùng class Compose UIKit và bridge native. Nếu project còn một framework như vậy (ví dụ của thư viện ads cũ), thì khi HDC bật, post-process iOS sẽ:

- Bỏ framework đó khỏi project Xcode, và xoá bản copy của nó trong thư mục export, để không script nào copy resources của nó đè lên resources của HDCAds.
- Bỏ các file native import framework đó.
- Sinh `Libraries/HDCAds/HDCAdsStandIns.mm`. Các hàm C của những file đó mà code C# gọi được thay bằng hàm không làm gì, nên bản build vẫn link được.

Tính năng native dùng framework đó không chạy trong bản build này, và log build ghi rõ những gì đã bị bỏ ra. Muốn build với framework đó thì tắt HDC (`HDC > Ads > Disable`) rồi export kiểu Replace.

## Scene đầu tiên: prefab HDCAdsSetup

Kéo prefab `Setup/HDCAdsSetup.prefab` vào scene đầu tiên của game, hoặc dùng `HDC > Setup > Add to open scene`. Không cần viết code khởi động. Prefab làm lần lượt:

1. Load `nextScene` ở nền.
2. Lấy config. Trên máy thật, config đến từ Remote Config (cần `HDC_FIREBASE`; bước này cũng khởi tạo Firebase), chỗ nào thiếu thì dùng config mặc định. Trong Editor, prefab dùng config mặc định.
3. Khởi tạo HDCAds: Google Mobile Ads (cả plugin Unity và thư viện native, cùng các adapter mediation), rồi bật các kênh:
   - App launch luôn được bật, vì splash chờ nó.
   - Các kênh khác chỉ được bật khi `startAllChannels` đang bật.
4. Chờ app launch xong, tối đa `maxWaitSeconds`, rồi mở `nextScene`.

Các trường của prefab:

- `nextScene`: scene mở sau splash, phải có trong Build Settings. Để trống thì ở lại scene hiện tại.
- `maxWaitSeconds` (mặc định 30): quá thời gian này thì vẫn mở scene tiếp, dù ads chưa xong.
- `startAllChannels` (mặc định bật): bật cả các kênh có `autoInit` tắt trong config, để scene sau có sẵn ad.
- `debugLog`: log mọi lệnh và sự kiện ads.
- `googleTestAds` (mặc định tắt): biến máy đang chạy thành test device của Google trước khi load ad. Request vẫn dùng ad unit thật trong config, Google trả về quảng cáo test (tiêu đề có chữ `Test mode`) cho chính các unit đó. Đây là cách Google khuyên dùng khi test unit thật; bấm quảng cáo thật khi test có thể làm hỏng tài khoản AdMob. Nút `Make Test Device` ở trang Device của bảng debug làm việc tương tự lúc đang chạy.
- `googleTestAdUnits` (mặc định tắt): thay mọi ad unit trong config bằng ad unit mẫu của Google (`ca-app-pub-3940256099942544/...`, theo định dạng và nền tảng), nên mọi vị trí đều ra quảng cáo test kể cả khi unit thật không có quảng cáo (ví dụ bản build ký bằng bundle ID khác). Áp dụng từ lần load đầu tiên; quảng cáo đã load trước đó giữ unit cũ.
- Cả hai tuỳ chọn trên chỉ dành cho bản test: khi bật, log có cảnh báo; nhớ tắt trước khi phát hành.
- `onAdsReady`, `onFinished`: sự kiện khi HDCAds khởi tạo xong, và ngay trước khi mở scene tiếp.

Assembly của prefab luôn được biên dịch. Khi HDC tắt, prefab chỉ mở scene tiếp theo, nên scene đầu không bị kẹt. Nếu HDCAds đã khởi tạo rồi (ví dụ quay lại scene đầu), prefab bỏ qua bước khởi tạo.

Scene chạy riêng mà không có prefab, như scene test, thì gọi `HDCAdsSetup.InitializeAds()`.

## Config mặc định

Config mặc định là giá trị dùng tới khi Remote Config có giá trị riêng. Nó nằm trong asset `Assets/HDCAds/Resources/HDCAdsSettings.asset` (`HDC > Settings asset`).

Trong Editor, `HDCRemoteConfig` dùng thẳng config mặc định: không fetch Firebase, không đọc giá trị máy lưu. Sửa config xong bấm Play là thấy ngay. Muốn fetch Remote Config thật trong Editor thì đặt `HDCRemoteConfig.FetchInEditor = true` trước khi gọi.

- `HDC > Edit configs > Ads configs`, `Ad core Android configs`, `Ad core iOS configs` mở cửa sổ sửa JSON:
  - Có 4 trang: ads_config và ad core config, mỗi loại cho Android và iOS. Trang iOS để trống thì dùng bản Android.
  - `Format` căn lề JSON, `Revert` bỏ thay đổi, `Save` chỉ lưu khi JSON hợp lệ. Trang có dấu `*` là chưa lưu.
- Prefab HDCAdsSetup tự dùng các config này. Nếu tự khởi tạo bằng code:

```csharp
HDCAdsSettings defaults = HDCAdsSettings.Load();
HDCRemoteConfig.FetchAndInitialize(defaults.AdsConfig, defaults.CoreConfigsByKey(), () => Debug.Log("ads ready"));
```

`CoreConfigsByKey()` đặt core config mặc định dưới mọi key mà ads_config có thể chọn: `adcore_main_android`, `adcore_main_ios` và key ghi trong `selectedAdCoreName`.

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
  - Load mất 0,5 giây. Ad unit có chữ `fail` trong ID thì load lỗi với mã 3 (no fill).
  - Show bắn `Shown`, `Impression`, `Paid`.
  - Interstitial và fullscreen đóng sau 1 giây, popup sau 3 giây. Banner giữ đến khi ẩn.
  - Rewarded, app open và banner view dùng quảng cáo mẫu có sẵn của plugin GMA trong Editor.

## Kênh quảng cáo (HDCAds)

```csharp
using HDC.Ads;

// Prefab HDCAdsSetup ở scene đầu đã khởi tạo HDCAds và mở scene tiếp sau app launch.
// Nếu tự làm bằng code: gọi HDCAdsSetup.InitializeAds(), HDCAds.AppLaunch.Initialize(), rồi chờ HDCAds.AppLaunch.Completed.

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
- Trên máy thật, `HDCRemoteConfig` lấy giá trị theo thứ tự: Remote Config, giá trị máy lưu từ lần trước, rồi config mặc định. Nó luôn fetch mới, timeout mặc định 10 giây. Firebase không chạy được (kể cả khi thiếu thư viện native) thì nó cũng dùng giá trị máy lưu rồi config mặc định. Trong Editor nó dùng config mặc định, trừ khi bật `HDCRemoteConfig.FetchInEditor`. Trang Remote Config của bảng debug cho thấy từng key lấy từ nguồn nào.
- Giá trị Remote Config là `{}` vẫn được coi là có giá trị. Một ad core config `{}` nghĩa là không có ad unit nào.

Hành vi các kênh:

- Force ad:
  - Chỉ show khi thời gian từ lúc đóng fullscreen gần nhất đạt capping của vị trí. Capping là `cappingTime` trừ số lượt đã hiển thị × mức giảm, không thấp hơn mức tối thiểu. Force ad đầu phiên phải chờ thêm `launchCappingTime`.
  - Số lượt hiển thị theo vị trí lưu trong PlayerPrefs (`fa_count_<vị trí>`).
  - `StartBreakAd()` chạy đồng hồ break ad. Đồng hồ reset mỗi khi có fullscreen mở. Có các event `BreakAdNotice`, `BreakAdShown`, `BreakAdClosed`, `BreakAdShowFailed`.
- Group có `useBackup`: ban đầu chỉ load unit đầu. Unit đang dùng mà lỗi load hoặc lỗi show thì load unit kế tiếp. Show lấy unit sẵn sàng đầu tiên.
- App launch:
  - Đồng hồ chạy từ lúc SDK sẵn sàng (hoặc từ `AppLaunch.Initialize()` khi `autoInit` tắt). Gọi `Initialize()` trước khi SDK sẵn sàng thì đồng hồ chờ tới lúc sẵn sàng.
  - Ad hiện khi đã qua `minWaitSeconds` (mặc định 5 giây) và ad đã sẵn sàng.
  - Quá `timeoutSeconds` thì bỏ qua ad.
  - Kênh tắt, đã gỡ quảng cáo hoặc không có unit nào: `Completed` được gọi ngay, không chờ.
  - `Completed` luôn được gọi đúng một lần.
- App resume:
  - Load native fullscreen khi app xuống nền và hiện khi load xong.
  - Lần xuống nền kế tiếp bị bỏ qua nếu vừa có fullscreen mở, vừa bấm banner, hoặc game đã gọi `AppResume.Block()`.
- Rewarded vẫn hiện khi đã gỡ quảng cáo. Các kênh còn lại đều bị chặn.
- Chưa hỗ trợ: collapsible banner, tracking doanh thu lên Firebase/Adjust, config theo quốc gia.

## Bảng debug

Prefab `Debug/HDCAdsDebugPanel.prefab` là bảng debug nằm đè lên game. Chức năng bám theo bảng debug của hệ thống cũ (ad systems, configs, tracking, diagnostics, Adjust, build/device/network), giao diện làm mới, và thêm trạng thái của từng ad unit.

- Thêm vào scene: `HDC > Debug panel > Add to open scene`, hoặc kéo prefab vào scene.
- Bảng ẩn khi vào scene. Mở bằng cách chạm nhanh 3 lần vào góc trên bên trái (14% chiều rộng và chiều cao, trong 0,9 giây), hoặc nhấn F10; đóng bằng nút `Close`.
  - Chạy được với Input Manager cũ lẫn Input System mới. Scene thiếu EventSystem thì bảng tự tạo, với input module hợp loại input của project.
  - Bảng nằm trong safe area, và tự đổi tỉ lệ khi màn hình nằm ngang.
  - Các tùy chọn trên prefab: góc mở (`activationCorner`), số lần chạm, `startOpen`, `keepAcrossScenes`.
- Đầu bảng: trạng thái SDK và ad core đang dùng, nút `Refresh`, `Close`, và `Init SDK` khi HDCAds chưa khởi tạo (mở thẳng scene không có HDCAdsSetup; nút gọi `HDCAdsSetup.InitializeAds()`). Bốn trang: Ads, Remote Config, Events, Device.

### Trang Ads

- Tab kênh: AL (app launch), AR (app resume), RW (rewarded), FA (force ad), BN (banner), MREC, CL (collapsible, HDC chưa có), PU (popup). Chấm màu trên tab là trạng thái chung của kênh: xanh lá có ad sẵn sàng, xanh dương đang hiện, cam đang load, đỏ đang lỗi, xám chưa chạy.
- Thẻ Actions: chọn `Group` và `Position` từ config HDCAds đang chạy (trên máy thật là giá trị Remote Config); banner chọn placement, MREC chọn vị trí trên màn hình. Các nút gọi thẳng API của HDCAds:
  - `Init` và `Show`. Với banner và MREC, nút `Show` thành `Activate`.
  - `Hide` cho banner, MREC và popup.
  - `UpdatePos` và `GetSize` cho MREC, `UpdatePos` cho popup. Popup hiện trong vùng `Popup area` ở cuối màn hình.
- Thẻ Detail Information Ad: chỉ group đang chọn (banner: placement đang chọn).
  - Thông tin group từng dòng: positions, priority, backup, số lần show tối đa, ready, units started...
  - Mỗi ad unit một thẻ, theo thứ tự group thử chúng. Nhãn trạng thái: `LOADING`, `READY`/`LOADED`, `SHOWING`, `LOAD FAILED` (kèm thời gian retry), `SHOW FAILED`, `CLOSED`, `BACKUP · NOT STARTED`, `NOT INITIALIZED`.
  - Lỗi load hoặc show gần nhất, mỗi ý một dòng: mã lỗi của SDK cùng tên (ví dụ `3 · NO_FILL`), ý nghĩa bằng tiếng Việt, gợi ý cần kiểm tra gì, ad unit lỗi và thông điệp gốc. Mã lấy theo Google Mobile Ads Android hoặc iOS; `-1` là lỗi của HDC hoặc native, không có mã của SDK.
  - Số liệu từng dòng: requests, loaded, load failed, shows, show failed, impressions, clicks, thời gian load gần nhất, doanh thu, lần retry tới.
  - `Copy Report` chép trạng thái group, sự kiện và system của kênh vào clipboard để gửi cho người khác.
- Thẻ Recent Events (ẩn sẵn): `Expand` mở 15 sự kiện gần nhất của group ngay trong thẻ; `Full Screen` mở toàn màn hình, chữ to, kèm giải thích từng sự kiện.
- Thẻ System (ẩn sẵn): `Expand` mở config, trạng thái và các điều kiện chặn của riêng kênh đang chọn, kèm position và group đang chọn, rồi API của kênh.

### Trang Remote Config

- Thẻ Remote Config: config lấy từ đâu (Remote Config, giá trị lưu trên máy, hay mặc định), trạng thái Firebase, lần fetch gần nhất, thời gian tải, ad core key, nguồn của từng key và lúc áp dụng vào HDCAds.
- Thẻ Config Check: lỗi và cảnh báo của config, bằng tiếng Việt: Firebase không chạy, fetch lỗi, key không có trên Remote Config, ad core config rỗng `{}`, kênh bật mà không có ad unit, position không thuộc group nào, layout group không tồn tại, position trùng tên...
- Thẻ Config Viewer: xem `ads_config`, ad core config hoặc mọi key Remote Config đang có (`All Keys`), theo 4 nguồn:
  - `Remote`: giá trị trên Remote Config; `Saved`: giá trị lần trước lưu trên máy; `Default`: config mặc định trong `HDC > Edit configs`; `Applied`: config HDCAds đang chạy.
  - Config đang chạy lấy từ nguồn nào được ghi rõ: dòng `APPLIED = REMOTE` (hoặc `SAVED`, `DEFAULT`) kèm lý do nằm ngay trên JSON, nút `Applied` hiện `Applied: Remote`, và nút của nguồn đó có chữ `used`. `Direct` nghĩa là code khác tự gọi `HDCAds.Initialize`, không qua Remote Config của HDC.
  - JSON được thụt dòng và tô màu; các key cấp 1 thu gọn sẵn, bấm để mở từng key hoặc `Expand All`. `Copy` chép JSON đầy đủ, `Full Screen` xem toàn màn hình.
- Thẻ Ad Units Map (ẩn sẵn): ad unit, priority và position của từng kênh, group và placement.
- Trong Editor HDC dùng config mặc định, không gọi Firebase, trừ khi bật `HDCRemoteConfig.FetchInEditor`.

### Trang Events

- Mọi sự kiện quảng cáo của mọi kênh (giữ 300 sự kiện gần nhất): tổng số, số theo loại và tổng doanh thu.
- `Sequential` (mới nhất ở trên) hoặc `Count` (gộp theo loại và ad); lọc theo channel và loại sự kiện; `Explain` thêm giải thích tiếng Việt; `Copy`, `Clear`, `Full Screen`.

### Trang Device

- Build: version, bundle ID, bản Unity, platform, development hay release, scripting backend.
- HDC Ads: thư viện native, Remote Config có bật không (`HDC_FIREBASE`), trạng thái khởi tạo, ads removed, `Google Test Device` và `Google Test Ad Units`. Nút `Debug Log`, `Make Test Device` (máy này thành test device: request vẫn dùng ad unit thật, Google trả quảng cáo test, áp dụng cho các lần load sau), `Meta Test Mode On/Off` kèm device hash. Muốn thay hẳn ad unit bằng unit test của Google thì bật `HDCAdsSetup > Google Test Ad Units` (xem phần HDCAdsSetup).
- Device: model, hệ điều hành, CPU, RAM, GPU, màn hình, safe area, pin.
- Network: kết nối, và IP công khai, quốc gia, nhà mạng (tra từ ipwho.is khi mở trang lần đầu hoặc bấm `Check Public IP`), để biết điều kiện quốc gia của Remote Config nhận máy là ở đâu.
- Adjust: phiên bản SDK, adid và attribution, nếu game có Adjust SDK (HDC Ads không dùng Adjust nên đọc qua reflection).

### Khác

- Trong Editor, ad unit có chữ `fail` trong ID sẽ load lỗi với mã 3 (no fill), để thử cách bảng hiện lỗi.
- Đây là công cụ debug: gỡ khỏi scene trước khi build bản phát hành.
- Giao diện prefab được dựng bằng code trong `Debug/Editor/HDCAdsDebugPanelBuilder.cs`. Muốn sửa giao diện thì sửa ở đó rồi chạy `HDC.Ads.DebugUI.Editor.HDCAdsDebugPanelBuilder.Build` để dựng lại prefab.

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
- Post-process đặt `CADisableMinimumFrameDurationOnPhone = true` trong Info.plist. Quảng cáo native (full-screen, popup, banner) vẽ bằng Compose Multiplatform, và Compose dừng app ngay lần đầu hiện quảng cáo nếu key này thiếu hoặc là `false`. Unity ghi `false` khi Player Settings > iOS > Enable ProMotion đang tắt.
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
- Menu `HDC` riêng trên thanh menu: bật/tắt Ads, sửa config mặc định trong cửa sổ có kiểm tra JSON.
- Bảng debug `HDCAdsDebugPanel`, bốn trang:
  - Ads: chọn kênh, group và vị trí theo config rồi gọi init/show/hide; group đang chọn hiện từng ad unit với trạng thái, mã lỗi kèm giải thích và số liệu. Native banner và popup cũng báo mã lỗi thật của SDK (thư viện Android 0.3.1 và framework iOS mới).
  - Remote Config: config lấy từ đâu, kiểm tra lỗi config, xem JSON theo Remote, Saved, Default, Applied và mọi key Remote Config.
  - Events: mọi sự kiện quảng cáo, lọc và giải thích. Device: build, test ads, thiết bị, mạng, Adjust.
- Prefab `HDCAdsSetup` cho scene đầu: lấy config, khởi tạo ads, chạy app launch rồi mở scene tiếp theo.
