# Changelog

Các thay đổi của HDCLib (package `com.hdc.ads`). Số phiên bản lấy theo `package.json`.

## Chưa phát hành (dự kiến 0.6.0)

Phiên bản này chưa gắn tag. Nó còn chờ kiểm thử trên máy Android và iOS thật.

### Thay đổi không tương thích

Game giờ chỉ thấy tầng API, namespace `HDC.Ads`, gồm:

- `HDCAds`;
- interface của 8 kênh;
- `HDCAdRevenue` và `HDCAdChannel`;
- `HDCAdFormat` và `HDCAdPosition`.

Những thứ sau đã đổi so với 0.5.0:

- `HDCAds.ForceAd`, `Rewarded`, `AppLaunch`, `AppResume`, `Banner`, `Mrec` và `Popup` giờ trả về interface (`IForceAds`…).
  - Các hàm giữ nguyên tên, nên lời gọi như `HDCAds.ForceAd.Show(...)` không phải sửa.
  - Code khai báo biến kiểu lớp cụ thể, như `HDCForceAds`, thì phải đổi sang interface.
- Các thành phần sau thành internal: `HDCAdsSdk`, các lớp kênh, `HDCAdEvent`, các lớp tùy chọn và config.
  - Công tắc test chuyển sang `HDCAds.Testing`: `DebugLog`, `EnableTestDevice`, `UseTestAdUnits` và Meta test mode.
  - Luồng sự kiện thô `HDCAdsSdk.AdEvent` không còn. Để nghe doanh thu, dùng `HDCAds.Revenue`.
- `HDCAds.Config`, `HDCAds.CoreConfig` và `HDCAds.LastFullscreenAdTime` không còn public.
- Break ad cũ của force ad bị bỏ: `HDCAds.ForceAd` không còn `StartBreakAd`, `StopBreakAd`, `ResetBreakAd`, `IsBreakAdRunning` và các event `BreakAdNotice`, `BreakAdShown`, `BreakAdClosed`, `BreakAdShowFailed`. `ads_config.forceAdChannel.breakAdConfig` không còn được đọc. Thay bằng kênh break ad mới (xem Thêm).

- Config quảng cáo đổi cấu trúc, Remote Config phải sửa theo (README, mục Config):
  - `androidUnit` đổi tên thành `nativeAdUnit` ở mọi chỗ của ad core (group force ad, popup, rewarded, banner, MREC). Tên cũ không còn được đọc.
  - `comebackChannel` của ad core bị bỏ. `appResumeChannel` chuyển từ ads_config sang ad core và chọn quảng cáo bằng `resumeAdType`: 0 = interstitial (`forceAdUnit.admobUnit`), 1 = native full-screen (`forceAdUnit.nativeAdUnit`), 2 = App Open (`appOpenUnit.admobUnit`).
  - `appLaunchChannel` của ads_config và `appOpenUnit` ở gốc ad core bị bỏ. `HDCAds.AppLaunch` không còn show quảng cáo: kênh hoàn tất ngay khi SDK sẵn sàng và `CanShow` luôn là `false`.
  - `splash_config.interSplashAdUnitId` của `onboarding_config` thay bằng `launchChannel`: `launchAdType` 0 = interstitial, 1 = native full-screen, 2 = App Open, khai báo unit giống `appResumeChannel`. Luồng load và show của splash không đổi.

- Banner chỉ còn vị trí FullBottom:
  - `fullTop`, `topLeft`, `topRight`, `bottomLeft`, `bottomRight` bị bỏ khỏi `bannerChannel` (ads_config) và `bannerUnit` (ad core).
  - `HDCBannerSlot` không còn. Các hàm của `HDCAds.Banner` không còn tham số slot: `Show()`, `Hide()`, `CanShow()`, `Expand(enableClick)`, `Initialize()`. Lời gọi không truyền slot không phải sửa.

- Remote Config không còn nguồn `Saved`. HDC không lưu ads_config, ad core config và custom key lên máy để dùng cho lần mở sau nữa.
  - Mỗi lần mở game, config lấy từ lần fetch Remote Config của chính lần đó; key nào thiếu thì lấy config mặc định của project.
  - `HDCRemoteConfig.Fetch` và `FetchAndInitialize` đổi timeout mặc định từ 10 lên 30 giây (`HDCRemoteConfig.TimeoutSeconds`).
  - PlayerPrefs mà bản trước đã lưu (`ads_config`, key ad core, `HDCAds.Custom.<key>`) không còn được đọc, và không bị xoá.

### Thêm

- Tracking câu trả lời trên form consent: `4_cs_accept`, `4_cs_partial`, `4_cs_denied`, đọc từ purpose TCF ngay khi form đóng. `4_cs_ok` không đếm được người từ chối, vì bấm Do not consent vẫn cho quảng cáo giới hạn.
- Cờ bằng false thì coi như đồng ý hết.
  - `toggleConsent` = false: `HDCAds.Consent.Status` là `Obtained`, và gửi `4_cs_accept` sau `4_cs_skip`.
  - `toggleAtt` = false: `HDCAds.Consent.TrackingStatus` là `Authorized` trên iOS, và gửi `4_att_auth` sau `4_att_skip`.
- Tracking consent và ATT: các bước `4_cs_*` và `4_att_*` trong funnel splash, gửi `f_`/`n_` và `a_` như các bước khác, ngay sau `4_admob_start`. Gồm cờ tắt, form hiện và đóng, có cho quảng cáo không, câu hỏi ATT và câu trả lời. Bảng đầy đủ ở README, mục Consent và ATT.
- `ads_config` có `toggleConsent` và `toggleAtt` (mặc định `true`), như hệ thống cũ.
  - `false` thì không hỏi consent (form UMP) hoặc ATT, và coi như đã đồng ý.
  - Thiếu khoá thì vẫn hỏi như trước.
  - Cờ lấy từ `ads_config` của phiên này, hoặc cờ lưu từ lần mở trước. Lần mở đầu tiên chờ `ads_config` rồi mới hỏi.
  - Bảng debug và Config Check hiện hai cờ.
- HDC Ads Settings có hàng `Config Source` cho từng nền tảng: `Remote Config` (mặc định), hoặc `Default` (không fetch, luôn dùng config mặc định). API mới `HDCAdsSettings.ConfigSource` và enum `HDCAdsConfigSource`.
- Bảng debug, trang Remote Config, có thêm:
  - dòng `Config Source`;
  - dòng `Steps`: thời gian của từng bước (Firebase, Fetch, Activation, kết quả);
  - dòng `Extra Load Calls`;
  - `Load Time` ghi thêm thời gian game chạy, khi app từng ở nền trong lúc tải.
  - Trong lúc tải, đầu bảng ghi `Đang tải Remote Config` và ẩn `Init SDK`.
- Consent (Google UMP) và ATT của iOS, theo luồng của hệ thống cũ: hỏi consent, rồi ATT nếu consent cho quảng cáo cá nhân hoá, rồi mới khởi tạo SDK quảng cáo.
  - `HDCAdsSetup` bắt đầu hỏi ngay khi scene đầu chạy, song song với Remote Config.
  - Splash dừng ở bước `4_admob` tới khi người dùng trả lời. Consent không cho request quảng cáo thì splash bỏ quảng cáo (`5_load_ad_skip_consent`).
  - API mới `HDCAds.Consent`: `IsDone`, `CanRequestAds`, `Status`, `TrackingStatus`, `PrivacyOptionsRequired`, `ShowPrivacyOptions`, sự kiện `Done`.
  - Google test ads làm máy được hỏi như ở châu Âu.
  - iOS: post-process link `AppTrackingTransparency.framework`, và ghi `NSUserTrackingUsageDescription` mặc định khi chưa có.
  - Bảng debug: thẻ Consent & ATT ở trang Device, có nút `Privacy Options` và `Reset Consent`.
  - Khác hệ thống cũ:
    - consent từ chối thì mọi kênh tắt trong phiên đó, không chỉ splash;
    - ATT đợi app active rồi mới hỏi;
    - dịch vụ consent không trả lời thì sau 10 giây dùng consent đã lưu;
    - game gọi được form đổi lựa chọn.
- `HDCAds.Revenue` báo doanh thu của từng impression. Mỗi sự kiện có:
  - kênh, position, group (force ad và popup) và định dạng;
  - mạng, nguồn quảng cáo và ad unit;
  - giá trị, tiền tệ và precision.
- `HDCAds.Testing`, gom các công tắc test.
- Prefab `HDCAdsSetup` cho scene đầu tiên:
  - lấy config, khởi tạo ads, chạy app launch rồi mở scene tiếp theo;
  - có Google test device và Google test ad units, đặt theo preset của Build Tool (Inspector của prefab không hiện hai trường này và debug log).
- Menu `HDC`, cùng cửa sổ `HDC Ads Settings` sửa config mặc định có kiểm tra JSON.
  - Có 4 thẻ màu: Ads config, Ad core, Custom keys, Country mode.
  - Ads config và Ad core có tab Android/iOS và JSON Default/Country. Mỗi custom key là một tab.
  - Ô văn bản kéo đổi chiều rộng được. Bấm đúp vào asset cũng mở cửa sổ này.
  - Thanh trên cùng có nút màu: `Save all`, `Expand all`, `Collapse all`, `Full width`, `Select asset`.
  - Mọi chỉnh sửa là bản nháp cho tới khi lưu. Đóng cửa sổ khi còn thay đổi thì Unity hỏi có lưu không.
  - Shift+; mở cửa sổ ở giữa cửa sổ Unity; bấm lại khi cửa sổ đang mở thì chỉ đưa nó lên trước và focus, không đóng. Shift+B làm như vậy với Build Tool.
  - Cửa sổ chỉ hiện config của một nền tảng: tab đang mở của Build Tool, hoặc nền tảng đang active trong Build Settings.
- Bảng debug `HDCAdsDebugPanel`, gồm bốn trang:
  - Ads: trạng thái từng ad unit, cùng mã lỗi của SDK kèm giải thích.
  - Remote Config: kiểm tra config và cho biết config đang áp dụng lấy từ nguồn nào.
  - Events: các sự kiện quảng cáo.
  - Device: có thẻ Mediation, cho biết trạng thái từng adapter và Meta test mode.
- Bảng debug: nút `Open Ad Inspector` ở thẻ Mediation (trang Device) mở Ad Inspector của Google Mobile Ads. Thẻ còn ghi chú vì sao bật Meta test mode mà quảng cáo vẫn là AdMob (mediation group quyết định nguồn thắng; `Test Ad Units` bật thì không qua mediation), và cách chỉ nhận quảng cáo Meta bằng Ad source testing.
- Bảng debug: tab PU và MREC có khung kéo được. Giữ khung khoảng nửa giây rồi kéo để đặt popup hay MREC ở bất kỳ chỗ nào trên màn hình; thả ra thì nó nằm yên ở đó. Popup đổi cỡ bằng bốn núm ở góc, không nhỏ hơn cỡ tối thiểu của layout. Khung popup nhớ vị trí và cỡ cho lần mở sau.
- Ad unit test của Google cho mọi định dạng (`UseTestAdUnits`).
- Chế độ quốc gia như hệ thống cũ: máy ở nước đích (mặc định `vn`) dùng config quốc gia của project thay cho Remote Config.
  - Config quốc gia: ads, ad core và giá trị quốc gia của custom key. Ads và ad core là JSON `Country` trong thẻ Ads config và Ad core của cửa sổ HDC Ads Settings; giá trị custom key là tab `Country` của từng key.
  - Cách nhận nước đích sửa ở `Country check`, gồm ngôn ngữ hệ thống, ngôn ngữ và vùng của locale, tên múi giờ, độ lệch UTC, và SIM/mạng trên Android. Locale và múi giờ đọc thẳng từ hệ điều hành.
  - Mặc định không tính theo UTC+7, vì múi giờ này cũng là của nước khác.
  - Máy test (`debugDevices` trong project, hoặc key Remote Config `devices`) không vào chế độ quốc gia. Trang Device hiện ID của máy, trang Remote Config hiện trạng thái chế độ quốc gia.
- Thiết lập build iOS riêng cho từng máy ở `HDC > iOS > Local build settings`, như hệ thống cũ:
  - Local signing: team và bundle ID của máy này, ký tự động cho target app, UnityFramework và Unity-iPhone Tests. Team chọn từ dropdown giống Xcode (các tài khoản trong Xcode > Settings > Accounts), hoặc gõ team ID.
  - Run on My Mac (Designed for iPad): `Xcode default`, `On` hoặc `Off`.
  - Thiết lập lưu trong `UserSettings` của project, không commit. Có thêm menu `HDC > iOS > Clear local signing`.
- Banner `fullBottom` có `useBackup` đổi sang unit dự phòng khi banner đang hiện refresh lỗi, như hệ thống cũ:
  - Unit dự phòng load, hoặc load quảng cáo mới nếu quảng cáo của nó đã hiện quá 10 giây, rồi thay chỗ banner đang lỗi. Native banner có `reloadTime` dưới 20 giây được lỗi một lần trước khi bị thay.
  - Native banner im lặng quá `max(30, reloadTime + 5)` giây thì tính là refresh lỗi. Banner AdMob không bị tính như vậy, khác hệ thống cũ, vì chu kỳ refresh của AdMob đặt trên AdMob console.
  - Unit ưu tiên cao hơn load lại được thì lấy lại chỗ. Unit bị đổi xuống hoặc đang ẩn mà lỗi thì load lại sau 20 tới 40 giây.
  - Thẻ banner trên bảng debug có thêm dòng `Refresh Fails On Screen` và `Swaps`.
- Layout theo nguồn quảng cáo (thư viện `hdc-ads-android` 0.3.4 và framework iOS build lại):
  - Fullscreen: `adSourceGroups` trong layout group. HDCLib chọn layout theo `adSourceId` của quảng cáo đã load, giống hệ thống cũ.
  - Popup không chọn layout theo nguồn: thư viện native theo cấu trúc mới (`update/refactor`) bỏ `adSourceLayouts`, nên popup luôn dùng `nativeAdUnit.layout` và trường này trong config bị bỏ qua.
  - Sự kiện Loaded của native fullscreen có `adSourceId` và tên nguồn; sự kiện Shown của popup có layout và nguồn của quảng cáo đang hiện. Bảng debug hiện layout thật đã hiện, Config Check kiểm tên layout của các nguồn.
- Custom key Remote Config của game, như custom remote config của hệ thống cũ:
  - Khai báo ở `HDC > Edit configs > Custom keys`, mỗi key có giá trị mặc định cho Android và iOS. Cửa sổ cảnh báo key trống, trùng, trùng key của HDC hoặc sai quy tắc tên.
  - Game đọc bằng `HDCCustomConfig.Get(key)`. Giá trị lấy theo thứ tự Remote Config, giá trị lưu trên máy (`HDCAds.Custom.<key>`), rồi mặc định, và đọc xong trước khi HDCAds khởi tạo. Có `IsReady` và sự kiện `Updated`.
  - Trang Remote Config của bảng debug hiện nguồn và giá trị của từng custom key.
- Native sau interstitial cho group force ad (`androidInterstitials.useNativeAfterInterstitial`, `nativeAfterInterstitialId`, `nativeAfterInterstitialLayout`), trên Android, giống hệ thống cũ:
  - Native full-screen hiện ngay dưới interstitial lúc interstitial hiện. Lệnh show được gọi thẳng trên luồng Android, không chờ Unity đang bị pause.
  - Native chưa load thì load ở lần interstitial đầu và hiện khi load xong. Sau mỗi lần đóng thì tự load lại, nhưng không tự hiện.
  - Doanh thu của native tính cho force ad và position của lần show. App resume không hiện quảng cáo trong lúc native đang trên màn hình.
  - Bảng debug có thêm dòng native sau interstitial của từng group. Config Check báo khi cấu hình không chạy được, và ghi chú rằng iOS không có phần này.
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
- Scene test trong `Tests/Scenes`, để thử thư viện trong project của game như một game thật:
  - `HDCAdsTestBoot` dùng prefab `HDCAdsSetup`, `HDCAdjust` và bảng debug như scene đầu của game, rồi mở `HDCAdsTestGame`.
  - Ở `HDCAdsTestGame`, mỗi kênh một tab, mỗi nút gọi một hàm của API public, với tên group và position lấy từ config đang dùng.
  - Menu `HDC > Test Scenes` mở scene, đưa chúng lên đầu Build Settings hoặc gỡ ra.
- `HDCAds.Testing.Groups(channel)` và `HDCAds.Testing.Positions(channel, group)`: tên group và position của force ad và popup trong config đang dùng, cho công cụ test.
- Module `HDCOnboarding` (assembly `HDC.Onboarding`): splash và intro của hệ thống cũ chạy trên HDCAds.
  - Cùng thứ tự các bước, cách load và show quảng cáo, nội dung config, sự kiện Firebase Analytics và key PlayerPrefs với hệ thống cũ.
  - Hai key Remote Config `splash_config` và `intro_config` của hệ thống cũ gộp thành một key `onboarding_config` có hai phần cùng tên, mỗi phần giữ nguyên JSON cũ.
  - Scene `Splash` và `Intro` dựng sẵn bằng TextMeshPro, prefab `HDCNoInternet`, menu `HDC > Onboarding` (mở scene, đưa lên đầu Build Settings, thêm `onboarding_config` với giá trị mặc định hoặc ghép từ hai custom key cũ).
  - Interstitial và native sau interstitial của splash báo doanh thu qua `HDCAds.Revenue` với kênh `AppLaunch`, position `splash`.
  - Màn Intro là màn ngang như cũ, gồm các trang AL, AR, PT và FORCE_AD; không có bước Profile và Avatar của hệ thống cũ. Những chỗ khác hệ thống cũ ghi trong README, mục Onboarding.
  - Popup bị thư viện native bỏ qua vì app mất focus (hộp thoại "Viewing full screen" ở lần mở đầu) được show lại tới khi hiện; popup của intro chờ native full-screen đóng rồi mới hiện; kênh AppLaunch hoàn tất khi splash xong.
  - Prefab `HDCNativeBanner` (trong scene `Splash`) thay NativeBannerLoader của hệ thống cũ. Nó đọc custom key `first_native_banner_config` (giữ nguyên tên và JSON) và show các group native banner xoay vòng, bắt đầu ở scene `activeSceneName`.
    - Khác bản cũ: quảng cáo đã load mà chưa hiện được show ở lượt sau thay vì bị huỷ; lượt chỉ bị trừ khi banner hiện thật; gỡ quảng cáo thì dừng; chỉ một loader chạy.
    - Menu `HDC > Onboarding > Add config key` đổi thành `Add config keys`, thêm cả `first_native_banner_config`. Build cảnh báo khi thiếu key hoặc `activeSceneName` không có trong Build Settings.
  - `HDCAdTracking` (object trong scene `Splash`) thay module AdTracking của hệ thống cũ: đếm phiên và các lượt có doanh thu của các group hoặc position trong danh sách trên inspector, rồi gửi Firebase Analytics event mới `hdc_ad_paid` (kênh, position, group, định dạng, nguồn, ad unit, doanh thu, số phiên, số lượt, tổng doanh thu). Số đếm tiếp nối key PlayerPrefs của hệ thống cũ. Game nhận cùng dữ liệu qua `HDCAdTracking.Tracked`.
  - Interstitial, native sau interstitial và popup của splash load hoặc show lỗi thì log mã lỗi và thông báo của SDK (`[Splash] Interstitial splash load failed: code=… …`).
  - Thanh loading của splash bắt đầu từ 0% khi màn splash của Unity tắt. Config không có `loadingTime` thì thanh vẫn chạy chậm, không nhảy thẳng lên 90%.
  - Build dừng khi Build Settings có scene dùng `HDCSplash` hoặc `HDCIntro` mà `HDCAdsSettings` chưa khai báo `onboarding_config`. Thiếu key này, splash đọc config trống và đứng mãi ở scene Splash. Build cũng cảnh báo khi giá trị mặc định không có giá trị cho nền tảng đang build, hoặc gọi tới scene trống hay không có trong Build Settings.
- Cửa sổ `HDC > Build Tool` (module `HDCTool`, assembly `HDC.Tool.Editor`, chỉ Editor): build iOS và Android trong hai tab, theo hai preset `Test Debug` và `Release`.
  - Rule của preset (Google test ads và test ad units, bảng debug, debug log, môi trường Adjust, development build, AAB, change log) chỉ áp vào bản copy của scene lúc build, không sửa scene của game. Rule và change log lưu ở `Assets/HDCTool/HDCBuildSettings.asset` của game để dùng chung qua git. Thư mục build lưu riêng từng máy, luôn nằm ngoài project Unity, mặc định `Workspace/Build/{tên project}`: iOS export thẳng vào đó, Android vào thư mục `Android` bên trong; export iOS kiểu Replace giữ lại thư mục `Android` và các báo cáo.
  - Footer `Signing & device`: Team, Bundle ID, `Run on` (iPhone, My Mac hoặc Simulator) và máy, thẳng một cột, cùng các nút build. Danh sách simulator lấy từ `xcrun simctl`; bản cho simulator dùng Simulator SDK và export vào thư mục `Simulator`. Thư viện tĩnh có phần arm64 chỉ cho máy thật (như Firebase C++ của Unity) được đánh dấu lại cho simulator trong bản export, nên bản cho simulator link được trên Mac chip Apple; file trong project không đổi. Target SDK của bản build đi theo `Run on`.
  - Thẻ App có Minimum iOS và Pods minimum (bật mặc định): Podfile của mỗi bản export có bước `post_install` nâng mọi pod thấp hơn lên minimum iOS của app, không phải sửa tay từng pod trong Xcode. Android: API tối thiểu và đích cũng nằm trong thẻ App; hai thẻ Player Settings riêng của iOS và Android đã bỏ.
  - Phase IL2CPP (GameAssembly) của project Xcode khai báo output `libGameAssembly.a`, nên lần build đầu của một bản export mới không còn báo `Build input file cannot be found`.
  - Thẻ App có Orientation (Portrait hoặc Landscape), dùng chung cho iOS và Android.
  - Thẻ App có ô Default Icon như Player Settings để chọn icon mặc định.
  - Thẻ SDK versions: bảng chia nhóm, chỉ có cột của nền tảng đang mở. Mỗi mạng mediation có dòng Adapter và dòng SDK. Cột iOS đọc version đã resolve từ `Podfile.lock` và `Package.resolved` của lần export gần nhất.
  - Kiểm tra trước khi build, có nút sửa nhanh: build target, scene, `HDCAdsSetup` ở scene 0, bảng debug, AdMob app ID, file cấu hình Firebase, version, change log, app token Adjust, key của onboarding, keystore, Team ID của iOS, thư viện không link được cho simulator. Lỗi chặn build.
  - Sửa thẳng Player Settings (version, version code và số build đều có nút `+1`, package hoặc bundle ID, keystore, team của máy chọn từ dropdown như Xcode) và version các SDK. Ô App name đặt tên dưới icon (iOS `CFBundleDisplayName`, Android `app_name`); để trống thì dùng Product name.
  - Build scenes kéo thả được: kéo tay cầm để đổi thứ tự, thả scene từ cửa sổ Project vào để thêm.
  - Giao diện gồm các thẻ có màu riêng, thông tin ở trên cùng: Overview (gồm cả preset và rule của nó), App, iOS hoặc Android, SDK versions, rồi Validation scan, Change log, Build scenes. Mỗi thẻ thu gọn được và máy nhớ trạng thái của nó. Lần đầu mở, cửa sổ chiếm nửa phải và trọn chiều cao của cửa sổ Unity.
  - Nút `Expand all` và `Collapse all` trên thanh công cụ mở hoặc gập mọi thẻ cùng lúc, nút mờ khi không còn gì để làm. Trạng thái thẻ được giữ trong bộ nhớ thay vì đọc EditorPrefs mỗi lần vẽ; thẻ SDK versions và Validation scan chỉ dựng lại các dòng khi dữ liệu đổi. Đo trên một clone, cửa sổ 900 × 1000: một lần vẽ cả cửa sổ mất khoảng 1,1–1,3 ms khi gập hết, 2,3 ms khi mở hết với 3 scene và 2,9 ms với 63 scene, ngang bản trước khi có nút.
  - Android: chọn máy qua adb, build APK hoặc AAB, build rồi cài và mở trên máy, cài, gỡ, mở, dừng app, xoá dữ liệu, xem màn hình qua scrcpy.
  - iOS: chạy trên iPhone đang cắm, My Mac hoặc simulator. Export Xcode (append vào lần export trước khi được), export rồi mở Xcode với máy đã chọn, build và chạy. Build và chạy đi qua Xcode bằng AppleScript, nên chạy được cả iPhone iOS 16 mà `devicectl` không nhận. Swift package (Firebase) của một thư mục export mới được tải trước bằng `xcodebuild`, nên lần chạy đầu không báo `Missing package product`. Dropdown Team lấy danh sách từ các tài khoản trong Xcode.
  - Tên bản build `{mã sản phẩm}_{preset}_{version}_{số build}_{APK|AAB|Xcode}`, kèm báo cáo `_buildreport.txt` cạnh bản build. Build được từ dòng lệnh với `HDC.Tool.Editor.HDCBuildRunner.BuildFromCommandLine -hdcPreset Release`.
- Break ad như prefab Break Ads của hệ thống cũ: kênh `HDCAds.BreakAd` (`IBreakAds`, key BRK) và prefab `HDCBreakAd` (assembly `HDC.Ads.BreakAd`).
  - Config `break_ads_config` nằm trong ad core (`adcore_main_ios`, `adcore_main_android`), cùng JSON với custom key cũ: `adUnitId`, `layoutGroup`, `showBreakAdTime`, `notiBreakAdTime`.
  - Thanh `BREAK AD...` đếm ngược trước mỗi lượt, `LOADING AD...` khi quảng cáo chưa load xong; native full-screen `fa_brk` hiện khi hết đếm ngược.
  - Khác hệ thống cũ: dừng khi gỡ quảng cáo, có doanh thu (force ad, position `break_ad`), không tính thời gian app ở nền, chờ load tối đa `loadingTimeout` giây (mặc định 10), chu kỳ tính từ lúc quảng cáo đóng, chạy cả iOS.
  - Menu `HDC > Break Ad` (thêm prefab vào scene, thêm `break_ads_config` vào config ad core mặc định), tab `BRK` trên bảng debug, tab Break ad và prefab trong scene test.

### Thay đổi

- Popup: `HDCAds.Popup.Reinitialize` không còn huỷ quảng cáo đã load mà chưa hiện, hay lần load đang chạy, mà giữ chúng. Chỉ quảng cáo đã hiện và lần load lỗi mới được load lại.
- Native banner (`first_native_banner_config`): banner mới hiện trước, banner trước chỉ bị ẩn khi banner mới đã lên. Load lỗi hoặc hết `timeOut` thì banner trước vẫn ở lại, vùng banner không còn trống giữa hai lượt.
- Popup của Intro không còn chờ quảng cáo full-screen khác đóng rồi mới gọi show: mỗi quảng cáo là một instance riêng, nên các lệnh show chồng lên nhau được. Thư viện native vẫn đưa popup lên khi màn game trống.
- Splash, iOS: native sau interstitial (`nativeAfterInterId`) hiện ngay bên dưới interstitial như hệ thống cũ và như Android, thay vì chờ interstitial đóng.
  - Native load xong trước hoặc trong lúc interstitial hiện đều được show ngay.
  - Thư viện native đặt nó vào cửa sổ dưới interstitial, đồng hồ chạy ở đó, và đưa nó lên khi interstitial đóng.
  - Quảng cáo mở app kiểu native hay App Open vẫn chờ đóng rồi mới tới native.
- MREC: `Move(GameObject)` lấy tâm khung của RectTransform thay vì pivot, và tự lấy camera theo Canvas của object. MREC đang hiện đi theo object mỗi khi nó di chuyển (animation, xoay màn, đổi safe area), thay vì chỉ cập nhật lúc load, refresh hay `Show`; vị trí chỉ được gửi sang native khi thật sự đổi. Khung có pivot khác (0,5; 0,5) giờ đặt MREC ở tâm khung.
- `HDCAds.Initialize` chờ consent và ATT xong rồi mới áp dụng config quảng cáo và khởi tạo SDK. Consent không cho request quảng cáo thì không khởi tạo SDK, `HDCAds.IsInitialized` giữ `false` và callback của `Initialize` không chạy.
- `HDCAdsSetup`: `maxWaitSeconds` tính từ lúc consent và ATT xong.
- Splash: hai vùng quảng cáo native splash thành prefab riêng (`HDCNativeSplashArea`, `HDCNativeSplashAdMobArea`) nằm ngoài canvas; canvas của scene Splash chỉ còn `Backdrop` và `Header`. Vùng native splash tự chọn theo màn (component `HDCSplashAdArea`): màn dọc cao 7,5%, màn ngang cao 17,5%, full chiều ngang. Scene cũ chuyển bằng `HDC > Onboarding > Move splash ad areas into prefabs`.
- Native banner: `Banner Area` của prefab `HDCNativeBanner` cũng tự chọn theo màn như vùng native splash: màn dọc cao 7,5%, màn ngang cao 17,5%, full chiều ngang (trước đây cố định 15%). Chiều ngang luôn full ở mọi màn dọc và ngang: component luôn giữ anchor ngang từ 0 tới 1, chỉ chiều cao lấy từ các anchor. `HDCSplashAdAreaKind` có thêm `NativeBanner`; splash không bao giờ lấy vùng này làm vùng native splash.
- Thư viện native theo cấu trúc mới của AdsMultiplatform (chia tầng, MVVM, Koin; nhánh `update/refactor`), gồm mọi sửa của nhánh `main` tới `3084fc7` (`hdc-ads-android` 0.4.0 và framework iOS build lại):
  - Android: `hdc-ads-android` kéo thêm `koin-core-jvm` 4.0.0 (Kotlin 2.0, bytecode Java 8). Lớp JNI của bridge đổi package (`com.hdc.adsmultiplatform.unity.bridge.HdcUnityBridge`, `…unity.callback.HdcUnityListener`); `HDCAndroidBridge` đã đổi theo.
  - Popup: `Close` và `Hide` giữ quảng cáo; `Show` sau đó hiện lại đúng quảng cáo đó cho tới khi có quảng cáo mới. Quảng cáo mới chỉ được load theo `reloadTime` khi popup đang hiện. HDCLib không còn tự load popup mới khi `Show` sau khi đóng.
  - Popup iOS: lệnh show chờ dưới quảng cáo fullscreen (interstitial, native full-screen), trang mở từ quảng cáo hay hộp thoại, rồi hiện khi chúng đóng, thay vì hiện đè lên.
  - Popup Android: lệnh show chờ khi game mất focus (interstitial, trang store, hộp thoại) hoặc đang có native full-screen trên game, rồi hiện khi được, thay vì bỏ qua. Nhiều popup hiện cùng lúc được. Popup Android có đếm ngược và nút đóng của layout.
  - Native full-screen iOS: hiện lúc interstitial đang mở thì nằm dưới interstitial và đếm giờ ở đó, rồi lộ ra khi interstitial đóng, như Android. Một instance chỉ có một màn: show lần hai khi màn đầu còn đang hiện bị từ chối.
  - Interstitial: có sự kiện Impression trên cả Android và iOS; lỗi lúc show báo là `ShowFailed` thay vì `LoadFailed`.
  - Sự kiện Paid của mọi định dạng mang precision của SDK.

- Toàn bộ thư viện quảng cáo dời vào thư mục `HDCAds/` (Runtime, Plugins, Editor, Setup, Adjust, Debug, Tests), để chừa chỗ cho module mới cạnh nó.
  - GUID, tên assembly và namespace giữ nguyên, nên code game, scene và prefab không phải sửa.
  - Lệnh xuất thư viện từ KMP giờ dùng `-PunityPluginsDir=<HDCLib>/HDCAds/Plugins`.
  - File dependencies sinh ra ở chỗ cũ (`Editor/HDCAdsDependencies.xml`, không nằm trong git) được tự xoá khi mở project.
- Mã nguồn chia thành các tầng Api, Logic, Domain, Diagnostics, Ports, Infrastructure và Composition.
  - Mạng quảng cáo (AdMob và native) là adapter sau `IAdNetwork`.
  - Partner mediation nằm sau `IMediationPartner`.
  - Mỗi kênh mang module debug và luật kiểm config của riêng nó.
- Khi SDK sẵn sàng, các kênh khởi động theo thứ tự AL, AR, RW, FA, BN, MREC, PU. Trước đây FA khởi động trước RW.
- Bảng debug bỏ tab CL (collapsible banner), vì HDCLib chưa có kênh này.
- Bảng debug không còn hỏi phía native mỗi giây.
- Bảng debug: `Meta Test Mode On` cũng đăng ký máy làm test device của Google, và `Open Ad Inspector` tự đăng ký máy khi chưa là test device, nên Ad Inspector không còn báo lỗi vì máy chưa là test device. Trên iOS, `HDCAds.Testing.EnableTestDevice()` đăng ký thêm IDFA khi ATT đã cho phép.
- Bảng debug gọn hơn:
  - Mọi thẻ của trang Device thu gọn sẵn, mỗi thẻ có nút `Expand`. `Expand all` và `Collapse all` cạnh `Refresh` mở hoặc gập mọi thẻ của trang đang xem.
  - Chữ `HDC Ads Debug` nhỏ hơn; tab kênh (AL, AR, RW…) nhỏ lại, 8 tab một hàng.
  - Event List của trang Events và Recent Events của trang Ads xếp mỗi sự kiện một hàng: giờ, nhãn loại có màu, kênh và ID, chi tiết.
  - Dựng lại prefab giữ các tùy chọn đã chỉnh trên prefab cũ (blocking mask, sorting order, tùy chọn mở bảng).
- Bảng debug mở và dùng không còn giật (đo trong Editor, MacBook M1 Pro):
  - Lần mở đầu không còn chờ tải font: font tải khi bảng khởi động. Mở lần đầu từ khoảng 470 ms còn khoảng 36 ms; mở lại từ khoảng 14 ms còn khoảng 4 ms.
  - Đóng bảng và chuyển trang tắt Canvas thay vì tắt GameObject; mỗi trang có Canvas riêng. Bảng đóng hoặc trang ẩn thì không vẽ lại. Chuyển sang trang đã mở trước đó còn khoảng 4–6 ms (trang Events trước đây khoảng 48 ms); đóng bảng khoảng 3 ms.
  - Danh sách sự kiện tự xếp hàng và giữ hàng cũ: sự kiện mới ở trang Events từ khoảng 21 ms còn khoảng 3 ms.
  - Chữ của bảng (`HDCDebugText`) nhớ cỡ đã đo, và không layout lại khi giá trị đổi mà cỡ không đổi: làm mới trang Ads khi mở hết thẻ từ khoảng 11 ms còn khoảng 1 ms.
  - Bỏ tự co chữ (best fit) ở các nút; Event List hiện 40 sự kiện (20 khi `Explain`), `Full Screen` vẫn 80 (40).
  - Khi có nhiều sự kiện liền nhau, trang vẽ lại nhiều nhất khoảng 3 lần mỗi giây.
- Dọn kiến trúc, không đổi hành vi:
  - Việc chọn config (Remote Config, giá trị đã lưu, mặc định) và chế độ quốc gia nằm ở một use case trong Logic (`HDCConfigSelection`). Trước đây chúng nằm trong `HDCRemoteConfig` và lặp lại một phần trong `HDCAdsSetup`.
  - Phần đọc vùng và ngôn ngữ của máy tách khỏi phần quyết định, và đứng sau port `IDeviceRegionSource`.
  - Domain và Logic không còn gọi thẳng `Debug.Log`. Logic log qua `IAdsLog` (thêm `Warning` và `Exception`); parse config trả lỗi về cho Logic log. `HDCLayerRulesTests` chặn việc gọi lại `Debug.Log` ở các tầng trong, và chặn `HDCRemoteConfig`, `HDCAdsSetup` hay Infrastructure tự chọn config hoặc tự quyết chế độ quốc gia.
  - `HDCAdjust` được chia nhỏ: component chỉ giữ cài đặt, API public và việc chọn cách khởi động; cấu hình SDK, attribution, doanh thu quảng cáo và mua hàng nằm ở các lớp riêng. API public giữ nguyên.
  - Chính sách của native sau interstitial chuyển lên Logic (`HDCCompanionShow`): load ở lần đầu, hiện khi load xong, chờ native đóng rồi mới huỷ. Infrastructure chỉ giữ đường show trên luồng Android, qua port `IShowWithLeader`.
- Mã nguồn C# của HDCLib không còn comment. Tài liệu nằm trong `README.md`, `ARCHITECTURE.md` và `CHANGELOG.md`.
- Prefab `HDCAdsSetup` chứa sẵn `HDCAdjust` và bảng debug (bật `keepAcrossScenes`) làm object con: kéo Setup vào scene đầu là có đủ ba phần. Scene test `HDCAdsTestBoot` giờ chỉ có `HDCAdsSetup`.
  - Lúc chạy, hai object con tự tách ra làm object gốc rồi giữ qua các scene.
  - Khi có nhiều `HDCAdjust`, bản đã cấu hình (có app token của nền tảng đang chạy, hoặc `startManually`) được dùng, bản chưa cấu hình tự rút. Trước đây bản chạy `Awake` trước thắng, nên `HDCAdjust` trống có thể lấn bản đã điền token.
  - Mỗi lúc chỉ một bảng debug chạy, bảng giữ qua scene được ưu tiên. Menu `HDC > Adjust` và `HDC > Debug panel > Add to open scene` chọn bản có sẵn thay vì thêm bản thứ hai.
  - Scene cũ đã đặt `HDCAdjust` hoặc bảng debug riêng cạnh `HDCAdsSetup` nên chuyển cài đặt sang object con rồi xoá bản riêng.
- `HDCAdsSetup` có thêm trường `runAppLaunch` (mặc định bật, như trước). Tắt nó thì prefab không show app launch và không chờ app launch trước khi mở scene tiếp; scene `Splash` của onboarding tắt trường này.
- Native sau interstitial có thêm cách chờ load: đã gắn vào interstitial mà chưa load xong thì native hiện ngay khi load xong, nếu interstitial vẫn đang hiện. Lệnh show chạy trên luồng Android như trước, và chỉ gọi khi native đã sẵn sàng.
- Thư mục `Demo` (assembly `HDC.Ads.Demo`) được thay bằng scene test trong `Tests/Scenes`. Demo cũ tự nạp config giả, nên không thử được `HDCAdsSetup` hay config của project.
  - Assembly của scene test chỉ vào bản build khi Build Settings có scene test. Trước đây `HDC.Ads.Demo` vào mọi bản build của game có bật HDC.
- Post-process iOS nhúng mọi framework động của pod mà app chưa có, ví dụ `AppLovinSDK` và `AdjustSigSdk`. Trước đây nó chỉ nhúng `FBAudienceNetwork`. Export kiểu Append thay build phase cũ của HDC thay vì thêm phase thứ hai.
- Banner và MREC của AdMob chạy như hệ thống cũ:
  - `Show` lúc chưa có quảng cáo trả `false`. View vẫn ẩn và tự hiện khi load xong; trước đây `Show` trả `true` và hiện view trống ngay.
  - Banner: `Show` chạy cả khi `fullBottom.isEnabled` tắt, miễn `bannerChannel` đang bật. `fullBottom.isEnabled` chỉ quyết định tự load, `Initialize` và `autoShowOnLoad`.
  - Lần load đầu lỗi thì view được dựng lại sau 20, 25, 30, 35 rồi 40 giây; trước đây thử lại sau 2, 4, 8… tới 64 giây.
  - Lệnh load lại unit AdMob dự phòng của banner `fullBottom` giờ dựng lại view thật; trước đây lệnh đó không làm gì.
  - MREC nhớ vị trí của `Move` và đặt lại mỗi lần load, refresh và `Show`. `Move(GameObject)` tính lại vị trí của object mỗi lần.

### Sửa

- Native banner: lần load còn đang chạy khi group tới lượt lần nữa bị huỷ và load lại từ đầu, vì `Reinitialize` huỷ cả lần load đang chạy. Ad unit load chậm hơn một vòng xoay vì thế không bao giờ kịp hiện, mà request cứ tăng. Giờ lượt sau giữ lần load đó.
- Editor: popup load lỗi giờ báo trạng thái `Failed` như thư viện native. Trước đây nó báo `Loading`.
- Android: đọc purpose TCF (`IABTCF_PurposeConsents`) từ SharedPreferences mặc định của app, nơi Google UMP lưu. Trước đây HDC đọc qua `PlayerPrefs`, mà Unity lưu `PlayerPrefs` ở file riêng, nên trên Android luôn đọc ra chuỗi rỗng.
- Bảng debug, iOS: `Clear Data & Quit` giờ xoá cả các suite `NSUserDefaults` của SDK trong `Library/Preferences` (Firebase Installations, Firebase Analytics, Google Mobile Ads `__gads__`…), như Android vẫn xoá. Trước đây chỉ domain của app bị xoá, nên sau khi mở lại, Firebase và Google Mobile Ads vẫn giữ dữ liệu của lần cài trước. Thử trên iOS Simulator: sau khi bấm, mọi domain còn 0 key và chỉ còn file metadata của container. Dòng nhắc trên trang Device nói rõ lựa chọn ATT và quyền thông báo do iOS giữ, app không xoá được.
- MREC nằm đúng chỗ Unity đặt trên mọi máy.
  - Trước đây HDCLib đổi pixel Unity sang point bằng `GetDeviceScale()`, trên iOS là `UIScreen.scale`, trong khi Unity vẽ theo `nativeScale` hoặc Fixed DPI.
  - Theo cách tính đó, MREC đặt giữa màn bị lệch:
    - iPhone 8 Plus: 27 pt sang trái và 48 pt lên trên.
    - iPhone 12/13 mini: 7,5 pt sang trái và 16 pt lên trên.
    - Bật Display Zoom: lệch sang phải và xuống dưới.
    - Android: lệch khi game giảm độ phân giải.
  - Giờ tỉ lệ được đo từ view của Unity trên iOS (hàm mới `HDCAds_ViewMetrics` trong `HDCAdsBridge.mm`) và từ content view trên Android.
  - `SizeInPixels` và khung MREC của bảng debug cũng dùng tỉ lệ này. Trên iPhone 8 Plus, `SizeInPixels` trước đây trả 900 × 750, trong khi MREC chỉ chiếm khoảng 783 × 652 pixel Unity.
- Bảng debug, trang Remote Config: nhãn `Applied` không còn báo sai `Direct`. Trước đây nhãn so config HDCAds đang chạy với lần tải config gần nhất, nên khi config bị tải lần nữa (ví dụ bấm `Init SDK` lúc SDK còn đang khởi tạo) trang báo `APPLIED = DIRECT` kèm "HDCAds.Initialize nhận config khác với giá trị HDCRemoteConfig chọn (Remote)", trong khi config đang chạy là của lần tải đầu. Giờ nguồn được chốt đúng lúc `HDCAds.Initialize` nhận config. Dòng ad core hiện `No ad core` khi `ads_config` không có `selectedAdCoreName`, bản build không có Firebase hiện `Default` kèm lý do (cả hai trước đây hiện `Direct`). Nút `Init SDK` ẩn khi HDCAds đang khởi tạo, thay bằng dòng `Đang khởi tạo HDCAds`; bản build không có Firebase không chọn config lần nữa khi đã gọi `HDCAds.Initialize`.
- Remote Config: bấm `Init SDK` trên bảng debug trong lúc config đang tải làm HDC fetch lần thứ hai.
  - Firebase iOS từ chối ngay fetch thứ hai khi fetch đầu chưa xong (`A fetch is already in progress. Ignoring duplicate request.`; bảng debug ghi `Pending (Fetch encountered an error…)`). Vì thế game chạy bằng config đã lưu hoặc mặc định.
  - Trang Remote Config còn trộn số của hai lần tải, ví dụ `Load Time 89.2 s` trong khi `Applied At` trùng `Load Started`.
  - Giờ mỗi lần mở game chỉ tải một lần; lần gọi sau chờ lần đang chạy.
- Remote Config: timeout giờ chỉ đếm thời gian game chạy (mỗi khung hình tính tối đa 1 giây). Lúc app ở nền, hay dưới hộp thoại hệ thống, không làm hết giờ.
  - Fetch đã xong thì luôn được dùng, kể cả khi kết quả tới cùng khung hình với timeout.
  - Trước đây timeout và callback của Firebase chạy ở hai `Update` khác nhau, nên bên nào chạy trước thì thắng.
- Build Tool: bỏ tick Debug panel, Debug log hay đổi rule khác rồi build lại thì bản build vẫn giữ giá trị cũ nếu scene không đổi, vì Unity dùng lại dữ liệu scene đã build lần trước và bỏ qua bước áp rule (đo trên clone: lần build thứ hai không xử lý scene nào, bảng debug vẫn bật). Giờ trong lúc build, tool đặt tạm `Assets/HDCTool/Resources/HDCBuildRules.txt` ghi rule của preset, build xong thì xoá. Rule đổi thì Unity build lại dữ liệu scene, nên rule của Build Tool luôn thắng giá trị trong scene. Rule giữ nguyên thì Unity vẫn dùng lại dữ liệu cũ (export iOS trên clone khoảng 45 giây, như trước).
- Popup trong vùng trải hết chiều ngang màn (như `Banner Area` của native banner) giờ luôn full chiều ngang trên máy thật. Trước đây chiều rộng được đổi từ pixel sang point bằng scale của Google Mobile Ads (`UIScreen.scale`), trong khi Unity vẽ theo `nativeScale`. Theo cách tính đó, popup hụt và hở hai bên trên iPhone 8 Plus (360 pt trên màn 414 pt), iPhone 12/13 mini (360 pt trên 375 pt), khi bật Display Zoom, hoặc khi game giảm độ phân giải (`Screen.SetResolution`, Fixed DPI, cả trên Android). Giờ HDCLib xin hết chiều rộng màn và thư viện native tự giới hạn đúng bằng màn.
- iOS: mọi lỗi và response của Google Mobile Ads (load, preload, show, banner) giờ được đọc ngay trong callback, rồi mới chuyển sang luồng chính. Trước đây vài chỗ đọc ở frame sau, cùng kiểu đã làm Ad Inspector crash.
- iOS: nút Ad Inspector của bảng debug không còn làm app crash (`EXC_BAD_ACCESS` trong `GADUGetAdErrorCode`) khi Ad Inspector báo lỗi, ví dụ máy chưa là test device. Lỗi của plugin Google Mobile Ads chỉ còn sống trong callback, nên mã và nội dung lỗi giờ được đọc ngay trong callback; bảng debug hiện chúng.
- Build Tool: danh sách Build scenes dài không còn làm cửa sổ giật khi cuộn. Trước đây mỗi lần vẽ, mỗi dòng kiểm tra lại file của mọi scene, nên một lượt vẽ mất khoảng 22 ms với 30 scene, 75 ms với 60 scene và 200 ms với 100 scene; giờ kết quả được tính một lần mỗi khi danh sách hoặc project đổi, và một lượt vẽ dưới 0,2 ms.
- Build Tool mở và dùng mượt hơn. Validation scan chạy lại mỗi lần click vào cửa sổ hay đổi một tuỳ chọn; trước đây mỗi lần mất khoảng 100 ms (kiểm tra Onboarding quét phụ thuộc của từng scene ba lần, kiểm tra Firebase duyệt cả thư mục Assets), giờ khoảng 1 ms vì kết quả tìm asset và phụ thuộc của scene được nhớ tới khi project thay đổi. Scene 0 chỉ mở lại để kiểm tra khi file của nó đổi. Team Xcode và phiên bản SDK đọc ở luồng nền, thư viện iOS cho Simulator chỉ quét lại khi project đổi (trước đây cứ 5 giây một lần), file APK mới nhất không còn tìm lại mỗi lần vẽ. Build thật (Build Tool và File > Build Settings) vẫn kiểm tra lại từ đầu.
- Editor: `HDCAdsSettings.asset` và `HDCBuildSettings.asset` không còn bị thay bằng asset mặc định khi Unity đang import.
  - Trước đây, nếu Unity chưa load được asset (ví dụ ngay sau khi biên dịch lại), menu `HDC > Settings asset`, nút `HDC settings` của Build Tool, cửa sổ HDC Ads Settings và Build Tool tạo asset mới đè lên file cũ. Config mặc định và custom key bị thay bằng giá trị mặc định.
  - Giờ chúng không bao giờ tạo đè lên file có sẵn, và không tạo gì khi Unity đang biên dịch hay import. Cửa sổ báo đang chờ và tự hiện lại khi Unity load được asset; build bằng Build Tool dừng với thông báo thay vì tạo asset mới.
- iOS: `HDCAds.Mrec.Move(screenPoint)` và `Move(GameObject)` đặt MREC đúng chỗ trên máy có tai thỏ. Plugin Google Mobile Ads tính vị trí tự chọn từ góc của vùng an toàn, nên trước đây MREC lệch đúng bằng lề đó (62 pt sang phải ở màn ngang iPhone 17, đo trên simulator). Hệ thống cũ cũng lệch như vậy. Android không đổi.
- iOS: export kiểu Append có lúc liệt kê phase Run Script của target GameAssembly hai lần, và Xcode dừng với `Unexpected duplicate tasks`. Post-process giờ bỏ các dòng trùng trong danh sách build phase của mọi target.
- Bảng debug giữ qua các scene (`keepAcrossScenes`) bấm được nút ở scene sau. Trước đây EventSystem do bảng tự tạo nằm ở scene đầu, nên mất khi chuyển scene, và nút của bảng không nhận chạm nữa. Giờ mỗi lần mở, bảng tạo lại EventSystem nếu scene đang chạy không có. Lỗi này lộ ra khi chạy scene test trên emulator.
- Android (thư viện `hdc-ads-android` 0.3.5): game không còn đứng 2 giây khi hỏi trạng thái quảng cáo đúng lúc Unity đang bị pause, ví dụ gọi `CanShow` ngay sau khi show interstitial, hoặc khi người chơi bấm Home.
  - Trước đây luồng Android pause Unity rồi chờ luồng Unity, còn luồng Unity lại chờ luồng Android trả lời câu hỏi. Hai bên chờ nhau đủ 2 giây. Sau đó câu hỏi nhận "chưa sẵn sàng", và Unity log `Timeout (2000 ms) while trying to pause the Unity Engine`.
  - Giờ trong lúc Unity đang bị pause, câu hỏi nhận ngay giá trị mặc định, tức cùng kết quả mà trước đây phải chờ 2 giây mới có.
  - Lệnh đã nhận giá trị mặc định, vì Unity đang pause hoặc vì chờ quá 2 giây, không bao giờ chạy muộn nữa. Trước đây `interstitial.show` quá 2 giây vẫn có thể hiện quảng cáo sau khi HDCLib đã nhận "không hiện".
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
