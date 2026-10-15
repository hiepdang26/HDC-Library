# HDCLib

Thư viện quảng cáo cho Unity. HDCLib gọi thư viện Kotlin Multiplatform của dự án AdsMultiplatform (nhánh `migrate/HDC-Lib`) trên Android và iOS.

Có hai nguồn quảng cáo:

- **Thư viện KMP** (native): interstitial, native fullscreen, popup, native banner. Logic load, waterfall ad unit, hiển thị, pause Unity và bắn sự kiện nằm trong KMP. Phía Unity chỉ gọi một hàm lệnh và nhận một luồng sự kiện.
- **Plugin Google Mobile Ads của Unity**: rewarded, app open, banner view (banner thường và MREC). HDCLib tự quản lý load, retry, show và sự kiện cho các định dạng này.

Game chỉ gọi `HDCAds` (thư mục `HDCAds/Runtime/Api`):

- Tám kênh quảng cáo chạy theo config lấy từ Firebase Remote Config: force ad theo vị trí có capping, rewarded, app launch, app resume, banner, MREC, popup, break ad.
- Sự kiện doanh thu `HDCAds.Revenue` và các công tắc test `HDCAds.Testing`.

Mọi thứ bên dưới (SDK, cầu nối native, config, sự kiện thô của từng quảng cáo) là internal: game không thấy và không phụ thuộc vào chúng. Xem phần [API cho game](#api-cho-game).

Muốn sửa thư viện thì đọc [ARCHITECTURE.md](ARCHITECTURE.md): các tầng, luật phụ thuộc, và các bước thêm mạng quảng cáo, partner mediation, kênh hay nút debug.

## Cấu trúc

```
HDCAds/                        Thư viện quảng cáo: mọi thư mục dưới đây (assembly HDC.Ads.*)
  Runtime/                     Assembly HDC.Ads, chỉ compile khi có define HDC_ADS. Mỗi thư mục là một tầng:
    Api/                       (HDC.Ads) API cho game: HDCAds, interface của 8 kênh, HDCAdRevenue, các enum
    Logic/                     (HDC.Ads.Logic) Kênh và group: khi nào load, show, dùng ad unit dự phòng
      Channels/                8 kênh (IAdChannel), mỗi kênh một class, cùng module debug của nó trong file *.Debug.cs
      Groups/                  Group fallback full-screen và banner, cùng các nguồn ad của chúng
      HDCAdsContext.cs         Thứ các kênh dùng chung: config, cờ gỡ quảng cáo, group, các port
      HDCAdGroups.cs           Kế hoạch của từng slot: mạng nào làm ad nào, theo thứ tự ưu tiên; tạo group từ đó
      HDCPriorityOrder.cs      mediationPriority 0/1/2 và thứ tự backup, viết thành dữ liệu
    Ports/                     (HDC.Ads.Ports) Interface Logic gọi, Infrastructure hiện thực:
                               mạng quảng cáo (IAdNetwork), ad full-screen, view, popup, đồng hồ, lưu trữ, main thread, log, SDK
    Domain/                    (HDC.Ads.Domain) Config, tuỳ chọn hiển thị, sự kiện của SDK
    Diagnostics/               (HDC.Ads.Diagnostics) Cho bảng debug: trạng thái từng ad, nguồn config, mediation,
                               interface module debug của kênh (IChannelDiagnostics) và luật config (IConfigRule)
    Infrastructure/            (HDC.Ads.Infrastructure) HDCAdsSdk, main thread, retry, ID test của Google
      Native/                  Mạng native (HDCNativeNetwork) và ad của nó; cầu nối iOS (DllImport), Android (JNI), Editor (giả lập)
      Gma/                     Mạng AdMob qua plugin GMA (HDCAdMobNetwork): interstitial, rewarded, app open, banner view, MREC
      Mediation/               Partner mediation có cài đặt riêng (IMediationPartner), hiện có Meta Audience Network
    Composition/               (HDC.Ads.Composition) HDCAdsRuntime: lắp Infrastructure vào các port, tạo context và 8 kênh
    Firebase/                  HDCRemoteConfig (assembly HDC.Ads.Firebase, cần define HDC_FIREBASE)
    Settings/                  HDCAdsSettings: config mặc định (assembly HDC.Ads.Settings, luôn được biên dịch)
  Plugins/iOS/                 HDCAds.xcframework, HDCAdsBridge.mm (post-process Xcode tự thêm vào project)
  Plugins/Android/Repository~/ Maven repo chứa thư viện Android hdc-ads-android (Unity bỏ qua thư mục có đuôi "~")
  Editor/                      Menu HDC (bật/tắt, sửa config), post-process Xcode, mẫu Dependencies.xml
  Setup/                       HDCAdsSetup.prefab: khởi động ads ở scene đầu, chứa sẵn HDCAdjust và bảng debug
                               (assembly HDC.Ads.Setup, luôn được biên dịch)
  Adjust/                      HDCAdjust.prefab: khởi động Adjust, đọc attribution, gửi doanh thu quảng cáo lên Adjust
                               (assembly HDC.Ads.Adjust, luôn được biên dịch)
  Debug/                       Bảng debug HDCAdsDebugPanel.prefab (assembly HDC.Ads.Debug, cần define HDC_ADS)
  BreakAd/                     HDCBreakAd.prefab: thanh đếm ngược của break ad (assembly HDC.Ads.BreakAd, luôn được biên dịch);
                               BreakAd/Editor dựng prefab và có menu HDC > Break Ad
  Tests/Editor/                Test tự động Edit Mode (assembly HDC.Ads.Tests, chỉ có trong Editor) và PublicApi.txt
  Tests/Scenes/                Scene test dựng như game thật: HDCAdsTestBoot (HDCAdsSetup, HDCAdjust, bảng debug)
                               mở HDCAdsTestGame (mỗi nút gọi một hàm của API public; assembly HDC.Ads.TestScenes)
HDCOnboarding/                 Splash và Intro của hệ thống cũ, chạy trên HDCAds (xem phần Onboarding)
  Runtime/                     Assembly HDC.Onboarding, cần define HDC_ADS:
    Ads/                       Ad splash: popup native, interstitial và native sau interstitial theo ad unit
    Splash/                    HDCSplash, thanh loading, popup mất mạng, nhãn version và ID máy
    Intro/                     HDCIntro và các view: AL, AR, PT
    Tracking/                  HDCAdTracking: đếm và gửi doanh thu quảng cáo lên Firebase Analytics (hdc_ad_paid)
    NativeBanner/              HDCNativeBannerLoader: native banner xoay vòng theo first_native_banner_config
    Common/                    Key, lần mở đầu, tracking Firebase Analytics, log
  Editor/                      Menu HDC > Onboarding, key config mặc định, builder dựng scene (assembly HDC.Onboarding.Editor)
  Scenes/                      Splash.unity, Intro.unity
  Prefabs/                     HDCNoInternet.prefab, HDCNativeBanner.prefab
  Tests/Editor/                Test Edit Mode và Play Mode (assembly HDC.Onboarding.Tests)
HDCTool/                       Cửa sổ HDC > Build Tool: build iOS và Android theo preset (xem phần Build tool)
  Editor/                      Assembly HDC.Tool.Editor, chỉ có trong Editor, cần define HDC_ADS:
    Model/                     Preset, rule, tên bản build, file settings, version SDK
    Devices/                   adb, xctrace, scrcpy
    Checks/                    Các mục kiểm tra trước khi build
    Build/                     Chạy build, áp rule vào bản copy của scene, báo cáo build
    Window/                    Cửa sổ và từng phần của nó
  Tests/Editor/                Test Edit Mode (assembly HDC.Tool.Tests)
Tools~/                        Script chạy test và biên dịch nhiều cấu hình (Unity bỏ qua thư mục có đuôi "~")
package.json                   Để cài HDCLib như một package (UPM)
ARCHITECTURE.md                Các tầng, luật phụ thuộc, cách thêm mạng, partner, kênh, nút debug
CHANGELOG.md                   Thay đổi theo từng phiên bản
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
- Define `HDC_ADJUST` không cần bật tay. Khi project có Adjust Unity SDK 5 (assembly `AdjustSdk.Scripts`), HDCLib tự thêm define này cho Android, iOS và Standalone mỗi lần Unity nạp lại script, biên dịch xong hoặc bắt đầu build, và tự gỡ khi SDK bị xoá. Define này không phụ thuộc HDC ads, nên Adjust vẫn khởi động khi HDC ads tắt.
- Plugin iOS (`HDCAds.xcframework`, `HDCAdsBridge.mm`) luôn để tắt trong importer. Khi build iOS có `HDC_ADS`, post-process copy framework vào `Frameworks/HDCAds/` và file bridge vào `Libraries/HDCAds/` của project Xcode, rồi thêm cả hai vào target UnityFramework.
  - Cách này chạy giống nhau trên mọi bản Unity, kể cả khi HDCLib là package chỉ đọc.
  - Nếu build không có `HDC_ADS` đè lên (Append) một bản export cũ, post-process gỡ hai file đó ra.

Hai static framework build từ cùng dự án KMP không link chung được vào một app iOS: chúng trùng class Compose UIKit và bridge native. Nếu project còn một framework như vậy (ví dụ của thư viện ads cũ), thì khi HDC bật, post-process iOS sẽ:

- Bỏ framework đó khỏi project Xcode, và xoá bản copy của nó trong thư mục export, để không script nào copy resources của nó đè lên resources của HDCAds.
- Bỏ các file native import framework đó.
- Sinh `Libraries/HDCAds/HDCAdsStandIns.mm`. Các hàm C của những file đó mà code C# gọi được thay bằng hàm không làm gì, nên bản build vẫn link được.

Tính năng native dùng framework đó không chạy trong bản build này, và log build ghi rõ những gì đã bị bỏ ra. Muốn build với framework đó thì tắt HDC (`HDC > Ads > Disable`) rồi export kiểu Replace.

## Scene đầu tiên: prefab HDCAdsSetup

Kéo prefab `HDCAds/Setup/HDCAdsSetup.prefab` vào scene đầu tiên của game, hoặc dùng `HDC > Setup > Add to open scene`. Không cần viết code khởi động.

Prefab có sẵn hai prefab con, nên kéo Setup vào là có đủ:

- `HDCAdjust`: điền app token trên object con này trong scene (xem [phần Adjust](#adjust-prefab-hdcadjust)).
- `HDCAdsDebugPanel`: bảng debug, bật `keepAcrossScenes` để dùng được ở mọi scene sau.
- Lúc chạy, hai object con tự tách ra làm object gốc rồi giữ qua các scene, nên vẫn còn khi scene đầu đóng lại.
- Không dùng phần nào thì bỏ tick object con đó trên instance trong scene.
- Scene cũ đã có `HDCAdjust` hoặc bảng debug đặt riêng cạnh `HDCAdsSetup`: chuyển cài đặt (token, `startManually`…) sang object con rồi xoá bản riêng. Nếu vẫn để cả hai, xem cách chọn ở phần Adjust và phần bảng debug.
- Script của bảng debug chỉ được biên dịch khi bật HDC (`HDC_ADS`). Khi tắt HDC, Unity báo thiếu script trên object con `HDCAdsDebugPanel`; bật lại HDC thì hết.

Prefab làm lần lượt:

1. Load `nextScene` ở nền.
2. Hỏi consent và ATT (xem [Consent và ATT](#consent-và-att)), song song với bước 3.
3. Lấy config. Trên máy thật, config đến từ Remote Config (cần `HDC_FIREBASE`; bước này cũng khởi tạo Firebase), chỗ nào thiếu thì dùng config mặc định. Trong Editor, prefab dùng config mặc định.
4. Khởi tạo HDCAds khi consent và ATT đã xong: Google Mobile Ads (cả plugin Unity và thư viện native, cùng các adapter mediation), rồi bật các kênh. Consent không cho request quảng cáo thì bỏ qua bước này.
   - App launch: kênh này không show quảng cáo nữa (quảng cáo mở app là `launchChannel` của splash) và hoàn tất ngay khi SDK sẵn sàng. Prefab chỉ chờ nó khi `runAppLaunch` bật.
   - Các kênh khác chỉ được bật khi `startAllChannels` đang bật.
5. Chờ consent và ATT xong, rồi chờ app launch xong (khi `runAppLaunch` bật), tối đa `maxWaitSeconds`, rồi mở `nextScene`.

Các trường của prefab:

- `nextScene`: scene mở sau splash, phải có trong Build Settings. Để trống thì ở lại scene hiện tại.
- `maxWaitSeconds` (mặc định 30): quá thời gian này thì vẫn mở scene tiếp, dù ads chưa xong. Thời gian tính từ lúc consent và ATT xong, nên người dùng đọc form bao lâu cũng không bị tính.
- `startAllChannels` (mặc định bật): bật cả các kênh có `autoInit` tắt trong config, để scene sau có sẵn ad.
- `runAppLaunch` (mặc định bật): chờ kênh AppLaunch hoàn tất trước khi mở scene tiếp. Kênh này không còn show quảng cáo nên xong ngay khi SDK sẵn sàng; quảng cáo mở app do splash show. Scene `Splash` của [Onboarding](#onboarding-splash-và-intro) tắt trường này.
- Debug log, Google test ads và Google test ad units không còn sửa trên prefab. Chúng lấy từ preset của [Build tool](#build-tool) (thẻ Overview): mỗi bản build làm từ Build Tool ghi giá trị của preset vào bản copy của scene.
  - Build bằng cách khác, hoặc bấm Play trong Editor, thì cả ba tắt. Ngoại lệ là scene cũ đã bật sẵn trước đây: Inspector của HDCAdsSetup báo và có nút tắt.
  - Google test ads biến máy đang chạy thành test device của Google trước khi load ad. Request vẫn dùng ad unit thật trong config, Google trả về quảng cáo test (tiêu đề có chữ `Test mode`); đây là cách Google khuyên dùng khi test unit thật. Nút `Make Test Device` ở trang Device của bảng debug làm việc tương tự lúc đang chạy.
  - Google test ad units thay mọi ad unit trong config bằng ad unit mẫu của Google (`ca-app-pub-3940256099942544/...`), nên mọi vị trí đều ra quảng cáo test, kể cả khi unit thật không có quảng cáo. Nút `Test Ad Units` ở trang Device của bảng debug bật tắt việc này lúc đang chạy và nhớ cho các lần mở app sau.
  - Khi bật, log có cảnh báo.
- `onAdsReady`, `onFinished`: sự kiện khi HDCAds khởi tạo xong, và ngay trước khi mở scene tiếp.

Assembly của prefab luôn được biên dịch. Khi HDC tắt, prefab chỉ mở scene tiếp theo, nên scene đầu không bị kẹt. Nếu HDCAds đã khởi tạo rồi (ví dụ quay lại scene đầu), prefab bỏ qua bước khởi tạo.

Scene chạy riêng mà không có prefab, như scene test, thì gọi `HDCAdsSetup.InitializeAds()`.

## Consent và ATT

HDCAds hỏi consent (Google UMP, form GDPR ở châu Âu) và quyền theo dõi của iOS (ATT) trước khi khởi tạo SDK quảng cáo, theo thứ tự của hệ thống cũ:

1. Hỏi dịch vụ consent của Google xem máy này có cần hỏi consent không. Đã có consent từ lần trước, hoặc không cần (ngoài vùng GDPR), thì sang bước 3.
2. Hiện form consent của Google và chờ người dùng trả lời.
3. Consent không cho request quảng cáo (`CanRequestAds` = false) thì dừng ở đây:
   - không hỏi ATT, không khởi tạo SDK quảng cáo, không load quảng cáo nào trong phiên này;
   - app launch hoàn tất ngay;
   - splash bỏ quảng cáo (`5_load_ad_skip_consent`) và mở scene tiếp.
4. Trên iOS, hỏi ATT của Apple và chờ trả lời, nếu cả hai điều sau đúng:
   - máy chưa từng được hỏi (`NotDetermined`);
   - consent cho quảng cáo cá nhân hoá (purpose 1, 3 và 4 của TCF), hoặc máy ở ngoài vùng GDPR.

   Câu hỏi chỉ hiện khi app đang active. Nếu đang có hộp thoại hệ thống khác, ví dụ xin quyền thông báo, thì đợi hộp thoại đó đóng rồi mới hỏi.
5. Khởi tạo Google Mobile Ads và thư viện native, rồi bật các kênh.

**Tắt câu hỏi bằng `ads_config`.** Như hệ thống cũ, `ads_config` có hai cờ, mặc định `true`:
- `toggleConsent`: `false` thì không hỏi dịch vụ consent và không hiện form. Coi như người dùng đã đồng ý hết: quảng cáo được request (`CanRequestAds` = true) và `HDCAds.Consent.Status` là `Obtained`.
- `toggleAtt`: `false` thì không bao giờ hỏi ATT và coi như đã đồng ý: `HDCAds.Consent.TrackingStatus` là `Authorized` (Android vẫn `Unsupported`) và quảng cáo vẫn load. Đây là trạng thái HDC báo. Người dùng không được hỏi nên iOS không cấp IDFA; game tự xử lý phần đó.
- Config không có khoá thì coi là `true`, nên config cũ vẫn hỏi như trước.
- `toggleConsent` = false mà `toggleAtt` = true thì ATT vẫn được hỏi, không xét purpose của TCF.
- Cờ được chốt lúc bắt đầu hỏi:
  - dùng `ads_config` của phiên này nếu `HDCAds.Initialize` đã có nó;
  - nếu chưa, dùng cờ đã lưu từ lần mở app trước;
  - lần mở đầu tiên chưa có gì lưu, thì chờ `ads_config` của phiên này (sau Remote Config) rồi mới hỏi.
  - Vì vậy đổi cờ trên Remote Config thì người đã từng mở app nhận giá trị mới từ lần mở sau.
- HDC không xoá consent đã lưu trên máy (`IABTCF_*` từ lần hỏi trước): Google Mobile Ads vẫn áp dụng nó.
- `NSUserTrackingUsageDescription` vẫn được ghi vào Info.plist; không hỏi ATT thì nó không làm gì.
- Bảng debug, thẻ Consent & ATT, hiện hai cờ và nơi lấy chúng. Config Check báo Info khi một cờ là `false`.

**Khi nào bắt đầu và dừng luồng.**
- `HDCAdsSetup` bắt đầu hỏi ngay khi scene đầu chạy, song song với Remote Config, như hệ thống cũ (lần mở đầu tiên thì chờ `ads_config` như trên). Game tự khởi tạo bằng code thì `HDCAds.Initialize` tự bắt đầu hỏi.
- Trong lúc form consent hoặc câu hỏi ATT đang hiện:
  - không quảng cáo nào được load hay show;
  - splash dừng ở bước `4_admob`;
  - `HDCAdsSetup` chưa bắt đầu tính `maxWaitSeconds`.
- Config quảng cáo chỉ được áp dụng sau bước này. Vì vậy lời gọi kênh trong lúc chờ (như `HDCAds.ForceAd.Initialize`) không load gì.

**Lỗi và các lần mở app.**
- Dịch vụ consent không trả lời sau 10 giây, hoặc báo lỗi (ví dụ không có mạng): HDC dùng consent đã lưu trên máy và không hiện form.
  - Lần mở app đầu tiên mà không có mạng thì chưa có consent nào, nên phiên đó không có quảng cáo. Lần mở sau sẽ hỏi lại.
- Mỗi lần mở app đều hỏi lại dịch vụ consent, nhưng form chỉ hiện khi Google yêu cầu. ATT chỉ hỏi một lần cho mỗi lần cài app.

**Test, Editor và cấu hình.**
- Google test ads (`googleTestAds` của preset Build Tool, hoặc `HDCAds.Testing.EnableTestDevice()` gọi trước khi hỏi) làm máy được hỏi như đang ở châu Âu (debug geography EEA), để thấy form khi test. Hệ thống cũ làm giống vậy với `Admob_TestDevice`.
- Trong Editor không hỏi consent và ATT: coi như được phép.
- Nội dung form consent soạn trong AdMob > Privacy & messaging, cho app ID của game.
- Câu hỏi ATT lấy từ `NSUserTrackingUsageDescription` trong Info.plist.
  - Đặt câu của game ở Assets > Google Mobile Ads > Settings > User Tracking Usage Description.
  - Ô này để trống thì post-process của HDC ghi câu mặc định "This uses device info for more personalized ads and content". Đây là câu hệ thống cũ nhận từ AppLovin.
  - Post-process cũng link `AppTrackingTransparency.framework`.

**Tracking consent và ATT.**
- Mỗi bước của consent và ATT là một bước trong funnel splash. Như các bước khác, mỗi bước gửi hai event Firebase: `f_` (lần mở đầu) hoặc `n_` + tên bước, và `a_` + tên bước.
- Các bước gửi ngay sau `4_admob_start`, kể cả bước xảy ra trước khi splash chạy (gửi bù), và mỗi bước chỉ một lần mỗi phiên.
- Android không có `4_att_*`. Trong Editor chỉ có `4_cs_u_s`, `4_cs_u_e`, `4_cs_ok`.

| Bước | Khi nào |
|---|---|
| `4_cs_wait_config` | Lần mở đầu chưa có cờ đã lưu: chờ `ads_config` rồi mới hỏi |
| `4_cs_skip` | `toggleConsent` = false: không hỏi consent, coi như đồng ý (gửi kèm `4_cs_accept`) |
| `4_cs_u_s` | Bắt đầu hỏi dịch vụ consent của Google |
| `4_cs_u_e` | Dịch vụ consent trả lời |
| `4_cs_u_m` | Dịch vụ consent lỗi hoặc quá 10 giây: dùng consent đã lưu trên máy |
| `4_cs_f_s` | Form consent hiện |
| `4_cs_f_e` | Form consent đóng |
| `4_cs_accept` | Người dùng đồng ý trên form: purpose 1, 3 và 4 của TCF đều được đồng ý (thường là nút Consent) |
| `4_cs_partial` | Người dùng đồng ý một phần: có purpose 1 nhưng thiếu purpose 3 hoặc 4 (chỉnh trong Manage options) |
| `4_cs_denied` | Người dùng từ chối: không đồng ý purpose 1 (thường là nút Do not consent) |
| `4_cs_ok` | Consent cho request quảng cáo |
| `4_cs_no` | Consent không cho request quảng cáo: phiên này không có quảng cáo |
| `4_att_skip` | `toggleAtt` = false: không hỏi ATT, coi như đồng ý (gửi kèm `4_att_auth`) |
| `4_att_known` | Máy đã trả lời ATT từ trước, không hỏi lại |
| `4_att_purpose_no` | Không hỏi ATT vì consent không cho quảng cáo cá nhân hoá |
| `4_att_s` | Hộp thoại ATT hiện |
| `4_att_auth` | Người dùng cho phép theo dõi |
| `4_att_denied` | Người dùng từ chối |
| `4_att_restricted` | iOS tự trả lời là bị giới hạn (Screen Time, MDM…) |

Đếm số người: trong Firebase Analytics > Events, cột Users của từng event là số người. Consent: `a_4_cs_accept` là số người đồng ý, gồm cả người được coi là đồng ý vì `toggleConsent` = false (số này là `a_4_cs_skip`); `a_4_cs_partial`, `a_4_cs_denied` là người đồng ý một phần và từ chối trên form. Không dùng `4_cs_ok`/`4_cs_no` để đếm từ chối: bấm Do not consent vẫn cho Google hiện quảng cáo giới hạn, nên vẫn là `4_cs_ok`. ATT: `a_4_att_auth` là số người cho phép, gồm cả người được coi là cho phép vì `toggleAtt` = false (số này là `a_4_att_skip`); `a_4_att_denied`, `a_4_att_restricted` là từ chối và bị giới hạn (ATT chỉ hỏi một lần mỗi lần cài). Bản `f_` chỉ đếm lần mở đầu.

Ví dụ lần mở đầu ở châu Âu, đồng ý consent và cho theo dõi: `4_admob_start`, `4_cs_wait_config`, `4_cs_u_s`, `4_cs_u_e`, `4_cs_f_s`, `4_cs_f_e`, `4_cs_accept`, `4_cs_ok`, `4_att_s`, `4_att_auth`, `4_admob_end`. Tắt cả hai cờ: `4_admob_start`, `4_cs_skip`, `4_cs_accept`, `4_cs_ok`, `4_att_skip`, `4_att_auth`, `4_admob_end`.

**Nút đổi lựa chọn.** Khi `HDCAds.Consent.PrivacyOptionsRequired` là true (người dùng ở vùng GDPR), Google yêu cầu game có nút mở lại form, ví dụ trong màn Settings:

```csharp
privacyButton.gameObject.SetActive(HDCAds.Consent.PrivacyOptionsRequired);
privacyButton.onClick.AddListener(() => HDCAds.Consent.ShowPrivacyOptions());
```

`HDCAds.Consent` có:
- `IsDone`, `CanRequestAds`, sự kiện `Done`;
- `Status` (`HDCConsentStatus`), `TrackingStatus` (`HDCTrackingStatus`);
- `PrivacyOptionsRequired`, `ShowPrivacyOptions(onClosed)`.

Trang Device của bảng debug có thẻ Consent & ATT.

**Khác hệ thống cũ.**
- Consent không cho request quảng cáo thì mọi kênh tắt hẳn trong phiên đó. Hệ thống cũ chỉ bỏ quảng cáo splash; các kênh khác vẫn khởi tạo và load.
- ATT đợi app active rồi mới hỏi. Hệ thống cũ hỏi ngay, nên câu hỏi có thể không hiện khi đang có hộp thoại hệ thống khác.
- Dịch vụ consent không trả lời thì sau 10 giây HDC dùng consent đã lưu. Hệ thống cũ chờ mãi.
- Game gọi được `HDCAds.Consent.ShowPrivacyOptions` để làm nút đổi lựa chọn. Hệ thống cũ có hàm này nhưng game không gọi được.

## Adjust: prefab HDCAdjust

Prefab `HDCAds/Adjust/HDCAdjust.prefab` thay cho prefab Adjust của SDK. Nó khởi động Adjust bằng cài đặt trên inspector, đọc attribution và tự gửi doanh thu quảng cáo của HDC lên Adjust. Cần Adjust Unity SDK 5 trong project (đã thử 5.4.2).

Cách dùng:

1. `HDCAdjust` có sẵn trong `HDCAdsSetup`. Scene không dùng `HDCAdsSetup` thì kéo prefab vào scene đầu tiên, hoặc dùng `HDC > Adjust > Add to open scene` (scene đã có HDCAdjust thì menu chọn bản đó).
2. Điền app token Android và iOS trên object `HDCAdjust` trong scene (con của `HDCAdsSetup`). Đừng sửa prefab gốc trong HDCLib.
3. Xoá prefab Adjust của SDK khỏi scene nếu còn. Nếu prefab đó vẫn tự khởi động Adjust, HDCAdjust để nó khởi động với cài đặt của nó và log cảnh báo; HDCAdjust vẫn gửi doanh thu và đọc attribution.

Prefab tự giữ mình qua các scene (`DontDestroyOnLoad`); nằm trong `HDCAdsSetup` thì nó tách ra làm object gốc trước. Khi scene đầu được mở lại, bản thứ hai tự huỷ, nên Adjust chỉ khởi động một lần.

Khi có nhiều `HDCAdjust`, bản đã cấu hình (có app token của nền tảng đang chạy, hoặc bật `startManually`) được dùng. Bản chưa cấu hình tự rút, kể cả khi nó chạy trước, nên `HDCAdjust` trống trong `HDCAdsSetup` không lấn bản đã điền token đặt riêng.

Các trường giống prefab Adjust của SDK:

| Trường | Mặc định | Ý nghĩa |
|---|---|---|
| `startManually` | tắt | Bật khi game tự gọi `Adjust.InitSdk`. HDCAdjust bỏ qua cài đặt SDK, chỉ đọc attribution và gửi doanh thu. |
| `androidAppToken`, `iosAppToken` | trống | App token của từng nền tảng trên dashboard Adjust. Token trống thì Adjust không khởi động trên nền tảng đó, và log báo lỗi. |
| `environment` | `Auto` | `Auto`: Sandbox trong Editor và bản Development build, Production ở các bản build khác. Có thể chọn cố định `Sandbox` hoặc `Production`. Bản release chạy Sandbox thì log cảnh báo. |
| `logLevel` | `Info` | Mức log của Adjust SDK. `Suppress` tắt log. |
| `coppaCompliance` | tắt | App dành cho trẻ em (COPPA). |
| `sendInBackground` | tắt | Cho Adjust gửi request khi app ở nền. |
| `launchDeferredDeeplink` | bật | Mở deferred deep link của chiến dịch cài đặt. |
| `costDataInAttribution` | tắt | Thêm cost type, amount và currency vào attribution. |
| `linkMe` | tắt | LinkMe: deferred deep link trên iOS, đọc từ clipboard. |
| `defaultTracker` | trống | Tracker cho lượt cài không thuộc chiến dịch nào, ví dụ bản cài sẵn. |
| `preinstallTracking`, `preinstallFilePath` | tắt, trống | Android: đọc chiến dịch cài sẵn của nhà sản xuất máy. |
| `adServices`, `idfaReading`, `skanAttribution` | bật | iOS: Apple Search Ads, đọc IDFA khi người chơi cho phép, SKAdNetwork. |

Các trường riêng của HDC:

| Trường | Mặc định | Ý nghĩa |
|---|---|---|
| `sendAdRevenue` | bật | Gửi doanh thu của mọi quảng cáo HDC lên Adjust. |
| `attributionTimeoutSeconds` | 7 | Số giây chờ attribution sau khi Adjust chạy. Quá thời gian thì `HDCAdjust.Network` là `time_out`. |
| `androidPurchaseEventToken`, `iosPurchaseEventToken` | trống | Event token mà `HDCAdjust.TrackPurchaseRevenue` gửi trên từng nền tảng. |

HDCAdjust làm giống hệ thống cũ:

- Deep link trên Android: link mở app (`Application.absoluteURL` lúc mở và `Application.deepLinkActivated` sau đó) được chuyển cho Adjust, như prefab của SDK. Trên iOS, SDK tự xử lý.
- Doanh thu quảng cáo: mỗi sự kiện `HDCAds.Revenue` thành một `AdjustAdRevenue`:
  - source `admob_sdk`, và `SetRevenue(Value, Currency)`;
  - network là `AdSource` (nguồn mediation, ví dụ `Meta Audience Network`), hoặc `AdMob` khi nguồn trống;
  - unit là `AdUnitId`, placement là `Position`.
- Attribution: HDCAdjust chờ Adjust báo đã bật rồi gọi `Adjust.GetAttribution`. Khi chính HDCAdjust khởi động Adjust, nó nghe thêm `AttributionChangedDelegate`. Mỗi attribution mới thì:
  - lưu vào PlayerPrefs `user_network`, `user_campaign`, `user_creative`, và `user_cost` khi có cost;
  - đặt `HDCAdjust.Network` là tên network đã chuẩn hoá: chữ thường, khoảng trắng thành `_`, bỏ ký tự không phải chữ, số hay `_`, bỏ chữ số đầu, tối đa 30 ký tự. Ví dụ `Facebook Installs` thành `facebook_installs`;
  - phát sự kiện `HDCAdjust.AttributionChanged`.
- Chưa gửi attribution lên Firebase làm user property. Phần này làm cùng Firebase tracking sau.

API:

```csharp
using HDC.Ads;

if (HDCAdjust.IsAttributionReady)
    Debug.Log(HDCAdjust.Network);                              // "facebook_installs", "organic"…
HDCAdjust.AttributionChanged += attribution => Debug.Log(attribution.Campaign);
HDCAdjust.TrackPurchaseRevenue(0.99, "USD", transactionId);    // event token của nền tảng đang chạy
```

- `HDCAdjust.Attribution` (`HDCAdjustAttribution`): `Network`, `Campaign`, `Adgroup`, `Creative`, `ClickLabel`, `TrackerName`, `TrackerToken`, `CostType`, `CostAmount`, `CostCurrency`. Giá trị là `null` cho tới khi có attribution.
- `HDCAdjust.Network`: rỗng khi đang chờ, `time_out` khi quá `attributionTimeoutSeconds`, và thành network thật nếu attribution tới muộn.
- `TrackPurchaseRevenue` trả `false` khi không gửi: thiếu token, scene không có prefab, hoặc không chạy trên Android hay iOS.

Trong Editor, Adjust không chạy. HDCAdjust vẫn nhận doanh thu, và khi bật Debug Log thì log ra thứ nó sẽ gửi. Inspector của prefab báo khi token còn trống hay project chưa có Adjust SDK. Với `Auto`, inspector cho biết bản build tới chạy Sandbox hay Production. Trang Device của bảng debug có trạng thái của HDCAdjust.

## Break ad: prefab HDCBreakAd

Break ad là quảng cáo native full-screen hiện theo chu kỳ trong lúc chơi, có thanh đếm ngược báo trước. Nó làm theo prefab "Break Ads" (`AdBreakDisplay`) của hệ thống cũ.

### Đưa vào game

1. Thêm prefab `HDCAds/BreakAd/HDCBreakAd.prefab` vào scene gameplay: `HDC > Break Ad > Add to open scene` (scene đã có thì menu chọn nó). Break ad chạy khi prefab bật và dừng khi scene của prefab đóng, nên chỉ đặt nó ở scene chơi.
2. Thêm `break_ads_config` vào ad core config trên Remote Config (`adcore_main_ios`, `adcore_main_android`), cùng cấp với `forceAdGroups`:

   ```json
   "break_ads_config": {
     "adUnitId": "ca-app-pub-xxxxxxxxxxxxxxxx/yyyyyyyyyy",
     "layoutGroup": "cls",
     "showBreakAdTime": 60,
     "notiBreakAdTime": 5
   }
   ```

   - `adUnitId`: ad unit native. Trống thì break ad tắt.
   - `layoutGroup`: layout group trong `forceAdLayoutConfig` của cùng ad core. Không có thì native hiện layout mặc định, và Config Check báo.
   - `showBreakAdTime`: số giây giữa hai lần break, mặc định 60. Bằng 0 thì tắt.
   - `notiBreakAdTime`: số giây đếm ngược trước khi quảng cáo hiện, mặc định 5.
   - JSON giống hệt custom key `break_ads_config` của hệ thống cũ: chép nguyên giá trị cũ vào ad core là dùng được.
3. Thử trong Editor: `HDC > Break Ad > Add break_ads_config to the default ad cores` thêm khối trên, với ad unit test của Google, vào config ad core mặc định của project (iOS chỉ khi config iOS không trống).

`ads_config.forceAdChannel.breakAdConfig` của hệ thống cũ không còn được đọc, và `HDCAds.ForceAd` không còn `StartBreakAd`, `StopBreakAd` hay các event break ad.

### Cách chạy

- Đồng hồ đếm thời gian app đang mở, kể cả lúc game pause bằng `Time.timeScale`. Thời gian app ở nền không tính. Một frame dài tính tối đa 1 giây.
- Còn `notiBreakAdTime` giây tới lượt break: quảng cáo bắt đầu load, thanh `BREAK AD...` hiện ở giữa mép trên màn hình với vòng đếm ngược.
- Hết đếm ngược:
  - Quảng cáo đã load thì hiện ngay: native full-screen, instance `fa_brk`, layout lấy từ layout group theo nguồn quảng cáo như các native full-screen khác.
  - Quảng cáo còn đang load thì thanh đổi sang `LOADING AD...` và chờ tối đa `loadingTimeout` giây (mặc định 10, sửa trên prefab). Quá thời gian đó thì bỏ lượt này. Quảng cáo load xong muộn được giữ cho lượt sau.
  - Quảng cáo load lỗi thì bỏ lượt này.
- Lượt sau đếm lại từ 0, tính từ lúc quảng cáo đóng hoặc lượt bị bỏ.
- Quảng cáo full-screen khác hiện (force ad, rewarded, app open…) thì thanh ẩn và đồng hồ về 0. Trong lúc quảng cáo đó còn trên màn hình, đồng hồ không chạy.
- Thanh đếm ngược không nhận chạm, game vẫn chơi được.
- Doanh thu tính cho kênh force ad, position `break_ad` (`HDCAds.Revenue`, nên Adjust cũng nhận).
- Đóng break ad thì capping của force ad tính lại từ lúc đó, như hệ thống cũ.
- Trong lúc break ad trên màn hình, app resume không hiện quảng cáo.
- Gỡ quảng cáo (`HDCAds.SetAdsRemoved(true)`) thì break ad dừng.

### Khác hệ thống cũ

- Config nằm trong ad core (`break_ads_config`), không còn là custom key riêng.
- Gỡ quảng cáo thì dừng. Hệ thống cũ vẫn hiện break ad.
- Có doanh thu. Hệ thống cũ không báo doanh thu của break ad.
- Thời gian app ở nền không tính. Hệ thống cũ đếm ngược theo đồng hồ thật, nên quay lại app có thể gặp quảng cáo ngay.
- `LOADING AD...` chờ tối đa `loadingTimeout` giây. Hệ thống cũ chờ tới khi load xong.
- Chu kỳ tính lại từ lúc quảng cáo đóng. Hệ thống cũ lặp đúng mỗi `showBreakAdTime` giây, kể cả thời gian quảng cáo đang hiện.
- Layout group không có thì dùng layout mặc định. Hệ thống cũ báo lỗi và không hiện quảng cáo.
- Chạy cả Android và iOS.

### Dùng từ code

Prefab chỉ là giao diện. Game muốn giao diện riêng thì dùng `HDCAds.BreakAd` và không đặt prefab:

```csharp
HDCAds.BreakAd.Start();                        // bắt đầu đếm (prefab gọi khi bật)
HDCAds.BreakAd.Stop();                         // dừng, ví dụ ở màn chọn level (prefab gọi khi tắt)
HDCAds.BreakAd.Restart();                      // đếm lại từ 0
HDCAds.BreakAd.StateChanged += state => { };   // Off, Waiting, Countdown, Loading, Showing
float left = HDCAds.BreakAd.CountdownLeft;     // giây còn lại khi State là Countdown
```

Bảng debug có tab `BRK`: trạng thái, thời gian đã đếm, lý do đang tắt, và nút `Start`, `Break Now` (đếm ngược ngay), `Stop`.

## Onboarding: Splash và Intro

Thư mục `HDCOnboarding` là splash và intro của hệ thống cũ, viết lại trên HDCAds. Những thứ sau giữ y như cũ:

- thứ tự các bước và cách load, show từng quảng cáo;
- nội dung config của splash và intro: cùng tên trường và cùng kiểu enum (số hoặc tên) như `splash_config` và `intro_config` cũ;
- sự kiện Firebase Analytics của từng bước (`f_`/`n_` theo lần mở đầu, và `a_`);
- key PlayerPrefs: `user_first_open`, `intro_<số trang>`.

Khác hệ thống cũ, hai key Remote Config `splash_config` và `intro_config` gộp thành một key `onboarding_config`. Bên trong nó là nguyên hai giá trị cũ:

```json
{
  "splash_config": { "loadingTime": 25, "nativeSplashGroup": "native_splash", "...": "..." },
  "intro_config": { "nextSceneName": "Game", "Configs": [ ... ] }
}
```

Chuyển một game từ hệ thống cũ sang: tạo `onboarding_config` trên Remote Config, dán giá trị `splash_config` cũ vào `splash_config` và giá trị `intro_config` cũ vào `intro_config`. HDCLib không đọc hai key cũ nữa. Thiếu phần nào thì phần đó coi như trống: không có `splash_config` thì splash không load quảng cáo, không có `intro_config` thì intro không có trang nào.

Màn Intro hiện là màn ngang (1920×1080) như cũ; bản dọc sẽ làm sau.

Assembly `HDC.Onboarding` chỉ compile khi bật HDC (`HDC_ADS`) và cần TextMeshPro. Nó dùng phần internal của HDCAds, nên không có code nào của game phải gọi thẳng SDK.

### Đưa vào game

1. `HDC > Onboarding > Add config keys` thêm custom key `onboarding_config` và `first_native_banner_config` (key nào đã có thì giữ nguyên).
   - Project đã khai báo custom key `splash_config` và `intro_config` (chép từ hệ thống cũ) thì giá trị của chúng được ghép vào `onboarding_config`. Hai key cũ được giữ lại; xoá chúng khi Remote Config đã có `onboarding_config`.
   - Nếu không, giá trị mặc định lấy theo hệ thống cũ, với ad unit test của Google cho từng nền tảng, và scene game là scene đầu tiên trong Build Settings không thuộc onboarding.
   - Sửa lại ở `HDC > Edit configs > Custom keys`.
   - `first_native_banner_config` mặc định là các loop của hệ thống cũ (xem [Native banner](#native-banner-first_native_banner_config)), bắt đầu ở scene game.
   - Thiếu `onboarding_config`, splash đọc config trống: không load quảng cáo và đứng mãi ở scene Splash. Vì vậy build dừng khi Build Settings có scene dùng `HDCSplash` hoặc `HDCIntro` mà key chưa được khai báo. Build cũng cảnh báo khi giá trị mặc định của nền tảng đang build trống, hoặc gọi tới scene trống hay không có trong Build Settings.
2. `HDC > Onboarding > Add scenes to Build Settings` đưa `Splash` và `Intro` (thư mục `HDCOnboarding/Scenes`) lên đầu Build Settings. Tên scene giữ như cũ, vì `firstOpenNextScene` trên Remote Config gọi `Intro` theo tên.
3. Config quảng cáo cần có mọi vị trí mà `onboarding_config` nhắc tới. Như hệ thống cũ, group và position của mỗi vị trí đặt cùng tên:
   - popup: `nativeSplashGroup` (mặc định `native_splash`) và `GroupName` của các trang intro dùng popup;
   - force ad: `GroupName` của các trang dùng force ad, như trang `FORCE_AD`;
   - layout group `nativeAfterLayoutGroup` (mặc định `cls`) trong `forceAdLayoutConfig`.
4. Thay ảnh nền và màu trong hai scene nếu cần. Ảnh của các trang intro vẫn nạp từ `Resources/Intro/<tên ảnh>`.

Scene `Splash` có:

- `HDCAdsSetup`, với ba trường khác mặc định:
  - `nextScene` trống: splash tự mở scene tiếp;
  - `startAllChannels` tắt: như hệ thống cũ, intro tự load quảng cáo của nó;
  - `runAppLaunch` tắt: quảng cáo mở app là `launchChannel` trong `splash_config`, do splash show. Kênh AppLaunch của core không show quảng cáo; nó hoàn tất (`HDCAds.AppLaunch.Completed`, `IsCompleted`) ngay khi SDK sẵn sàng. `HDCSplash` vẫn đặt `HDCAds.AppLaunch.IgnoreAds = true`.
- `HDCSplash`: luồng splash.
- `HDCNativeBanner` (prefab): native banner xoay vòng của `first_native_banner_config`, xem [Native banner](#native-banner-first_native_banner_config).
- `HDCNoInternet`: popup mất mạng. Khi máy mất mạng, popup hiện và game dừng (`Time.timeScale = 0`) cho tới khi có mạng lại. Popup giữ qua mọi scene.
- Thanh loading (`HDCSplashLoadingView`) và nhãn `v<version>-<ID máy>` (`HDCAppIdLabel`).
  - Như hệ thống cũ, thanh đi từ 0% tới 90%: 30 giây trước khi đọc config, sau đó `loadingTime` giây tính từ chỗ đang đứng. Khi splash load xong quảng cáo, thanh lên 100% trong 0,25 giây rồi mở scene tiếp.
  - Thanh chỉ bắt đầu chạy khi màn splash của Unity (logo Made with Unity) đã tắt.
- Canvas `Canvas` chỉ chứa phần thiết kế: `Backdrop` và `Header` (thanh loading). Vùng quảng cáo là prefab riêng nằm ngoài canvas, mỗi prefab có canvas của nó (sorting order 1):
  - `HDCNativeSplashArea`: vùng `Native Splash Area`, full chiều ngang ở đáy màn hình. Component `HDCSplashAdArea` tự chọn theo màn: màn dọc cao 7,5% (anchor `(0, 0)`–`(1, 0.075)`), màn ngang cao 17,5% như scene cũ (`(0, 0)`–`(1, 0.175)`). Vùng đổi lại khi xoay màn, và cả trong Editor khi đổi khung của cửa sổ Game. Hai bộ anchor sửa được trên component.
  - `HDCNativeSplashAdMobArea`: vùng `Native Splash AdMob Area`, dùng khi AdMob lấp quảng cáo và `enableAdmobLayout` bật. Vùng này giữ anchor của nó, không đổi theo màn.
  - `HDCSplash` trỏ tới hai vùng này. Trường để trống thì splash tìm `HDCSplashAdArea` đúng loại trong các scene đang mở.
  - Scene Splash có từ trước (vùng còn nằm trong canvas): mở scene, chọn `HDC > Onboarding > Move splash ad areas into prefabs` rồi Save. Lệnh thay hai vùng cũ bằng hai prefab, giữ màu của dải native và khung của vùng AdMob, và Undo được.
  - Thư viện native không cho popup thấp hơn chiều cao tối thiểu của layout. Dải 17,5% ở màn ngang thấp hơn chiều cao đó của layout kiểu MREC như `popup_single_manual_01`, nên popup sẽ cao hơn dải. Group `nativeSplashGroup` cần layout kiểu banner, như config cũ của game.

Scene `Intro` có `HDCIntro` và các view:

- AL: chữ bên trái, quảng cáo bên phải. AR: ngược lại.
- PT: ảnh nền, quảng cáo ở phía dưới.
- Vùng quảng cáo của từng view nằm trong `Ad Areas`.

Hai scene và prefab `HDCNoInternet` do builder dựng từ code. Muốn dựng lại (ví dụ sau khi sửa builder), chạy Unity với `-executeMethod HDC.Onboarding.Editor.HDCOnboardingSceneBuilder.BuildFromCommandLine`. Lệnh này ghi đè các file trong `HDCOnboarding/Scenes` và `HDCOnboarding/Prefabs`.

### Luồng splash

1. Ghi nhận lần mở đầu (`user_first_open`), rồi giữ `HDCSplash` qua các scene.
2. Chờ lần lượt:
   - consent, ATT và SDK quảng cáo (`4_admob_start`, `4_admob_end`; các bước bên trong là `4_cs_*` và `4_att_*`, xem [Consent và ATT](#consent-và-att)); consent không cho request quảng cáo thì chỉ chờ consent;
   - Firebase (`1_firebase_end`);
   - mạng, nếu bật `checkInternet` (`2_internet_start`, `2_internet_end`);
   - Remote Config và SDK (`3_admob_start`, `3_admob_end`).
3. Đọc phần `splash_config` của `onboarding_config` (`4_remote_start`, `4_remote_end`). Consent không cho request quảng cáo thì bỏ quảng cáo splash (`5_load_ad_skip_consent`), mở scene tiếp rồi kết thúc (`6_end_splash`).
4. Load song song (`5_load_ad_start` … `5_load_ad_end`):
   - native splash: popup của group `nativeSplashGroup`, chờ tối đa `nativeSplashLoadTimeOut`. Load được thì show ở vùng native splash, giữ ít nhất `nativeSplashShowTime` giây và tới khi có doanh thu (`5_na_*`);
   - quảng cáo mở app của `launchChannel` (interstitial, native full-screen hoặc App Open, xem bên dưới), chờ tối đa `interLoadAdTimeout`. Nếu `waitNativeSplashLoad` bật thì chờ native splash load xong mới load (`5_it_*`).
5. Load native sau interstitial `nativeAfterInterId`, không chờ (`5_naf_*`). Lần mở đầu, khi native này load xong hoặc lỗi, splash load sẵn popup của trang intro đầu tiên (`5_nap_*`).
6. Mở scene tiếp: `firstOpenNextScene` ở lần mở đầu, `nextScene` các lần sau. Ẩn native splash.
7. Sau `delayShowInterstitials` giây, show interstitial (`5_it_s_s`, `5_it_s_e`), rồi native sau interstitial:
   - Native đã load khi interstitial show: native hiện ngay bên dưới interstitial, như hệ thống cũ.
   - Native load xong trong lúc interstitial đang hiện: native cũng hiện ngay bên dưới.
   - Native nằm dưới interstitial và đồng hồ của nó chạy trong lúc interstitial hiện. Interstitial đóng thì người chơi thấy native ngay.
     - Android: native được show khi interstitial báo Shown, ngay trên luồng Android, vì Unity đứng yên dưới AdActivity.
     - iOS: native được show ngay sau interstitial, không chờ interstitial đóng. Thư viện native đặt nó vào một cửa sổ dưới interstitial và đưa lên trên khi interstitial đóng. Sự kiện load của native tới C# ngay cả khi interstitial đang dừng Unity, nên native load muộn vẫn vào được bên dưới.
   - Quảng cáo mở app không phải interstitial (native full-screen, App Open): native sau đó chờ nó đóng rồi mới hiện.
   - Interstitial không có: chờ native load xong rồi show riêng native.
8. `onSplashFinished`, `6_end_splash`.

Doanh thu của quảng cáo mở app và native sau interstitial báo qua `HDCAds.Revenue` với kênh `AppLaunch`, position `splash`. Native splash là popup nên báo theo kênh `Popup`.

`launchChannel` trong `splash_config` chọn quảng cáo mở app. Nó thay cho `interSplashAdUnitId` cũ, trường này không còn được đọc:

```json
"launchChannel": {
  "launchAdType": 0,
  "forceAdUnit": {
    "admobUnit": { "id": "ca-app-pub-3940256099942544/4411468910" },
    "nativeAdUnit": { "id": "", "layoutGroupName": "" }
  },
  "appOpenUnit": { "admobUnit": { "id": "" } }
}
```

- `launchAdType` 0: interstitial `forceAdUnit.admobUnit.id`, chạy qua thư viện native như trước. Trên Android, native sau interstitial hiện bên dưới nó.
- `launchAdType` 1: native full-screen `forceAdUnit.nativeAdUnit.id`, layout lấy từ `layoutGroupName` trong `forceAdLayoutConfig` của ad core. Native sau interstitial hiện khi nó đóng.
- `launchAdType` 2: App Open `appOpenUnit.admobUnit.id`, chạy qua plugin Google Mobile Ads. Native sau interstitial hiện khi nó đóng.
- Luồng load và show của splash không đổi, các bước vẫn ghi `5_it_*`. Kiểu khác 0, 1, 2 hoặc ad unit trống thì splash bỏ qua quảng cáo này; kiểu sai thì log lỗi.
- Splash chỉ load một quảng cáo, nên `preloadAd` và `adBufferSize` không dùng ở đây.

### Luồng intro

1. Đọc phần `intro_config` của `onboarding_config` (`7_start_intro`).
2. Mỗi trang (`7_start_intro_<số trang>`):
   - Trang có `EnableCompleteIntro` đã xem rồi thì bỏ qua.
   - Sau `TimeDelayPreload` giây, load sẵn quảng cáo của trang sau.
   - Trang AL, AR, PT: show quảng cáo của trang (chờ tới khi load xong), đếm ngược `IntroInterval` giây rồi hiện nút Continue.
   - Trang `FORCE_AD`: show force ad và chờ nó đóng; nếu `PreloadType` là popup thì chỉ show rồi ẩn ngay.
   - Hết trang thì ẩn popup của trang và ghi `intro_<số trang>`.
3. Mở `nextSceneName` (`7_end_intro`).

### Theo dõi doanh thu quảng cáo

Scene `Splash` có object `HDCAdTracking`, thay cho module AdTracking (event `ad_tracking_ltv`) của hệ thống cũ. Component giữ qua mọi scene và chỉ một bản chạy; mở lại scene Splash không đếm thêm.

- Danh sách `trackedAds` trên inspector: tên group hoặc position của các quảng cáo cần theo dõi.
  - Mặc định là quảng cáo của onboarding: `splash` (interstitial và native sau interstitial của splash), `native_splash`, `native_intro1`, `native_intro2`, `native_intro_full`. Game thêm group của mình, ví dụ group force ad giữa màn.
  - Một lượt có doanh thu được tính khi group của nó, hoặc position nếu group không có trong danh sách, trùng một tên trong danh sách. Banner không có group nên dùng `FullBottom`.
- Mỗi lần mở app, số phiên tăng 1.
- Mỗi lượt có doanh thu (paid) của quảng cáo trong danh sách: tăng các số đếm rồi gửi Firebase Analytics event `hdc_ad_paid`. Game cũng nhận được qua `HDCAdTracking.Tracked` (`HDCAdPaid`).

| Tham số | Kiểu | Nội dung |
|---|---|---|
| `ad_name` | chuỗi | Tên trong danh sách đã khớp |
| `ad_channel` | chuỗi | `app_launch`, `app_resume`, `force_ad`, `rewarded`, `banner`, `mrec`, `popup` |
| `ad_position` | chuỗi | Position; `FullBottom` với banner |
| `ad_group` | chuỗi | Group trong config; không gửi khi quảng cáo không thuộc group nào |
| `ad_format` | chuỗi | Một giá trị của `HDCAdFormat`: `interstitial`, `fullscreen`, `popup`, `banner`… |
| `ad_network` | chuỗi | Hiện luôn là `AdMob` |
| `ad_source` | chuỗi | Nguồn mediation, ví dụ `Meta Audience Network` |
| `ad_unit` | chuỗi | Ad unit |
| `value` | số thực | Doanh thu của lượt này, theo `currency` |
| `currency` | chuỗi | Mã tiền tệ |
| `precision` | số nguyên | 0 không rõ, 1 ước tính, 2 publisher cung cấp, 3 chính xác |
| `session_count` | số nguyên | Số lần mở app, tính cả lần này |
| `name_paid_count` | số nguyên | Số lượt có doanh thu của `ad_name`, tính cả lượt này |
| `total_paid_count` | số nguyên | Số lượt có doanh thu của mọi quảng cáo trong danh sách |
| `total_value` | số thực | Tổng doanh thu của mọi quảng cáo trong danh sách, cùng `currency` |

- Chuỗi rỗng không được gửi; chuỗi dài quá 100 ký tự (giới hạn của Firebase) bị cắt.
- Số đếm lưu trong PlayerPrefs: `HDCAdTracking.Session`, `HDCAdTracking.Total`, `HDCAdTracking.Count.<tên>`, `HDCAdTracking.Value.<tiền tệ>`. Máy chưa có các key này mà còn key của hệ thống cũ (`PlayerSessionPlay`, `TotalAdPaid`, `<tên group>`) thì đếm tiếp từ đó, để người chơi cập nhật từ bản cũ không bị đếm lại từ 0.
- Event chỉ được gửi khi project có Firebase Analytics, và không gửi trong Editor. `HDCAdTracking.Tracked` vẫn chạy trong Editor.
- Xem event trên Firebase DebugView: iOS thêm launch argument `-FIRDebugEnabled` vào scheme của Xcode; Android chạy `adb shell setprop debug.firebase.analytics.app <package>`.
- Khác hệ thống cũ: tên event và tham số mới; đếm được cả quảng cáo của splash (hệ thống cũ không đếm, vì chúng tạo thẳng từ ad unit); khớp theo group hoặc position; có thêm tổng doanh thu.

### Native banner (`first_native_banner_config`)

Prefab `HDCNativeBanner` trong scene Splash thay cho NativeBannerLoader của hệ thống cũ. Prefab gồm `HDCNativeBannerLoader` và một canvas riêng, giữ qua mọi scene. Banner hiện trong `Banner Area`: dải dưới cùng màn hình. Component `HDCSplashAdArea` (loại `NativeBanner`) tự chọn chiều cao theo màn, như vùng native splash: màn dọc cao 7,5%, màn ngang cao 17,5%. Chiều ngang luôn full màn ở mọi màn dọc và ngang: component luôn giữ anchor ngang từ 0 tới 1, và popup trong vùng này được xin hết chiều rộng màn (xem mục Popup ở [Config và hành vi các kênh](#config-và-hành-vi-các-kênh)). Muốn đổi chiều cao thì sửa giá trị y của các anchor trong component; giá trị x tự về 0 và 1. Tắt `followOrientation` thì tự sửa RectTransform.

Config là custom key `first_native_banner_config`, giữ nguyên tên và JSON của hệ thống cũ:

```json
{
  "activeSceneName": "Gameplay",
  "bannerLoops": [
    { "isEnabled": true, "groupName": "native_banner", "timeOut": 30, "adShowCount": 2, "defaultTimeConfig": 30, "metaTimeConfig": 60 },
    { "isEnabled": true, "groupName": "native_banner_1", "timeOut": 30, "adShowCount": -1, "defaultTimeConfig": 45, "metaTimeConfig": 45 }
  ]
}
```

- `activeSceneName`: scene mà banner bắt đầu chạy, một lần.
- Mỗi loop là một group popup, position đặt cùng tên như hệ thống cũ:
  - `adShowCount`: số lượt còn lại, `-1` là không giới hạn;
  - `timeOut`: thời gian chờ load tối đa;
  - `defaultTimeConfig`: số giây giữ banner; `metaTimeConfig` thay cho nó khi nguồn quảng cáo có chữ `meta`.

Luồng chạy:

1. Chờ Remote Config và SDK xong. Khi scene `activeSceneName` mở (hoặc đang mở), bắt đầu. `autoInitialize` tắt thì game tự gọi `Initialize()`.
2. Xoay vòng các loop, bỏ qua loop tắt hoặc hết lượt. Mỗi lượt:
   - load group: lần đầu `Initialize`, các lần sau `Reinitialize`, chờ tối đa `timeOut`. `Reinitialize` giữ quảng cáo đã load mà chưa hiện và lần load còn đang chạy, không request lại;
   - show banner mới vào `Banner Area`. Banner trước chỉ bị ẩn khi banner mới đã hiện, nên vùng banner không trống giữa hai lượt. Load lỗi, hết `timeOut` hoặc show không được thì banner trước vẫn ở lại;
   - khi banner hiện thật: trừ 1 lượt và giữ banner `defaultTimeConfig` (hoặc `metaTimeConfig`) giây;
   - load lỗi, hoặc banner không show được: chờ 2 giây, gấp đôi mỗi lần lỗi liên tiếp, tối đa 64 giây. Hết `timeOut` mà chưa load xong thì sang loop tiếp ngay.
3. Dừng khi không loop nào còn lượt, hoặc khi người chơi gỡ quảng cáo (banner đang hiện được ẩn).

Banner tới lượt lúc quảng cáo fullscreen (interstitial, native sau interstitial) đang hiện:

- Thư viện native giữ lệnh show của popup cho tới khi màn game trống: không còn quảng cáo fullscreen, trang mở từ quảng cáo hay hộp thoại ở trên (Android: game có lại focus và không có native full-screen trên game). Popup hiện ngay sau khi chúng đóng.
- Loader vẫn show lại mỗi giây cho tới khi banner hiện. Lượt chỉ tính từ lúc banner hiện.
- HDCLib không chặn hay bắt chờ lệnh show vì đang có quảng cáo khác: mỗi quảng cáo là một instance riêng, nên các lệnh show chồng lên nhau được.

Khác NativeBannerLoader cũ:

- Quảng cáo đã load mà chưa hiện (ví dụ load xong sau `timeOut`) được show ở lượt sau của group đó, không bị huỷ để load lại. Lần load còn đang chạy khi tới lượt sau cũng được giữ, không bắt đầu request mới.
- Banner trước chỉ bị ẩn khi banner mới đã hiện. Bản cũ ẩn nó ngay trước khi show banner mới, kể cả khi lượt mới load lỗi, nên vùng banner bị trống.
- Chỉ trừ `adShowCount` khi banner hiện thật; thời gian giữ tính từ lúc hiện. Bản cũ trừ lượt ngay khi gọi show.
- Một group tới lượt hai lần liền nhau thì banner của nó được ẩn trước khi load lại, vì `Reinitialize` không load lại popup đang hiện.
- Gỡ quảng cáo thì dừng. Chỉ một loader chạy: mở lại scene Splash không tạo thêm.
- `autoInitialize` tắt thì loader không tự chạy cả khi scene `activeSceneName` mở sau đó. Bản cũ vẫn chạy trong trường hợp này.

Config quảng cáo cần có các popup group trong `bannerLoops`, với position cùng tên được bật trong `popupChannel`, và layout dạng banner. Muốn đo doanh thu của chúng thì thêm tên group vào danh sách của `HDCAdTracking`. Khi build, có cảnh báo nếu chưa khai báo `first_native_banner_config`, hoặc `activeSceneName` không có trong Build Settings.

### Khác hệ thống cũ

- Consent không cho request quảng cáo: splash bỏ quảng cáo (`5_load_ad_skip_consent`) như hệ thống cũ. Khác hệ thống cũ, mọi kênh khác cũng tắt (xem [Consent và ATT](#consent-và-att)).
- Người chơi đã mua gỡ quảng cáo (`HDCAds.SetAdsRemoved(true)`): splash bỏ qua quảng cáo và mở scene tiếp. Hệ thống cũ vẫn load và show interstitial cùng native của splash.
- Quảng cáo không show được (ShowFailed) thì bước đó kết thúc luôn. Hệ thống cũ chờ sự kiện đóng mãi.
- Thư viện native bỏ qua lệnh show popup khi app đang mất focus, ví dụ lúc Android hiện hộp thoại "Viewing full screen" ở lần mở app đầu tiên; quảng cáo vẫn giữ để show lần sau. Khi đó onboarding show lại mỗi giây cho tới khi popup hiện, rồi chờ doanh thu như cũ. Nếu không, native splash ở lần mở đầu sẽ không bao giờ hiện và splash chờ doanh thu mãi.
- Native splash đã hiện rồi bị đóng mà chưa có doanh thu thì splash chỉ chờ đủ `nativeSplashShowTime` rồi đi tiếp. Native splash phải show lại nhiều lần thì `nativeSplashShowTime` tính từ lúc nó hiện.
- Popup của intro không chờ quảng cáo full-screen khác (như native sau interstitial của splash) đóng rồi mới gọi show: mỗi quảng cáo là một instance riêng, nên các lệnh show chồng lên nhau được. Thư viện native vẫn chỉ đưa popup lên khi màn game trống (xem [Native banner](#native-banner-first_native_banner_config)), nên popup hiện ngay khi quảng cáo kia đóng.
- Kênh AppLaunch của core không show quảng cáo và hoàn tất ngay khi SDK sẵn sàng. Quảng cáo mở app là `launchChannel` của splash; hệ thống cũ chọn nó ở `comebackChannel` của ad core.
- Không có bước Profile (chọn tuổi, quốc gia) và Avatar sau các trang intro. Phần `Profile` và `Avatar` trong `intro_config` cũ được bỏ qua. Trang có kiểu không view nào hiện (như kiểu PROFILE = 3, AVATAR = 4 của hệ thống cũ) thì log lỗi và bỏ qua, intro vẫn chạy tiếp.
- Popup đã load sẵn khi splash bắt đầu load thì được tính là load xong ngay.
- Interstitial splash load lỗi thì không tự load lại, như ad unit `autoReload` tắt của hệ thống cũ.
- Thiếu layout group cho native sau interstitial: log lỗi rồi đi tiếp. Hệ thống cũ ném exception, splash dừng ở đó.
- Thanh loading chờ màn splash của Unity tắt rồi mới chạy, nên luôn bắt đầu từ 0%. `loadingTime` trống hoặc bằng 0 thì thanh tiếp tục chạy chậm (30 giây), còn hệ thống cũ nhảy thẳng lên 90%.
- Config trống hoặc sai JSON: log lỗi rồi dùng giá trị trống. Scene tiếp trống hoặc không có trong Build Settings: log lỗi và ở lại scene hiện tại.
- AdTracking (`ad_tracking_ltv`) thay bằng `HDCAdTracking` với event mới `hdc_ad_paid` (xem phần trên).
- NativeBannerLoader (`first_native_banner_config`) thay bằng prefab `HDCNativeBanner` (xem phần Native banner).
- Chưa chuyển sang: PlaySceneLoader và các công cụ inspector của hệ thống cũ.

### Dùng từ code

- `HDCSplash.IsFinished`, `HDCIntro.IsFinished`: splash và intro đã xong. `HDCAds.AppLaunch.Completed` chạy ngay khi SDK sẵn sàng, vì quảng cáo mở app do splash show.
- Sự kiện của `HDCSplash`, gắn trong inspector hoặc qua `FirebaseInitialized`, `ProgressChanged` và `SplashFinished`.
- `HDCFirstSession.IsFirstOpen`: lần mở app đầu tiên.
- `HDCIntro.IsCompleteIntro("0")`, `HDCIntro.CompleteIntro("0")`: trang intro đã xem hay chưa, theo số thứ tự trang (key `intro_<số trang>`).
- `HDCOnboardingTracking.Tracked`: chạy cho mỗi bước tracking, với tên bước chưa có tiền tố. Sự kiện Firebase Analytics chỉ được gửi khi project có SDK Firebase Analytics.
- `HDCOnboardingKeys`: tên key Remote Config (`onboarding_config`) và các key PlayerPrefs.
- `HDCOnboardingConfig.Parse(json)`: đọc một giá trị `onboarding_config`, ví dụ để kiểm tra config trước khi đưa lên Remote Config.

## Config mặc định

Config mặc định là giá trị dùng tới khi Remote Config có giá trị riêng. Nó nằm trong asset `Assets/HDCAds/Resources/HDCAdsSettings.asset` (`HDC > Settings asset`, hoặc bấm đúp vào asset, mở cửa sổ HDC Ads Settings).

Trong Editor, `HDCRemoteConfig` dùng thẳng config mặc định, không fetch Firebase. Sửa config xong bấm Play là thấy ngay. Muốn fetch Remote Config thật trong Editor thì đặt `HDCRemoteConfig.FetchInEditor = true` trước khi gọi.

- Cửa sổ `HDC Ads Settings` gom mọi thứ của asset vào 4 thẻ có màu: Ads config (xanh dương), Ad core (tím), Custom keys (xanh ngọc) và Country mode (cam). Mở bằng `HDC > Settings asset`, các mục `HDC > Edit configs > …`, bấm đúp vào asset, hoặc nút `Open` của asset.
  - Phím tắt **Shift+;** (Shift và phím chấm phẩy) mở cửa sổ. Bấm lại khi cửa sổ đang mở thì chỉ đưa nó lên trước và focus: không đóng, không đổi nền tảng hay thẻ đang xem. Đóng bằng nút `X` của cửa sổ.
  - Cửa sổ mở ở giữa cửa sổ Unity, rộng 960 px để thấy đủ các nút, cao bằng 80% cửa sổ Unity.
  - Cửa sổ chỉ hiện config của **một nền tảng**; nhãn màu cạnh tiêu đề ghi `iOS` hay `Android`.
    - Mở từ Build Tool (nút `HDC settings`, nút sửa của Validation scan): nền tảng của tab đang mở.
    - Mở cách khác (Shift+;, menu HDC, bấm đúp vào asset): nền tảng đang active trong Build Settings. Hai mục menu `Ad core Android/iOS configs` mở đúng nền tảng ghi trong tên.
  - Thanh trên cùng có các nút màu:
    - `Save all` (xanh lá): ghi số mục chưa lưu và lưu tất cả. JSON đang lỗi được giữ lại, không lưu.
    - `Expand all` (xanh dương) và `Collapse all` (tím).
    - `Full width` (xanh ngọc) và `Select asset` (cam).
  - Ngay dưới thanh trên cùng là hàng `Config Source (<nền tảng>)`. Mỗi nền tảng có lựa chọn riêng, áp cho bản build của nền tảng đó:
    - `Remote Config` (mặc định): mỗi lần mở game đều fetch Remote Config, chờ tối đa 30 giây game chạy. Key nào lần đó không lấy được từ Remote Config thì dùng config mặc định bên dưới.
    - `Default`: không fetch Remote Config; lần mở nào cũng dùng config mặc định bên dưới.
    - Đổi lựa chọn cũng là bản nháp: hàng đó hiện `Unsaved changes` và `Save all` đếm thêm một mục cho tới khi lưu.
  - Thẻ mặc định thu gọn; bấm vào thanh tiêu đề để mở. `Expand all` và `Collapse all` mở hoặc gập cả 4 thẻ. Các mục `HDC > Edit configs > …` mở sẵn thẻ và tab tương ứng.
  - Bên phải tiêu đề là trạng thái: `Valid` (thêm `· country set` khi có JSON Country), `Invalid JSON: …` (ghi bản nào lỗi), `Unsaved changes`, số custom key, country mode bật hay tắt.
  - Ads config và Ad core: JSON của nền tảng đang hiện, với tab `Default` / `Country`.
    - Default là config thường. Bản iOS để trống thì dùng bản Android.
    - Country là config cho máy ở nước đích khi bật country mode (xem dưới). Để trống thì máy đó dùng config thường.
    - Chấm màu trên tab cho biết trạng thái JSON: xanh là hợp lệ, xám là trống, đỏ là lỗi, vàng là chưa lưu.
    - `Save all` lưu cả thay đổi đã sửa ở nền tảng kia (khi mở lại cửa sổ cho nền tảng khác) nếu chúng còn chưa lưu.
  - Custom keys: mỗi key một tab, `+ Add key` để thêm key. Chọn một tab key thì có ô `Key` (đổi tên), nút `Remove`, và tab giá trị của nền tảng đang hiện (`Android` hoặc `iOS`) cùng tab `Country`.
  - Country mode: các ô cài đặt xếp thẳng, không có mục xổ xuống; danh sách ghi cách nhau bằng dấu phẩy.
  - Ô văn bản có thanh cuộn riêng, cao tối đa 520 px.
    - Kéo vạch dọc bên phải ô để đổi chiều rộng. Mọi ô dùng chung chiều rộng và máy nhớ lựa chọn.
    - Bấm đúp vào vạch, hoặc bấm `Full width`, để ô rộng hết cửa sổ.
  - Mọi thay đổi (config source, JSON, custom key, country mode) chỉ là bản nháp trong cửa sổ cho tới khi bấm `Save` hay `Save all`; asset thật và Play Mode chưa thấy thay đổi.
    - `Format` căn lề JSON. `Revert` bỏ thay đổi của thẻ hay tab đó. `Save` chỉ lưu JSON hợp lệ.
    - Bấm `X` để đóng cửa sổ khi còn thay đổi chưa lưu thì Unity hỏi `Save` / `Discard` / `Cancel`. Chọn Save mà còn JSON lỗi thì cửa sổ báo tên JSON đó và không đóng, để bạn sửa.
  - Inspector của asset chỉ có nút `Open HDC Ads Settings` và trạng thái 4 thẻ, mỗi dòng có nút `Edit`.
  - Khi Unity chưa load được asset (ví dụ đang import ngay sau khi biên dịch lại), cửa sổ chỉ báo đang chờ và không đụng tới file; nó tự hiện lại khi load được. Chỉ khi chưa có file nào, cửa sổ mới có nút `Create`.
- Prefab HDCAdsSetup tự dùng các config này. Nếu tự khởi tạo bằng code:

```csharp
HDCAdsSettings defaults = HDCAdsSettings.Load();
HDCRemoteConfig.FetchAndInitialize(defaults.AdsConfig, defaults.CoreConfigsByKey(), () => Debug.Log("ads ready"));
```

`CoreConfigsByKey()` đặt core config mặc định dưới mọi key mà ads_config có thể chọn: `adcore_main_android`, `adcore_main_ios` và key ghi trong `selectedAdCoreName`.

### Custom key của game

Ngoài ads_config và ad core config, game khai báo được key Remote Config của riêng mình, như danh sách custom remote config trong configs SO của hệ thống cũ:

- Khai báo ở `HDC > Edit configs > Custom keys`. Mỗi key có giá trị mặc định cho Android và cho iOS; iOS để trống thì dùng giá trị Android.
  - Cửa sổ cảnh báo key không tên, key trùng, key trùng với key HDC đang đọc (`ads_config`, `adcore_main_android`, `adcore_main_ios`), và tên sai quy tắc của Remote Config (chữ, số và `_`, không bắt đầu bằng số).
  - Thay đổi được lưu vào asset khi bấm `Save`, hoặc khi cửa sổ mất focus.
- Game đọc giá trị bằng `HDCCustomConfig.Get("key")`, kết quả là chuỗi; giá trị JSON thì game tự parse, ví dụ bằng `JsonUtility`. Gọi trên main thread.
  - `TryGet` trả `false` cho key chưa khai báo. Key chưa khai báo đọc ra chuỗi rỗng, và log cảnh báo một lần.
  - `HDCCustomConfig.Keys` liệt kê các key đã khai báo.
- Giá trị lấy từ lần fetch Remote Config của chính lần mở game đó. Key không có giá trị, lần đó không fetch được, hoặc `Config Source` là `Default`, thì dùng giá trị mặc định. Giống hệ thống cũ, chuỗi rỗng trên Remote Config được coi là chưa có giá trị.
  - HDC không lưu giá trị lên máy để dùng cho lần mở sau. Khác hệ thống cũ: hệ thống cũ lưu giá trị vào PlayerPrefs dưới đúng tên key và dùng lại khi Remote Config không trả lời.
  - Trong Editor chỉ dùng giá trị mặc định, giống ads_config, trừ khi bật `HDCRemoteConfig.FetchInEditor`.
- `HDCRemoteConfig` đọc các key này cùng lúc với ads_config, trước khi khởi tạo HDCAds. Vì vậy trong `onAdsReady` của HDCAdsSetup và ở scene sau, giá trị đã là giá trị mới.
  - `HDCCustomConfig.IsReady` cho biết đã đọc xong. Sự kiện `HDCCustomConfig.Updated` chạy mỗi lần đọc xong; handler ném lỗi thì các handler khác vẫn chạy.
  - Trước khi đọc xong, `Get` trả giá trị mặc định.
  - Project không có Firebase (`HDC_FIREBASE` tắt) thì HDCAdsSetup dùng giá trị mặc định.
- `HDCCustomConfig` nằm trong assembly `HDC.Ads.Settings`, luôn được biên dịch, nên game gọi được kể cả khi tắt HDC ads.

### Chế độ quốc gia

Giống hệ thống cũ, máy ở nước đích (mặc định Việt Nam) có thể nhận bộ config riêng của project thay cho giá trị trên Remote Config:

- Config quốc gia là JSON `Country` (cạnh `Default`) trong thẻ Ads config và thẻ Ad core của cửa sổ HDC Ads Settings, cho từng tab Android và iOS. `HDC > Edit configs > Country configs` mở sẵn thẻ Ads config ở tab Android, JSON Country.
  - Khi bật country mode, máy ở nước đích dùng Country JSON thay cho ads_config hay ad core config của Remote Config.
  - Country JSON của iOS để trống thì dùng bản Android. Bản Android để trống thì máy ở nước đích vẫn dùng config thường.
- Giá trị quốc gia của custom key nằm ở tab giá trị `Country` của từng key, trong thẻ Custom keys. Để trống thì dùng giá trị mặc định của key.
- Cách nhận nước đích sửa ở `HDC > Edit configs > Country check`, mặc định là tắt:
  - `targetCountry`: mã quốc gia, ví dụ `vn`.
  - Máy được coi là ở nước đích khi có một tín hiệu khớp:
    - ngôn ngữ hệ thống (`systemLanguages`);
    - ngôn ngữ của locale (`languageCodes`);
    - vùng của locale (`matchRegion`);
    - tên múi giờ chứa `timezoneNames`, ví dụ `Ho_Chi_Minh`;
    - độ lệch UTC trong `utcOffsets`;
    - trên Android, quốc gia của SIM và của mạng di động.
  - Locale và múi giờ đọc thẳng từ hệ điều hành (Java trên Android, `NSLocale`/`NSTimeZone` trên iOS), không chỉ từ .NET.
  - Khác hệ thống cũ: hệ thống cũ coi mọi máy UTC+7 là Việt Nam, nên Thái Lan và tây Indonesia cũng bị tính. HDC để `utcOffsets` trống theo mặc định; muốn giống hệ thống cũ thì thêm `7`.
- Máy test không bao giờ vào chế độ quốc gia. Danh sách máy test lấy từ hai chỗ:
  - `debugDevices` trong trang Country check;
  - key Remote Config `devices`, dạng `{"debugDevices":["..."]}`, giống hệ thống cũ.
  - ID của máy là ANDROID_ID trên Android và `SystemInfo.deviceUniqueIdentifier` ở nơi khác. Trang Device của bảng debug hiện ID này ở dòng `Device ID`.
- Editor không bao giờ vào chế độ quốc gia, trừ khi bật `simulateInEditor` để thử config quốc gia.
- Ở chế độ quốc gia:
  - `HDCRemoteConfig` dùng config quốc gia cho ads_config, ad core config và custom key, rồi khởi tạo HDCAds bằng chúng.
  - Giá trị Remote Config vẫn được đọc và lưu cho lần sau như thường; config quốc gia thì không lưu.
  - Project không có Firebase thì HDCAdsSetup cũng làm như vậy, nhưng không đọc được key `devices`.
- Trang Remote Config của bảng debug cho biết có đang ở chế độ quốc gia hay không, tín hiệu nào khớp, ID của máy, và ghi nguồn `Country` cho các config đã thay.

## API cho game

```csharp
using HDC.Ads;

// Prefab HDCAdsSetup ở scene đầu đã khởi tạo HDCAds và mở scene tiếp sau app launch.
// Nếu tự làm bằng code: gọi HDCAdsSetup.InitializeAds(). Quảng cáo mở app do splash show (launchChannel của onboarding_config).

HDCAds.ForceAd.Show("gameplay", onDone: ResumeGame);
HDCAds.Rewarded.Show("shop", onRewarded: GiveCoins);
HDCAds.Banner.Show();                               // banner FullBottom, vị trí duy nhất
HDCAds.Popup.Move("popup", popupArea);              // RectTransform; popup chỉ show sau khi đặt vị trí
HDCAds.Popup.Show("popup");
HDCAds.SetAdsRemoved(true);                         // mua gỡ quảng cáo: chặn mọi kênh trừ rewarded
HDCAds.BreakAd.Start();                             // break ad; prefab HDCBreakAd tự gọi khi có trong scene

// Doanh thu của từng lượt hiển thị, kèm kênh và vị trí, để gửi lên analytics.
HDCAds.Revenue += revenue => Debug.Log(revenue);    // "ForceAd gameplay fullscreen 0.0012 USD from AdMob Network"

HDCAds.Testing.EnableTestDevice();                  // chỉ cho bản test
```

- `HDCAds` có:
  - `Initialize`, `IsInitialized`, sự kiện `Initialized`, `IsAdsRemoved`, `SetAdsRemoved`.
  - `Consent`: trạng thái consent và ATT, cùng nút đổi lựa chọn (xem [Consent và ATT](#consent-và-att)).
  - Tám kênh: `ForceAd`, `Rewarded`, `AppLaunch`, `AppResume`, `Banner`, `Mrec`, `Popup`, `BreakAd`. Mỗi kênh là một interface (`IForceAds`, `IRewardedAds`, `IBreakAds`…), game chỉ gọi hàm của interface.
- `HDCAds.Revenue` báo mọi sự kiện paid dưới dạng `HDCAdRevenue`:
  - `Channel` (kênh), `Position` (vị trí trong game; `FullBottom` với banner; rỗng với app launch, app resume, MREC).
  - `Group`: group trong config của force ad và popup; rỗng với các kênh khác.
  - `Format` (một giá trị của `HDCAdFormat`), `Network` (hiện luôn là `"AdMob"`), `AdSource` (nguồn mediation, ví dụ `"Meta Audience Network"`), `AdUnitId`.
  - `Value` theo đơn vị `Currency`, và `Precision` (0 không rõ, 1 ước tính, 2 publisher cung cấp, 3 chính xác).
  - Handler của game ném lỗi thì lỗi được log, các handler khác vẫn nhận.
- `HDCAds.Testing`: `DebugLog`, `EnableTestDevice()` / `IsTestDevice`, `UseTestAdUnits`, Meta test mode, và `Groups` / `Positions` (tên group và position của force ad và popup trong config đang dùng, cho công cụ test). Chỉ dùng cho bản test.
- Gọi API từ main thread của Unity. Callback và sự kiện cũng luôn tới trên main thread.
- Danh sách đầy đủ của API nằm trong `HDCAds/Tests/Editor/PublicApi.txt`. Test hợp đồng API giữ file này luôn đúng với code.

### Bên dưới API

- Load lỗi được thử lại sau 2, 4, 8, 16, 32 rồi 64 giây, và reset khi load được:
  - Rewarded và app open: sau mỗi lần show sẽ load ad mới. Show lúc chưa có ad thì load ngay, trừ khi đang chờ retry.
  - Interstitial: KMP load mọi ad unit cùng lúc và dừng khi tất cả lỗi, nên HDCLib gọi load lại.
  - Banner view (banner AdMob và MREC): lần load đầu lỗi, hoặc không có phản hồi, thì view được dựng lại (huỷ `BannerView` cũ, tạo cái mới rồi load) sau 20 giây, rồi 25, 30, 35 và 40 giây cho mỗi lần lỗi tiếp theo, như hệ thống cũ. Sau khi có ad, view tự refresh theo cấu hình ad unit; refresh lỗi thì giữ quảng cáo đang có. Banner `fullBottom` có `useBackup` thì đổi sang unit dự phòng khi refresh lỗi (xem phần banner ở dưới).
- Rewarded và app open có chế độ `preload`: plugin tự giữ sẵn 1–5 ad và tự load lại.
- App open quá 4 giờ kể từ lúc load thì coi là chưa sẵn sàng.
- `HDCAds.Testing.EnableTestDevice()` đăng ký máy đang chạy là test device của Google Mobile Ads, áp dụng cho mọi định dạng. Trên iOS, máy được đăng ký bằng mã IDFV, và cả IDFA khi người dùng đã cho phép ở ATT.
- Trên iOS, sự kiện tới ngay cả khi quảng cáo fullscreen đang pause Unity. Trên Android, sự kiện phát ra trong lúc quảng cáo fullscreen che game sẽ tới khi Unity chạy lại.
- Trên Android, việc hỏi interstitial hay popup đã sẵn sàng chưa (ví dụ `CanShow`) chờ luồng Android trả lời, tối đa 2 giây. Lúc Unity đang bị pause, ví dụ ngay sau khi interstitial mở hoặc khi người chơi bấm Home, câu hỏi nhận ngay "chưa sẵn sàng" mà không phải chờ.
- Hỗ trợ tắt domain reload (Enter Play Mode Options, mặc định của project Unity 6.6 mới). Mọi state tĩnh của HDCLib (ad, callback, subscriber, kênh) được reset mỗi lần vào Play Mode.
- Trong Editor, SDK được giả lập:
  - Load mất 0,5 giây. Ad unit có chữ `fail` trong ID thì load lỗi với mã 3 (no fill); có chữ `slow` thì load mất 2 giây.
  - Show bắn `Shown`, `Impression`, `Paid`.
  - Interstitial và fullscreen đóng sau 1 giây, popup sau 3 giây. Banner giữ đến khi ẩn. Popup bị `Hide` thì giữ quảng cáo như trên máy thật.
  - Native sau interstitial hiện cùng lúc với interstitial, như trên máy.
  - Mọi quảng cáo giả lập có nguồn `editor` (`adSourceId`), nên `adSourceGroups` có `editor` thì thử được layout theo nguồn của fullscreen trong Editor.
  - Rewarded, app open và banner view dùng quảng cáo mẫu có sẵn của plugin GMA trong Editor.

## Config và hành vi các kênh

Config giữ nguyên key và schema JSON của hệ thống cũ, nên dùng lại được giá trị Remote Config sẵn có:

- `ads_config` có `toggleConsent` và `toggleAtt`: có hỏi consent và ATT hay không (xem [Consent và ATT](#consent-và-att)).
- `ads_config` quy định từng kênh:
  - Có bật không, có tự load không (`autoInit`).
  - Force ad: vị trí, capping, `launchCappingTime`, mức giảm capping theo lượt hiển thị.
  - Banner: `isEnabled` của kênh và `fullBottom` (`isEnabled`, `autoInit`, `autoShowOnLoad`). Banner chỉ còn vị trí FullBottom.
  - Popup: vị trí.
- Config core nằm dưới key do `selectedAdCoreName` chỉ định:
  - Group force ad theo vị trí, ad unit theo thứ tự ưu tiên (`mediationPriority`, `useBackup`), `maxShowCount`, `disablePostInitReload`.
  - Layout group của native fullscreen, asset config.
  - `appResumeChannel`: quảng cáo khi quay lại app (xem App resume bên dưới). `comebackChannel` và `appOpenUnit` ở gốc không còn được đọc.
  - `break_ads_config`: break ad (ad unit, layout group, chu kỳ, đếm ngược). Xem phần [Break ad](#break-ad-prefab-hdcbreakad).
- `admobUnit` chạy qua plugin Google Mobile Ads. `nativeAdUnit` chạy qua thư viện native trên cả Android và iOS (tên cũ `androidUnit` không còn được đọc):
  - Native fullscreen dùng layout ngẫu nhiên trong layout group, không lặp cho tới khi dùng hết.
  - Nếu bật `switchToInterstitialAndroid` thì dùng interstitial.
  - Native sau interstitial (`androidInterstitials.useNativeAfterInterstitial`, `nativeAfterInterstitialId`, `nativeAfterInterstitialLayout`) chỉ áp dụng cho group force ad dùng interstitial, và chỉ trên Android, giống hệ thống cũ. Trên iOS, Config Check ghi chú là phần này không chạy.
- Quảng cáo native luôn hiện đúng layout mà config ghi. Layout mặc định chỉ dùng khi config không ghi layout, hoặc ghi tên layout không tồn tại:
  - Popup (`popupGroups[].nativeAdUnit.layout`): `popup_single_manual_01` tới `15`. Tên cũ `mrec_single_manual_NN` hiện layout `popup_single_manual_NN`. Mặc định là `popup_single_manual_01`.
  - Fullscreen (`forceAdLayoutConfig`): các layout `fs_single_*` của thư viện native. Tên có đuôi `_left` hoặc `_right` (ví dụ `fs_single_cls_03_left`, `fs_single_nav_01_right`) hiện layout gốc. Mặc định là `fs_single_universal_01`.
  - Tên không tồn tại hiện thành lỗi ở thẻ Config Check. Dòng `Layout` của popup trên trang Ads cho biết layout đang dùng.
- Layout theo nguồn quảng cáo (ad source, ví dụ Meta Audience Network hay AdMob Network), như hệ thống cũ:
  - Fullscreen: một layout group có thể có `adSourceGroups`, mỗi phần tử gồm `adSourceIds` và `layouts`. Lúc show, HDCLib lấy `adSourceId` của quảng cáo đã load (thư viện native gửi kèm sự kiện Loaded) rồi chọn ngẫu nhiên một layout của nhóm có nguồn đó. Không nhóm nào có nguồn đó thì dùng `layouts` của layout group. Layout group không có `layouts` thì dùng nhóm nguồn đầu tiên có layout.
  - Popup không chọn layout theo nguồn: mỗi popup luôn dùng `nativeAdUnit.layout`. Thư viện native theo cấu trúc mới (`update/refactor`) đã bỏ `adSourceLayouts`, nên trường này trong config bị bỏ qua.
  - Khác hệ thống cũ: hệ thống cũ chọn layout popup theo nguồn trên Android, HDCLib thì không.
  - Vùng popup giữ chiều cao tối thiểu của layout mà popup hiện.
  - Sự kiện Shown của popup ghi layout và nguồn của quảng cáo đang hiện. Thẻ của ad unit trên trang Ads có dòng `Layout` là layout thật đã hiện.
  - Config Check báo lỗi tên layout không tồn tại trong `adSourceGroups`.
  - Bật Google test ad units không đổi layout: chỉ ad unit được thay.
- Nguồn config của bản build chọn ở hàng `Config Source` của HDC Ads Settings, mỗi nền tảng một lựa chọn:
  - `Remote Config`: mỗi lần mở game, `HDCRemoteConfig` fetch mới và chờ tối đa 30 giây (`HDCRemoteConfig.TimeoutSeconds`). Key có giá trị trong lần fetch đó thì dùng giá trị đó. Key không có, hoặc fetch lỗi, hết giờ, Firebase không chạy được (kể cả khi thiếu thư viện native), thì dùng config mặc định. Giá trị của lần mở trước không được dùng, kể cả giá trị Firebase còn giữ.
  - `Default`: không gọi Firebase, luôn dùng config mặc định.
  - Khác hệ thống cũ: hệ thống cũ chờ 10 giây, rồi dùng giá trị đã lưu trên máy từ lần trước.
  - 30 giây chỉ đếm lúc game đang chạy. Khi app ở nền, hay đang có hộp thoại hệ thống (ATT, cuộc gọi), Unity dừng; mỗi khung hình chỉ được tính tối đa 1 giây. Fetch xong mà còn đang activate thì được chờ thêm tối đa 5 giây.
  - Mỗi lần mở game chỉ tải một lần. Lần gọi tải thứ hai trong lúc đang tải (ví dụ `Init SDK` trên bảng debug) sẽ chờ lần đang chạy, không fetch thêm. Firebase iOS từ chối ngay fetch thứ hai khi fetch đầu chưa xong.
  - Trong Editor nó dùng config mặc định, trừ khi bật `HDCRemoteConfig.FetchInEditor`. Trang Remote Config của bảng debug cho thấy từng key lấy từ nguồn nào, và thời gian của từng bước.
- Giá trị Remote Config là `{}` vẫn được coi là có giá trị. Một ad core config `{}` nghĩa là không có ad unit nào.

Hành vi các kênh:

- Force ad:
  - Chỉ show khi thời gian từ lúc đóng fullscreen gần nhất đạt capping của vị trí. Capping là `cappingTime` trừ số lượt đã hiển thị × mức giảm, không thấp hơn mức tối thiểu. Force ad đầu phiên phải chờ thêm `launchCappingTime`.
  - Số lượt hiển thị theo vị trí lưu trong PlayerPrefs (`fa_count_<vị trí>`).
  - Native sau interstitial (Android) chạy như hệ thống cũ:
    - Khi interstitial hiện, native full-screen (instance `fa_naf_<group>`, layout lấy ngẫu nhiên trong `nativeAfterInterstitialLayout`) hiện ngay bên dưới nó. Lúc interstitial đóng, người chơi thấy native, và đồng hồ của native đã chạy trong lúc interstitial hiện.
    - Lệnh show native được gọi thẳng trên luồng Android lúc interstitial báo đã hiện, không chờ Unity, vì lúc đó Unity đang bị pause.
    - Native chưa load thì lần interstitial đầu bắt đầu load nó, và native hiện ngay khi load xong. Sau mỗi lần đóng, native tự load lại để sẵn cho lần sau. Native load lại xong không tự hiện: nó chỉ hiện khi có interstitial hiện.
    - Doanh thu của native tính cho force ad, đúng position của lần show đó. `onDone` của `Show` vẫn chạy khi interstitial đóng.
    - Trong lúc native đang trên màn hình, app resume không hiện quảng cáo.
    - Layout group không có thì native hiện layout mặc định `fs_single_universal_01`, và Config Check báo. Theo code của hệ thống cũ, trường hợp này native không hiện.
- Group có `useBackup`: ban đầu chỉ load unit đầu. Unit đang dùng mà lỗi load hoặc lỗi show thì load unit kế tiếp. Show lấy unit sẵn sàng đầu tiên.
- Banner (AdMob), như hệ thống cũ:
  - `bannerChannel.isEnabled` tắt, hoặc đã gỡ quảng cáo, thì `Show`, `CanShow` và `Expand` đều bị chặn. `Hide` luôn chạy.
  - `fullBottom.isEnabled` chỉ quyết định banner có tự load (`autoInit`), có nhận `Initialize` và `autoShowOnLoad` hay không. `Show` vẫn chạy khi `fullBottom.isEnabled` tắt, và load banner nếu chưa load.
  - `Show` lúc chưa có quảng cáo trả `false`: view vẫn ẩn và tự hiện khi load xong. `Show` lúc đã có quảng cáo hiện ngay và trả `true`. `Hide` huỷ lần hiện đang chờ.
  - `Hide` chỉ ẩn view, không huỷ. `Show` sau đó hiện lại chính view đó.
  - Banner chỉ có một vị trí, FullBottom: banner adaptive theo chiều ngang màn hình. `fullTop` và bốn góc (`topLeft`, `topRight`, `bottomLeft`, `bottomRight`) của hệ thống cũ đã bị bỏ khỏi `bannerChannel` và `bannerUnit`, và `HDCBannerSlot` không còn.
- MREC (AdMob), như hệ thống cũ:
  - `Show` lúc chưa có quảng cáo trả `false` và hiện khi load xong, giống banner.
  - Vị trí của `Move` được nhớ: gọi trước khi view có cũng được. Vị trí được đặt lại mỗi lần quảng cáo load hoặc refresh, và mỗi lần `Show`.
  - `Move(GameObject)` đặt tâm MREC vào tâm khung của object. Với RectTransform đó là tâm của 4 góc, không phải pivot.
    - Camera lấy theo Canvas của object: Canvas Overlay không cần camera, Screen Space - Camera dùng camera của Canvas, World Space dùng camera của Canvas hoặc `Camera.main`. Camera truyền vào `Move` được ưu tiên.
    - Object không thuộc Canvas mà không có camera truyền vào thì vị trí của object được coi là điểm trên màn hình, như trước.
  - Trong lúc MREC đang hiện, HDCLib đo lại vị trí ở cuối mỗi frame, sau animation, và chỉ đặt lại MREC khi vị trí đổi. Khung chạy animation, xoay màn hay đổi safe area thì MREC đi theo. `Hide` thì ngừng đo.
  - Pixel Unity được đổi sang tọa độ của plugin Google theo tỉ lệ đo thật, nên MREC nằm đúng chỗ Unity đặt trên mọi máy:
    - iOS: kích thước view của Unity (point) và gốc safe area, đúng view mà plugin Google đặt MREC vào. Không dùng `UIScreen.scale`, vì Unity vẽ theo `nativeScale` (khác nhau trên iPhone 6/7/8 Plus, iPhone 12/13 mini, khi bật Display Zoom) hoặc theo Fixed DPI.
    - Android: content view (px) và density, dp tính từ góc cửa sổ. Đúng cả khi game giảm độ phân giải (Fixed DPI, `Screen.SetResolution`).
    - Editor: theo canvas của quảng cáo giả mà plugin Google vẽ trong Editor (cao 1080 đơn vị). Quảng cáo giả có tâm đúng chỗ; khung của nó hơi lớn hơn 300 × 250.
  - `SizeInPixels` trả cỡ thật của MREC theo pixel Unity (300 × 250 pt nhân tỉ lệ đo được), dùng để chừa chỗ cho MREC trong UI.
  - MREC là lớp native nằm trên mọi UI của Unity: sorting order không che được, và MREC nhận mọi chạm trong vùng của nó. Khung đặt MREC cần chừa đúng `SizeInPixels`.
  - Android: plugin Google không cho MREC lấn vào vùng tai thỏ ở cạnh trái và cạnh trên; khung đặt sát vùng đó thì MREC bị đẩy ra.
  - Mặc định MREC ở góc dưới bên phải.
  - Khác hệ thống cũ: gỡ quảng cáo thì MREC đang hiện cũng ẩn (hệ thống cũ chỉ ẩn banner `fullBottom`).
- Rewarded (AdMob), như hệ thống cũ:
  - `Show` khi chưa có quảng cáo trả `false`, không gọi callback, và load lại nếu không đang chờ retry. `IgnoreAds` trao thưởng ngay và trả `true`.
  - Thưởng chỉ có khi SDK báo người chơi đã xem đủ. `onRewarded` rồi `onClosed` chạy lúc quảng cáo đóng.
  - Đóng rewarded thì capping của force ad tính lại từ lúc đó. Rewarded không bị capping chặn.
- Banner `fullBottom` có `useBackup` (banner AdMob và native banner) đổi unit khi refresh lỗi, như hệ thống cũ:
  - Banner trên màn hình refresh lỗi thì unit dự phòng load. Unit dự phòng đã có quảng cáo nhưng quảng cáo đó đã hiện quá 10 giây thì nó load quảng cáo mới. Unit dự phòng có quảng cáo mới thì thay chỗ banner đang lỗi.
  - Native banner có `reloadTime` dưới 20 giây được lỗi một lần mà chưa bị thay. Từ lần lỗi thứ hai mới đổi.
  - Native banner đang hiện, có `reloadTime`, mà không báo load xong hay lỗi trong `max(30, reloadTime + 5)` giây thì tính là một lần refresh lỗi.
  - Unit ưu tiên cao hơn (theo `mediationPriority`) load được quảng cáo mới thì luôn lấy lại chỗ.
  - Unit bị đổi xuống, hoặc unit đang ẩn mà refresh lỗi, thì load lại sau 20, 25, 30, 35 rồi 40 giây. Các lần load lại chỉ chạy trong lúc banner đang hiện.
  - Khác hệ thống cũ: banner AdMob không bị tính là lỗi khi im lặng 30 giây, vì AdMob tự refresh theo chu kỳ đặt trên AdMob console (30–120 giây). Banner AdMob chỉ bị đổi khi SDK báo refresh lỗi.
  - MREC chỉ có unit AdMob, nên không có phần đổi unit này.
  - Thẻ banner trên trang Ads của bảng debug có dòng `Refresh Fails On Screen` (số lần refresh lỗi liên tiếp của banner đang hiện) và `Swaps` (số lần đã đổi unit).
- App launch:
  - Kênh không show quảng cáo: quảng cáo mở app là `launchChannel` của splash (xem Onboarding). `appLaunchChannel` của ads_config, `comebackChannel` và `appOpenUnit` ở gốc ad core không còn được đọc.
  - `Completed` được gọi đúng một lần, ngay khi SDK sẵn sàng. `CanShow` luôn là `false`.
- App resume, cấu hình bằng `appResumeChannel` trong ad core (không còn ở ads_config):

  ```json
  "appResumeChannel": {
    "isEnabled": true,
    "autoInit": true,
    "resumeAdType": 1,
    "forceAdUnit": {
      "admobUnit": { "id": "", "preloadAd": false, "adBufferSize": 0 },
      "nativeAdUnit": { "id": "ca-app-pub-xxx/yyy", "layoutGroupName": "cls" }
    },
    "appOpenUnit": { "admobUnit": { "id": "", "preloadAd": false, "adBufferSize": 0 } }
  }
  ```

  - `resumeAdType` 0: interstitial `forceAdUnit.admobUnit`. 1: native full-screen `forceAdUnit.nativeAdUnit`, layout theo `layoutGroupName`. 2: App Open `appOpenUnit.admobUnit`. `adUnitId` và `layoutGroup` cũ của ads_config ứng với `nativeAdUnit.id` và `nativeAdUnit.layoutGroupName`, với `resumeAdType` 1.
  - Kiểu khác 0, 1, 2 hoặc ad unit của kiểu đã chọn trống: kênh không chạy, Config Check báo lỗi.
  - Load khi app xuống nền và hiện khi load xong. Quảng cáo đã sẵn sàng từ trước (ví dụ có `preloadAd`) thì hiện ngay lúc quay lại.
  - Lần xuống nền kế tiếp bị bỏ qua nếu vừa có fullscreen mở, vừa bấm banner, hoặc game đã gọi `AppResume.Block()`.
- Popup:
  - Phải gọi `Move` (đặt vùng popup) trước khi `Show`. `Move` lúc popup đang ẩn thì lần hiện sau dùng vị trí mới.
  - Vùng trải hết chiều ngang màn (như `Banner Area` của native banner) thì popup luôn full chiều ngang màn: HDCLib xin thư viện native hết chiều rộng màn, thay vì đổi pixel sang point bằng scale của máy. Nhờ vậy popup không bị hở hai bên khi độ phân giải Unity khác scale của hệ thống, ví dụ iPhone 8 Plus, iPhone 12/13 mini, khi bật Display Zoom, hoặc khi game giảm độ phân giải (`Screen.SetResolution`, Fixed DPI). Vùng hẹp hơn màn vẫn đổi theo scale như trước.
  - `Hide` chỉ ẩn popup và giữ quảng cáo. `Show` sau đó hiện lại đúng quảng cáo đó, không load thêm và không tính thêm impression.
  - Quảng cáo bị đóng (nút X trên popup, hoặc `timeShow` hết khi bật tự đóng) cũng được giữ như `Hide`: `Show` lần sau hiện lại đúng quảng cáo đó cho tới khi có quảng cáo mới. Quảng cáo không còn hiện được (load lỗi) thì `Show` hoặc `Initialize` lần sau load quảng cáo mới; `Show` hiện popup ngay khi load xong và trả `true`.
  - `Show` lúc quảng cáo đang load cũng trả `true` và hiện khi load xong. `Hide` huỷ lần hiện đang chờ đó.
  - `Reinitialize(group)` không huỷ quảng cáo đã load mà chưa hiện, cũng không huỷ lần load đang chạy: nó trả `true` và giữ chúng. Chỉ quảng cáo đã hiện (đang ẩn hoặc đã đóng) và lần load lỗi mới được thay bằng lần load mới. Popup đang hiện thì không load lại và trả `false`.
  - `disablePostInitReload` (dòng `Reload After Show` trên bảng debug) chỉ quyết định popup có tự đổi quảng cáo mới theo `reloadTime` trong lúc đang hiện hay không. `reloadTime` bằng 0 thì popup luôn hiện lại quảng cáo cũ.
- Break ad: xem phần [Break ad](#break-ad-prefab-hdcbreakad).
- Rewarded vẫn hiện khi đã gỡ quảng cáo. Các kênh còn lại đều bị chặn.
- Chưa hỗ trợ: collapsible banner. Thư viện quảng cáo không tự gửi doanh thu lên Firebase; game nhận `HDCAds.Revenue` rồi tự gửi, hoặc dùng `HDCAdTracking` của [Onboarding](#theo-dõi-doanh-thu-quảng-cáo). Doanh thu lên Adjust do prefab `HDCAdjust` gửi.

## Bảng debug

Prefab `HDCAds/Debug/HDCAdsDebugPanel.prefab` là bảng debug nằm đè lên game. Chức năng bám theo bảng debug của hệ thống cũ (ad systems, configs, tracking, diagnostics, Adjust, build/device/network), giao diện làm mới, và thêm trạng thái của từng ad unit.

- Bảng có sẵn trong `HDCAdsSetup` và giữ qua các scene. Scene không có `HDCAdsSetup` thì dùng `HDC > Debug panel > Add to open scene` (scene đã có bảng thì menu chọn bảng đó), hoặc kéo prefab vào scene.
- Mỗi lúc chỉ một bảng chạy. Bảng mới tự huỷ khi đã có bảng khác, trừ khi bảng mới giữ qua scene còn bảng đang chạy thì không: khi đó bảng giữ qua scene thay chỗ.
- Bảng mở, đóng và chuyển trang không giật:
  - Khung bảng dựng sẵn khi vào scene và font tải ngay lúc đó, nên lần mở đầu chỉ còn dựng nội dung trang.
  - Đóng bảng hay chuyển trang chỉ tắt Canvas của bảng hoặc của trang, không tắt GameObject, nên mở lại không phải dựng lại. Bảng đóng hoặc trang đang ẩn thì không vẽ lại.
  - Sự kiện mới chỉ dựng hàng của nó, không layout lại cả trang. Giá trị đổi mà không đổi cỡ chữ (ví dụ số giây) cũng không layout lại.
- Bảng ẩn khi vào scene. Mở bằng cách chạm nhanh 3 lần vào góc trên bên trái (14% chiều rộng và chiều cao, trong 0,9 giây), hoặc nhấn F10; đóng bằng nút `Close`.
  - Chạy được với Input Manager cũ lẫn Input System mới. Scene thiếu EventSystem thì bảng tự tạo, với input module hợp loại input của project. Bảng giữ qua scene (`keepAcrossScenes`) cũng tự tạo lại EventSystem khi mở ở scene sau.
  - Bảng nằm trong safe area, và tự đổi tỉ lệ khi màn hình nằm ngang.
  - Các tùy chọn trên prefab: góc mở (`activationCorner`), số lần chạm, `startOpen`, `keepAcrossScenes`.
- Đầu bảng: trạng thái SDK và ad core đang dùng, nút `Refresh`, `Expand all`, `Collapse all`, `Close`, và `Init SDK` khi HDCAds chưa khởi tạo (mở thẳng scene không có HDCAdsSetup; nút gọi `HDCAdsSetup.InitializeAds()`). Trong lúc Remote Config đang tải, đầu bảng ghi `Đang tải Remote Config` kèm số giây, và nút `Init SDK` ẩn đi. Bốn trang: Ads, Remote Config, Events, Device.
  - `Expand all` và `Collapse all` mở hoặc gập mọi thẻ có nút `Expand` của trang đang xem. Trang không có thẻ như vậy (Events) thì hai nút mờ đi.

### Trang Ads

- Tab kênh (nút nhỏ, 8 tab một hàng; kênh thứ 9 trở đi sang hàng mới), mỗi kênh của HDCAds một tab: AL (app launch), AR (app resume), RW (rewarded), FA (force ad), BN (banner), MREC, PU (popup), BRK (break ad). Chấm màu trên tab là trạng thái chung của kênh: xanh lá có ad sẵn sàng, xanh dương đang hiện, cam đang load, đỏ đang lỗi, xám chưa chạy. Tab, nút, thông tin và phần kiểm tra config đều lấy từ module debug của từng kênh, nên kênh mới có tab ngay mà không phải sửa bảng.
- Thẻ Actions: chọn `Group` và `Position` từ config HDCAds đang chạy (trên máy thật là giá trị Remote Config); banner chọn placement, MREC chọn vị trí trên màn hình. Các nút gọi thẳng kênh của HDCAds, đúng object mà `HDCAds.ForceAd`, `HDCAds.Popup`… trả về. Vì vậy chúng chạy cùng code với lời gọi của game, nhưng không đi qua class `HDCAds`. Muốn thử đúng đường gọi của game thì dùng [scene test](#scene-test).
  - `Init` và `Show`. Với banner và MREC, nút `Show` thành `Activate`.
  - `Hide` cho banner, MREC và popup.
  - `UpdatePos` và `GetSize` cho MREC, `UpdatePos` cho popup.
  - Break ad: `Start`, `Break Now` (đếm ngược ngay, không chờ hết chu kỳ) và `Stop`.
- Khung quảng cáo của tab PU và MREC: popup hay MREC hiện đúng trong khung này.
  - Giữ ngón tay trên khung khoảng nửa giây rồi kéo để đưa khung tới bất kỳ chỗ nào trên màn hình; thả ra thì khung nằm yên ở đó. Quảng cáo đang hiện đi theo khung, và bảng mờ đi trong lúc kéo để thấy game phía sau.
  - Popup: kéo một trong bốn núm tròn ở góc để đổi cỡ. Khung không nhỏ hơn cỡ tối thiểu của layout như thư viện native (rộng 300 pt; cao 250 pt với layout có media, 66 pt với layout 10, 11, 13 và 14), nên khung luôn trùng với popup thật. Vị trí và cỡ khung được nhớ cho lần mở sau, riêng màn dọc và màn ngang.
  - MREC chỉ có một cỡ (300 × 250 pt) nên chỉ kéo được. Khung đứng đúng chỗ MREC đang ở, kể cả sau khi chọn vị trí có sẵn rồi bấm `UpdatePos`. Cỡ khung tính theo tỉ lệ đo được như MREC thật.
  - Chạm nhanh hay vuốt trên khung vẫn tới bảng bên dưới; chỉ giữ lâu mới kéo khung.
  - Trên máy, quảng cáo là view native nằm trên Unity: chạm vào quảng cáo là chạm quảng cáo, để thử click. Lúc quảng cáo đang hiện, giữ viền hoặc núm góc quanh nó để kéo.
- Thẻ Detail Information Ad: chỉ group đang chọn (banner: placement đang chọn).
  - Thông tin group từng dòng: positions, priority, backup, số lần show tối đa, ready, units started...
  - Mỗi ad unit một thẻ, theo thứ tự group thử chúng. Nhãn trạng thái: `LOADING`, `READY`/`LOADED`, `SHOWING`, `LOAD FAILED` (kèm thời gian retry), `SHOW FAILED`, `CLOSED`, `BACKUP · NOT STARTED`, `NOT INITIALIZED`.
  - Lỗi load hoặc show gần nhất, mỗi ý một dòng: mã lỗi của SDK cùng tên (ví dụ `3 · NO_FILL`), ý nghĩa bằng tiếng Việt, gợi ý cần kiểm tra gì, ad unit lỗi và thông điệp gốc. Mã lấy theo Google Mobile Ads Android hoặc iOS; `-1` là lỗi của HDC hoặc native, không có mã của SDK.
  - Số liệu từng dòng: requests, loaded, load failed, shows, show failed, impressions, clicks, thời gian load gần nhất, doanh thu, lần retry tới.
  - `Copy Report` chép trạng thái group, sự kiện và system của kênh vào clipboard để gửi cho người khác.
- Thẻ Recent Events (ẩn sẵn): `Expand` mở 15 sự kiện gần nhất của group ngay trong thẻ; `Full Screen` mở toàn màn hình, chữ to, kèm giải thích từng sự kiện.
  - Mỗi sự kiện một hàng: giờ, nhãn loại sự kiện có màu (xanh lá `Loaded`/`Rewarded`, đỏ lỗi, xanh dương `Shown`/`Impression`, vàng `Paid`), rồi ID của ad và chi tiết (nguồn quảng cáo, mã lỗi, doanh thu).
- Thẻ System (ẩn sẵn): `Expand` mở config, trạng thái và các điều kiện chặn của riêng kênh đang chọn, kèm position và group đang chọn, rồi API của kênh.

### Trang Remote Config

- Thẻ Remote Config gồm:
  - config lấy từ đâu (Remote Config hay mặc định) và vì sao, cùng `Config Source` của bản build;
  - trạng thái Firebase và lần fetch gần nhất;
  - thời gian tải, và `Steps`: thời gian của từng bước (Firebase, Fetch, Activation, kết quả);
  - `Extra Load Calls`: số lần gọi tải thêm trong lúc đang tải;
  - ad core key, nguồn của từng key, và lúc áp dụng vào HDCAds.
  - `Load Time` lớn hơn hẳn thời gian game chạy thì dòng đó ghi thêm thời gian game chạy. Phần chênh lệch là lúc app ở nền hoặc dưới hộp thoại hệ thống.
  - Mỗi custom key có một dòng `Custom: <key>`, gồm nguồn (`Remote`, `Default`, `Country`) và đầu giá trị đang dùng.
- Thẻ Config Check: lỗi và cảnh báo của config, bằng tiếng Việt: Firebase không chạy, fetch lỗi, key không có trên Remote Config, ad core config rỗng `{}`, kênh bật mà không có ad unit, position không thuộc group nào, layout group không tồn tại, tên layout không tồn tại, position trùng tên...
- Thẻ Config Viewer: xem `ads_config`, ad core config hoặc mọi key Remote Config đang có (`All Keys`), theo 3 nguồn:
  - `Remote`: giá trị trên Remote Config; `Default`: config mặc định trong `HDC > Edit configs`; `Applied`: config HDCAds đang chạy.
  - Config đang chạy lấy từ nguồn nào được ghi rõ: dòng `APPLIED = REMOTE` (hoặc `DEFAULT`, `COUNTRY`) kèm lý do nằm ngay trên JSON, nút `Applied` hiện `Applied: Remote`, và nút của nguồn đó có chữ `used`. `Direct` nghĩa là code khác tự gọi `HDCAds.Initialize`, không qua Remote Config của HDC.
  - Nguồn của config đang chạy được chốt đúng lúc `HDCAds.Initialize` nhận config, nên lần tải hay lần gọi sau không đổi được nhãn. Dòng ad core hiện `No ad core` khi `ads_config` đang chạy không có `selectedAdCoreName` (không có ad core nào được chọn, nên không có group quảng cáo nào). Bản build không có Firebase (thiếu define `HDC_FIREBASE`) hiện `Default` kèm lý do đó. `Direct` chỉ còn khi game tự gọi `HDCAds.Initialize` với config HDC không tải ra.
  - JSON được thụt dòng và tô màu; các key cấp 1 thu gọn sẵn, bấm để mở từng key hoặc `Expand All`. `Copy` chép JSON đầy đủ, `Full Screen` xem toàn màn hình.
- Thẻ Ad Units Map (ẩn sẵn): ad unit, priority và position của từng kênh, group và placement.
- Trong Editor HDC dùng config mặc định, không gọi Firebase, trừ khi bật `HDCRemoteConfig.FetchInEditor`.

### Trang Events

- Mọi sự kiện quảng cáo của mọi kênh (giữ 300 sự kiện gần nhất): tổng số, số theo loại và tổng doanh thu.
- `Sequential` (mới nhất ở trên) hoặc `Count` (gộp theo loại và ad); lọc theo channel và loại sự kiện; `Explain` thêm giải thích tiếng Việt; `Copy`, `Clear`, `Full Screen`.
- Event List xếp mỗi sự kiện một hàng như thẻ Recent Events, có thêm tên kênh trước ID (`Count`: số lần ở cột đầu). `Copy` và `Full Screen` dùng dạng chữ.
  - Event List hiện 40 sự kiện mới nhất (20 khi bật `Explain`); `Full Screen` hiện 80 (40); `Copy` lấy tất cả.

### Trang Device

- Mọi thẻ của trang thu gọn sẵn; bấm `Expand` trên từng thẻ, hoặc `Expand all` ở đầu bảng. Thẻ đang gập không vẽ lại. `Check Public IP` và `Refresh` (Adjust) tự mở thẻ của chúng.
- Build: version, bundle ID, bản Unity, platform, development hay release, scripting backend.
- HDC Ads: thư viện native, Remote Config có bật không (`HDC_FIREBASE`), trạng thái khởi tạo, ads removed, `Google Test Device` và `Google Test Ad Units`. Các nút:
  - `Debug Log`.
  - `Make Test Device`: máy này thành test device. Request vẫn dùng ad unit thật, Google trả quảng cáo test, áp dụng cho các lần load sau.
  - `Test Ad Units: On/Off`: bật tắt việc thay mọi ad unit bằng ad unit mẫu của Google (`HDCAds.Testing.UseTestAdUnits`). Lựa chọn được lưu trên máy (PlayerPrefs `HDCAds.Debug.TestAdUnits`) và áp dụng từ lúc app mở, trước scene đầu tiên, cho tới khi tắt.
  - `Restart App` hiện ra sau khi đổi `Test Ad Units`, vì quảng cáo đã load vẫn giữ ad unit cũ. Trên Android nút mở lại app từ đầu; trong Editor nút thoát rồi vào lại Play Mode. iOS không cho app tự mở lại, nên nút thành `Quit App`: app thoát, rồi mở lại bằng tay.
  - `Clear Data & Restart` (iOS: `Clear Data & Quit`) luôn có sẵn. Nút xoá toàn bộ dữ liệu của app rồi mở lại app như lần cài đầu. Phải bấm hai lần trong 4 giây; lần đầu chỉ đổi chữ thành `Tap Again To Clear`.
    - Những gì bị xoá: PlayerPrefs, gồm cả số lượt force ad; file và cache của app; dữ liệu của các SDK (Firebase, Google Mobile Ads, consent…).
    - Android: mọi thứ trong thư mục dữ liệu của app và thư mục ngoài (`Android/data/<package>`), trừ thư viện native. App tự mở lại.
    - iOS: mọi `NSUserDefaults` trong `Library/Preferences` của app, gồm domain của app và các suite của SDK (Firebase Installations, Firebase Analytics, Google Mobile Ads `__gads__`…), trừ domain `com.apple.*`; cùng `Documents`, `Library` và `tmp`. Keychain không bị xoá, giống như khi cài lại app. Lựa chọn ATT và quyền thông báo do iOS giữ, app không xoá được: muốn được hỏi lại thì xoá app rồi cài lại. iOS không cho app tự mở lại, nên app thoát và phải mở lại bằng tay.
    - Editor: PlayerPrefs của project, `persistentDataPath` và `temporaryCachePath`, rồi vào lại Play Mode.
    - Công tắc `Test Ad Units` được giữ lại.
  - Khi preset của Build Tool bật `Test ad units`, prefab bật lại test ad unit mỗi lần mở app, nên nút trên bảng chỉ tắt được cho lần chạy hiện tại.
- Consent & ATT:
  - Bước đang chạy (hỏi dịch vụ consent, form đang hiện, câu hỏi ATT đang hiện, xong), trạng thái consent và có được request quảng cáo không.
  - Có cần nút privacy options không, trạng thái ATT, máy có được hỏi như ở châu Âu không, và lỗi gần nhất.
  - `Privacy Options`: mở form đổi lựa chọn của Google.
  - `Reset Consent`: xoá consent đã lưu. Mở lại app để được hỏi lại từ đầu; muốn thấy form như ở châu Âu thì bấm `Make Test Device` trước.
  - Header bảng báo khi đang chờ consent và ATT, và khi consent không cho phép quảng cáo.
- Mediation:
  - Số adapter mediation Google Mobile Ads đã khởi tạo xong, lấy từ kết quả `MobileAds.Initialize`.
  - Từng partner có cài đặt riêng (hiện là Meta Audience Network): adapter sẵn sàng chưa, độ trễ hoặc lý do lỗi, test mode và device hash. Nút `Meta Test Mode On/Off`; `On` cũng đăng ký máy làm test device của Google (như `Make Test Device`), nên Ad Inspector mở được ngay.
  - Meta test mode chỉ làm SDK Meta trả quảng cáo test khi Meta được hỏi. Nguồn nào thắng do mediation group của ad unit trên AdMob quyết định, nên phần lớn quảng cáo vẫn là AdMob. Muốn chỉ nhận Meta: `Open Ad Inspector`, chọn ad unit, `Ad source testing`, `Meta Audience Network`, rồi load lại quảng cáo. Khi `Test Ad Units` bật, request không qua mediation nên không bao giờ có Meta; thẻ ghi chú cả hai điều này.
  - `Open Ad Inspector`: mở Ad Inspector của Google Mobile Ads (`MobileAds.OpenAdInspector`). Chỉ chạy trên máy thật, và máy phải là test device: nếu chưa phải, nút tự đăng ký máy trước (như `Make Test Device`). Kết quả, kể cả mã và nội dung lỗi, hiện ngay dưới danh sách.
  - Các adapter khác trong build: tên và trạng thái.
- Device: model, hệ điều hành, CPU, RAM, GPU, màn hình, safe area, pin.
- Network: kết nối, và IP công khai, quốc gia, nhà mạng (tra từ ipwho.is khi mở trang lần đầu hoặc bấm `Check Public IP`), để biết điều kiện quốc gia của Remote Config nhận máy là ở đâu.
- Adjust:
  - Trạng thái HDCAdjust: ai khởi động Adjust, ở môi trường nào, `HDCAdjust.Network`, số lần gửi doanh thu, doanh thu gần nhất và purchase token. Thiếu prefab hay thiếu app token thì dòng đầu báo vàng hoặc đỏ.
  - Phiên bản SDK, adid và attribution, đọc thẳng từ Adjust SDK qua reflection.

### Khác

- Trong Editor, ad unit có chữ `fail` trong ID sẽ load lỗi với mã 3 (no fill), để thử cách bảng hiện lỗi.
- Đây là công cụ debug. Bản Release build bằng [Build tool](#build-tool) tự tắt bảng; build bằng cách khác thì gỡ bảng khỏi scene trước khi build bản phát hành.
- Giao diện prefab được dựng bằng code trong `HDCAds/Debug/Editor/HDCAdsDebugPanelBuilder.cs`. Muốn sửa giao diện thì sửa ở đó rồi chạy `HDC.Ads.DebugUI.Editor.HDCAdsDebugPanelBuilder.Build` để dựng lại prefab. Bản dựng lại giữ các tùy chọn đã chỉnh trên prefab cũ: blocking mask của GraphicRaycaster, sorting order của Canvas và các tùy chọn mở bảng.

## Build tool

Cửa sổ `HDC > Build Tool` (phím tắt Shift+B; bấm lại khi đang mở thì chỉ đưa cửa sổ lên trước và focus, không đóng) build game cho iOS và Android từ một chỗ. Code nằm trong `HDCTool/` (assembly `HDC.Tool.Editor`, chỉ có trong Editor, cần define `HDC_ADS`), nên không vào bản build của game.

- Hai tab `1. iOS` và `2. Android`, mỗi tab là công cụ build của nền tảng đó. Khi build target đang chọn khác tab, đầu cửa sổ hiện nút `Switch to …`.
- Hai preset `Test Debug` và `Release`. Mỗi tab nhớ preset riêng.

| Rule | Test Debug | Release |
|---|---|---|
| Google test ads (máy thành test device) | bật | tắt |
| Google test ad units | bật | tắt |
| Bảng debug | bật | tắt |
| Debug log | bật | tắt |
| Môi trường Adjust | Sandbox | Production |
| Development build | tắt | tắt |
| Android: file AAB | tắt (APK) | bật |
| Cần change log | không | có |

- Rule chỉ áp vào bản copy của scene lúc build (`IProcessSceneWithReport`). Scene và prefab của bạn giữ nguyên, nên không có bước khôi phục sau build.
  - `HDCAdsSetup`: debug log, Google test ads, Google test ad units. Inspector của prefab không hiện ba trường này nữa; chỉ Build Tool đặt chúng.
  - `HDCAdjust`: môi trường.
  - Bảng debug: GameObject của `HDCAdsDebugPanel` bị tắt trong bản build khi rule tắt.
  - Rule của Build Tool luôn thắng giá trị đang đặt trong scene, kể cả khi scene và prefab không đổi gì từ lần build trước. Ví dụ bỏ tick Debug panel thì bảng debug không hiện dù prefab của nó trong scene Splash đang bật. Unity dùng lại dữ liệu scene của lần build trước khi scene không đổi, nên trong lúc build tool đặt tạm `Assets/HDCTool/Resources/HDCBuildRules.txt` ghi rule của preset, build xong thì xoá. Rule đổi thì Unity build lại dữ liệu scene; rule giữ nguyên thì Unity vẫn dùng lại dữ liệu cũ, build không chậm thêm.
  - Build bằng `File > Build Settings` hay script khác thì không áp rule nào.
- Sửa rule ngay trong thẻ `Overview`; `Reset to defaults` trả preset về bảng trên.
- Rule, change log và tuỳ chọn append của iOS lưu trong `Assets/HDCTool/HDCBuildSettings.asset` của game (tự tạo lần đầu mở cửa sổ khi chưa có file; file có sẵn không bao giờ bị tạo lại). Commit file này để cả team dùng chung qua git. File nằm ngoài thư mục HDCLib, vì HDCLib có thể là submodule hay package chỉ đọc.
- Những thứ riêng từng máy không nằm trong file đó:
  - thư mục build, tab và preset đang chọn, `Run on` của iOS, máy Android, iPhone và simulator đang chọn (EditorUserSettings);
  - Team và Bundle ID của iOS (giống `HDC > iOS > Local build settings`);
  - thẻ nào đang thu gọn (EditorPrefs);
  - mật khẩu keystore (Unity quên khi đóng Editor).

### Các phần của cửa sổ

Lần đầu mở (khi cửa sổ chưa dock vào layout), cửa sổ chiếm nửa bên phải và trọn chiều cao của cửa sổ Unity chính, rộng ít nhất 560 px. Mỗi phần là một thẻ có màu riêng. Bấm tiêu đề hay mũi tên để thu gọn hoặc mở thẻ, máy nhớ trạng thái của từng thẻ. `Expand all` và `Collapse all` trên thanh công cụ mở hoặc gập mọi thẻ cùng lúc; nút mờ đi khi mọi thẻ đã mở, hoặc đã gập. Thẻ đang gập không tốn gì khi vẽ. Cửa sổ rộng từ 720 px thì các ô ngắn xếp thành hai cột, cùng một lưới cho mọi thẻ; hẹp hơn thì thanh công cụ ẩn dòng mô tả cạnh tên tool.

Thứ tự từ trên xuống: các thẻ thông tin (Overview, App, SDK versions), Validation scan, Change log, Build scenes. `Signing & device` là footer cố định ở cuối, có cả các nút build.

- Overview: tên bản build, thư mục build (`Open`, `Set`, `Default`; iOS thêm `Append to last export`), rồi preset của bản build.
  - Chọn `Test Debug` hay `Release`, sửa rule của preset ngay trong thẻ (Google test ads, test ad units, bảng debug, debug log, development build, change log, AAB với Android, môi trường Adjust). `Reset to defaults` trả preset về bảng ở trên.
- App: sửa thẳng Player Settings. Mọi ô cùng độ rộng nhãn và thẳng mép.
  - App name: tên hiện dưới icon trên máy. Để trống thì dùng Product name. Lưu trong file settings (dùng chung qua git) và áp cho mọi bản build: iOS ghi `CFBundleDisplayName` vào Info.plist, Android ghi `app_name` của module launcher.
  - Product name, company, package name hoặc bundle ID, version, version code (Android) hoặc build number (iOS). Version, version code và build number đều có nút `+1`; với version, nút tăng số cuối (1.2.3 thành 1.2.4).
  - iOS: Minimum iOS (sửa thẳng Player Settings) và Pods minimum.
    - Pods minimum bật (mặc định, lưu trong file settings): mỗi lần export, Podfile có thêm bước `post_install` nâng mọi pod có minimum iOS thấp hơn lên bằng minimum của app. Không cần sửa từng pod trong Xcode nữa, và lần `pod install` sau vẫn giữ.
    - Minimum của app ở đây là số cao hơn giữa Minimum iOS và dòng `platform :ios` của Podfile (HDCAds nâng dòng này lên ít nhất 15.0). Pod đã cao hơn thì giữ nguyên.
  - Android: API tối thiểu và đích.
  - Orientation (cả hai tab, dùng chung cho iOS và Android): `Portrait` hoặc `Landscape`, sửa thẳng Player Settings.
    - Landscape cho xoay giữa ngang trái và ngang phải; Portrait cố định dọc.
    - Khi Player Settings đang để xoay mọi hướng thì không ô nào sáng, và có dòng nhắc chọn.
    - Báo cáo build ghi orientation.
  - Default Icon: chọn icon mặc định ngay trong thẻ, như Player Settings > Default Icon. Cỡ icon nào chưa có icon riêng trong Player Settings thì iOS và Android dùng icon này.
  - Target SDK của iOS không còn ô riêng: nó đi theo `Run on` ở footer (Simulator dùng Simulator SDK, iPhone và My Mac dùng Device SDK). Tool chỉ đổi SDK trong lúc build, xong thì trả Player Settings về như cũ.
- SDK versions: bảng hai cột, Library và version của nền tảng đang mở (tab iOS chỉ có cột iOS, tab Android chỉ có cột Android). Chia nhóm HDC, Google, Mediation, Analytics & attribution, mỗi thư viện một dòng. Thư viện hay mạng không có ở nền tảng này thì không hiện.
  - HDC: HDCLib, `hdc-ads-android`, `HDCAds.xcframework` (framework không có số version nên hiện ngày build).
  - Google: plugin Unity, Mobile Ads SDK, User Messaging Platform.
  - Mediation: mỗi mạng một dòng tên, dưới là `Adapter` (version adapter của Google) và `SDK` (version SDK của mạng). Mạng nào có adapter trong project cũng được liệt kê, kể cả mạng tool chưa biết tên.
  - Analytics & attribution: Adjust, Firebase Unity SDK, Firebase Analytics, Firebase Remote Config.
  - Android: version khai báo trong các file `*Dependencies.xml`, khai báo trùng thì lấy version cao nhất.
  - iOS: version CocoaPods (`Podfile.lock`) và Swift Package Manager (`Package.resolved`) đã chọn ở lần export gần nhất. Chưa export thì hiện version khai báo, chữ nghiêng màu xám.
  - Version SDK của mạng: trên iOS đọc từ pod của mạng (ví dụ `FBAudienceNetwork`). Trên Android, hoặc khi chưa export, là version adapter bỏ số cuối, theo cách Google đặt version adapter (adapter `6.20.0.0` đi với SDK `6.20.0`).
  - Rê chuột lên một version để xem nó đọc từ đâu. Bấm `Refresh` sau khi External Dependency Manager đổi version.
- Validation scan: kiểm tra trước khi build, lỗi xếp trên cùng. Lỗi chặn build, cảnh báo thì không. Phần lớn mục có nút sửa nhanh. Cửa sổ quét lại mỗi khi được focus; nút `Scan` quét cả scene 0.
  - Build target, scene trong Build Settings, scene 0 có `HDCAdsSetup`, bảng debug khi preset bật bảng.
  - AdMob app ID của nền tảng, version (build number của iOS phải là số cách nhau bằng dấu chấm).
  - File cấu hình Firebase: `google-services.json` cho Android, `GoogleService-Info.plist` cho iOS. Thiếu file thì Firebase không chạy: Remote Config dùng config mặc định và không có sự kiện Analytics. Đây là lỗi với Release và cảnh báo với Test Debug.
  - Change log khi preset cần, app token Adjust của nền tảng.
  - `onboarding_config` và `first_native_banner_config`: cùng luật với bước kiểm tra lúc build của [Onboarding](#onboarding-splash-và-intro).
  - Android: Release cần keystore và alias; keystore riêng cần đủ mật khẩu. iOS: có team. Hai mục này sửa ở `Signing & device` cuối cửa sổ.
  - iOS với `Run on` là Simulator: báo (mức Info) các thư viện tĩnh có phần arm64 chỉ cho máy thật, mà bản export cho simulator sẽ đánh dấu lại (xem `Signing & device`). Chỉ cảnh báo và khoá chạy khi có thư viện tool không đánh dấu được.
- Change log (lưu trong file settings, có trong báo cáo build).
- Build scenes: scene của Build Settings, dùng chung cho hai nền tảng.
  - Kéo tay cầm ở đầu dòng để đổi thứ tự; số thứ tự (build index) đổi theo, scene tắt hoặc mất file không có số.
  - Kéo scene từ cửa sổ Project thả vào danh sách để thêm; `–` bỏ scene đang chọn khỏi danh sách.
  - Bật tắt từng scene, `Open`, `Add open scenes`, `Clean missing`, `Build Settings`.
- `Signing & device`: footer cố định ở cuối cửa sổ, viền xanh lá phía trên. Bên phải tiêu đề là kết quả lần build gần nhất (rê chuột để xem đủ). Các ô thẳng một cột, nhãn cùng độ rộng. Dòng chữ cam dưới các ô nói còn thiếu gì để `Build and run`.
  - iOS, từ trên xuống:
    - Team: dropdown như trong Xcode: `None` (dùng team của Player Settings), các team đã đăng nhập trong Xcode (Xcode > Settings > Accounts), `Other team ID…` để gõ tay, `Add an Account…` để mở Xcode. Nút làm mới đọc lại danh sách. Cửa sổ `HDC > iOS > Local build settings` dùng cùng dropdown này.
      - Có team thì mỗi lần export, Unity-iPhone, UnityFramework và Unity-iPhone Tests đều nhận team và ký tự động. Mở Xcode lên chỉ việc bấm Build.
      - Team cá nhân (miễn phí) chỉ ký được bundle ID mà nó đăng ký được. Bundle ID của bản store thuộc team khác thì điền ô Bundle ID.
    - Bundle ID: bundle ID của app trên máy này. Để trống thì dùng bundle ID của project (hiện mờ trong ô).
    - Run on: `iPhone`, `My Mac` hoặc `Simulator`.
      - iPhone: các iPhone đang cắm (danh sách của xctrace).
      - My Mac: chạy bản iPhone ngay trên máy Mac (Designed for iPad), cần Mac chip Apple. Tool bật Run on My Mac cho riêng lần build đó.
      - Simulator: các simulator iOS có trên máy (`xcrun simctl`); simulator đang chạy xếp đầu, có chữ `running`. Bản cho simulator dùng Simulator SDK (arm64 trên Mac chip Apple) và export vào thư mục `Simulator` trong thư mục build. Simulator không cần ký, nên ô Team và Bundle ID mờ đi.
      - Thư viện tĩnh có phần arm64 chỉ cho máy thật, ví dụ Firebase C++ của Unity (`libFirebaseCppApp.a`, `libFirebaseCppAnalytics.a`, `libFirebaseCppRemoteConfig.a`: phần arm64 cho iPhone, phần simulator chỉ có x86_64 cho Mac Intel). Simulator trên Mac chip Apple chạy arm64, nên link báo `building for 'iOS-simulator', but linking in object file built for 'iOS'`.
        - Tool sửa việc này trong bản export cho simulator: sau khi export, các file `.a` đó trong `Libraries/` của bản export được đổi nhãn platform của từng object arm64 từ iOS sang iOS Simulator. Mã máy giữ nguyên, file trong project Unity không đổi, bản cho máy thật không bị ảnh hưởng.
        - Dòng chữ xám dưới Device nói thư viện nào được đánh dấu. Thư viện có object không ghi platform (thư viện rất cũ) thì không đánh dấu được: tool báo tên và khoá `Build and run on Simulator`.
    - Device: máy của đích đang chọn. Nút làm mới quét lại iPhone, Mac và simulator.
    - Nút: `▶ Build and run on iPhone` (hoặc `My Mac`, `Simulator`), `Export and open Xcode`, `Export Xcode`, rồi `Open Xcode project`, `Open export folder`, `Open report`.
      - `Export and open Xcode` mở bản export trong Xcode, chọn sẵn scheme `Unity-iPhone` và máy đang chọn làm đích chạy.
      - `Build and run` export, rồi bảo Xcode (qua AppleScript) build, cài và mở app trên máy đang chọn. Cửa sổ báo Xcode đang làm gì và kết quả: app đã mở, hoặc lỗi build của Xcode. Log của app xem trong console của Xcode.
      - iPhone và My Mac cần ký app, nên cần team (ô Team, hoặc team trong Player Settings). Thiếu team, không có máy, hoặc simulator không link được thì nút bị khoá và dòng chữ cam nói lý do.
      - Lần đầu, macOS hỏi có cho Unity điều khiển Xcode không: chọn Allow. Lỡ chọn Don't Allow thì bật lại ở System Settings > Privacy & Security > Automation.
      - Swift package (Firebase): mỗi thư mục export có DerivedData riêng trong Xcode, nên lần đầu chạy một thư mục (ví dụ thư mục `Simulator` mới), Xcode phải tải package. Nếu bấm Run lúc đó, build báo `Missing package product 'FirebaseAnalytics'` (`'FirebaseCore'`, `'FirebaseRemoteConfig'`) kèm `Package resolution errors must be fixed before building`. Bấm Run thêm khi Xcode đang tải còn huỷ việc tải (`The source control operation was canceled`).
        - Vì vậy, khi DerivedData chưa có package, tool tải trước bằng `xcodebuild -resolvePackageDependencies` (lần đầu khoảng 25 giây) rồi mới mở Xcode. Nếu build vẫn báo thiếu package, tool chờ tải xong rồi mới chạy lại.
        - Tool cũng không dừng (`stop`) workspace mà Xcode vừa mở; chỉ dừng khi workspace đã mở sẵn, để thay lần chạy cũ.
        - Các lỗi đỏ của lần chạy hỏng vẫn nằm trong danh sách lỗi của Xcode cho tới khi lần build sau xong.
      - iPhone phải mở khoá lúc Xcode cài app, nếu không Xcode đứng chờ.
      - Mọi bản export của Unity đều tên `Unity-iPhone.xcworkspace`, và AppleScript của Xcode chỉ phân biệt workspace theo tên. Nếu Xcode đang mở một workspace cùng tên ở thư mục khác, tool không bấm Run (để không chạy nhầm project kia) và báo đường dẫn của nó: đóng workspace đó rồi bấm lại. `Export and open Xcode` khi đó vẫn mở project, nhưng để bạn tự chọn máy và bấm Run.
      - Không dùng Build And Run của Unity: Unity 2022.3 cần `ios-deploy` để cài lên máy, còn `devicectl` của Xcode không nhận iPhone chạy iOS 16. Xcode thì cài được lên cả hai.
  - Android, từ trên xuống:
    - Keystore: `Unity debug key` hoặc `Custom keystore`. Keystore riêng thì có thêm File (`Browse`), Key alias và hai ô mật khẩu (keystore, key). Unity quên mật khẩu khi đóng Editor.
    - Device: danh sách của adb, kèm phiên bản Android và độ phân giải. Nút làm mới quét lại.
    - Nút: `▶ Build and run`, `Build APK` / `Build AAB`, rồi `Open build folder`, `Open report`, rồi `Install`, `Launch`, `Stop`, `Clear data`, `Uninstall`, `Mirror screen` (scrcpy, cài bằng `brew install scrcpy`).
      - `Build and run` với APK cài và mở app trên máy đang chọn. Với AAB, Unity tự cài lên máy chạy (Run Device) của nó.
      - `Install` cài file APK mới nhất trong thư mục build.
  - Cửa sổ hẹp thì các nút trong một hàng cùng co lại cho vừa.

### Tên, thư mục và báo cáo

- Tên bản build: `{mã sản phẩm}_{testDebug|release}_{version}_{số build}_{APK|AAB|Xcode}`, thêm `_dev` khi bật development build.
  - Mã sản phẩm là các chữ hoa trong tên sản phẩm, ví dụ `SampleAd-HDC` thành `SAHDC`. Tên không có chữ hoa thì lấy 3 ký tự đầu.
- Thư mục build không bao giờ nằm trong project Unity. Mặc định là `Build/{tên thư mục project}` trong thư mục chứa thư mục cha của project. Ví dụ project `Workspace/UnityProject/SampleAd-HDC` thì build vào `Workspace/Build/SampleAd-HDC`.
  - `Set` chọn thư mục khác; thư mục nằm trong project bị từ chối. `Default` quay về thư mục mặc định. Lựa chọn lưu riêng trên máy này.
- iOS export thẳng vào thư mục build, dùng chung cho Test Debug và Release. Lần sau Unity append vào project Xcode đó (giữ thay đổi bạn làm trong Xcode) khi nó cho phép. Bản cho simulator export vào thư mục `Simulator` bên trong, để không đè bản cho máy thật.
- Android ghi file APK/AAB vào thư mục `Android` bên trong thư mục build.
- Khi không append được, Unity xoá cả thư mục rồi export lại. Trước lần export đó, tool chuyển thư mục `Android`, `Simulator` và các file báo cáo sang một thư mục tạm cạnh đó, export xong thì chuyển lại.
- Mỗi bản export iOS, tool còn sửa project Xcode:
  - Phase Run Script của target GameAssembly (IL2CPP) được khai báo output `$(CONFIGURATION_BUILD_DIR)/libGameAssembly.a` và luôn chạy. Không có khai báo này, lần build đầu của một bản export mới (DerivedData trống) có thể báo `Build input file cannot be found: …/libGameAssembly.a`, vì Xcode link UnityFramework trước khi script IL2CPP ghi xong file.
  - Pods minimum (xem thẻ App) và App name.
- Mỗi lần build ghi `{tên bản build}_buildreport.txt` cạnh bản build (iOS: trong thư mục build, Android: trong thư mục `Android`): tổng quan, Player Settings (iOS ghi cả `Run on` và Target SDK đã dùng), rule, máy, change log, kết quả kiểm tra và lỗi build.

### Dòng lệnh

```bash
Unity -batchmode -projectPath <project> -buildTarget Android -executeMethod HDC.Tool.Editor.HDCBuildRunner.BuildFromCommandLine -hdcPreset Release
```

Lệnh này dùng cùng preset, bước kiểm tra và báo cáo với cửa sổ. Nó trả mã 0 khi build xong, và 1 khi build lỗi hoặc bị kiểm tra chặn. Với `-buildTarget iOS`, lệnh export project Xcode cho iPhone; thêm `-hdcIosTarget Simulator` để export bản cho simulator, hoặc `-hdcIosTarget Mac` cho My Mac. Bỏ `-hdcPreset` thì dùng Test Debug.

## Cập nhật thư viện native

Chạy trong dự án AdsMultiplatform, nhánh `migrate/HDC-Lib`:

```bash
./gradlew :shared:exportUnityPlugins -PunityPluginsDir=<HDCLib>/HDCAds/Plugins
```

Lệnh này làm hai việc:

- Build `HDCAds.xcframework` bản release (kèm Compose resources) từ module `:shared`.
- Publish thư viện Android `com.hdc.adsmultiplatform:hdc-ads-android` từ module `:unityAndroid` vào `HDCAds/Plugins/Android/Repository~`.
  - Module này dùng lại logic của `:shared` và vẽ quảng cáo bằng View/XML, không dùng Compose, để mọi bản Unity build được.
  - Khi đổi version, sửa cả `unityAndroid/build.gradle.kts` và `HDCAds/Editor/Templates~/HDCAdsDependencies.xml`.

Khi chỉ thư viện Android đổi, chỉ cần publish phần Android:

```bash
./gradlew :unityAndroid:exportUnityAndroidPlugin -PunityPluginsDir=<HDCLib>/HDCAds/Plugins
```

## Kiểm thử

Thư mục `HDCAds/Tests` có hai phần: test tự động trong `HDCAds/Tests/Editor`, và scene test trong `HDCAds/Tests/Scenes` để thử thư viện trên máy như một game thật (xem [Scene test](#scene-test)).

Test Edit Mode nằm trong `HDCAds/Tests/Editor` (assembly `HDC.Ads.Tests`), `HDCOnboarding/Tests/Editor` (assembly `HDC.Onboarding.Tests`) và `HDCTool/Tests/Editor` (assembly `HDC.Tool.Tests`). Ba assembly này chỉ compile khi project có package Test Framework và đang bật HDC (define `HDC_ADS`), và không bao giờ vào bản build của game.

- Chạy trong Editor: Window > General > Test Runner > EditMode > Run All.
- Chạy bằng dòng lệnh: `Tools~/run-tests.sh <project>`.
  - Unity không mở được project đang mở trong Editor, nên hãy chạy trên một bản copy (`cp -c -R` trên APFS copy tức thì).
  - Kết quả ghi vào `Logs/hdc-tests.xml` của project đó.
- Test cần mạng (lấy Remote Config thật của project) có `[Explicit]` và category `Network`. Run All không chạy test này; muốn chạy thì chọn đích danh.

Các nhóm test:

- Logic các kênh (`HDCLogicTests`): chạy trên port giả (`HDCAds/Tests/Editor/Fakes`), không cần Play Mode, mỗi test vài mili giây.
  - Có: mạng giả load và show, capping (launch, giảm theo lượt, mức tối thiểu), backup khi mạng đầu lỗi, đổi banner khi refresh lỗi, timeout của app launch, gỡ quảng cáo, thứ tự ưu tiên, doanh thu.
  - Thêm mạng hay kênh mới thì viết test kiểu này trước: `HDCFakeAds` dựng runtime với đồng hồ, lưu trữ, SDK và mạng giả do test điều khiển.
- Bảng debug, tracker, ID test của Google, post-process iOS: chạy Play Mode với phần giả lập của Editor. Trong giả lập, ad unit có chữ `fail` trong ID load lỗi no fill.
  - Một kênh giả (`HDCFakeChannel`) đăng ký từ test hiện đủ tab, nút, ad unit, trạng thái, cảnh báo config và bản đồ ad unit: bảng debug không có code riêng cho kênh nào.
- Adjust (`HDCAdjustTests`): chuẩn hoá network, chọn môi trường, chuyển doanh thu sang Adjust, attribution, giá trị mặc định của prefab, define `HDC_ADJUST`. Một test Play Mode kiểm tra doanh thu của quảng cáo tới được HDCAdjust.
- Hợp đồng API (`HDCPublicApiTests`):
  - So mọi type và member public của các assembly runtime với `HDCAds/Tests/Editor/PublicApi.txt`.
  - Type hay member public mà file chưa có làm test fail, mục trong file mà code không còn cũng vậy.
  - Khi cố ý đổi API, cập nhật file trong cùng commit: chạy test một lần với `HDC_ACCEPT_API=1`, hoặc sửa tay.
- Onboarding (`HDCOnboarding/Tests/Editor`, assembly `HDC.Onboarding.Tests`): đọc `splash_config` và `intro_config` của hệ thống cũ bên trong `onboarding_config`, kiểm hai scene đã nối đủ, kiểm tra lúc build (`onboarding_config` chưa khai báo, scene thiếu trong Build Settings), và chạy luồng splash và intro trong Play Mode với quảng cáo giả lập: thứ tự sự kiện tracking, native sau interstitial (dưới interstitial trên Android, sau khi đóng ở nền tảng khác), interstitial lỗi, gỡ quảng cáo, thanh loading bắt đầu từ 0% và không nhảy khi thiếu `loadingTime`, các trang intro, trang không có view bị bỏ qua.
- Build tool (`HDCTool/Tests/Editor`, assembly `HDC.Tool.Tests`): tên bản build và số build tiếp theo, rule mặc định của hai preset, đường dẫn thư mục build, đọc danh sách thiết bị của adb, xctrace và simctl (output đọc theo UTF-8), đích chạy iOS và thư viện chỉ có code cho máy thật, rule áp vào `HDCAdsSetup`, `HDCAdjust` và bảng debug, các mục kiểm tra, báo cáo build, version SDK (`Podfile.lock`, `Package.resolved`, adapter mediation), cỡ nút ở footer.
- Luật tầng (`HDCLayerRulesTests`):
  - Chỉ Infrastructure được gọi thẳng Google Mobile Ads, Firebase, `PlayerPrefs`, JNI hay `DllImport`.
  - Không tầng nào phụ thuộc ngược lên: Domain và Ports không dùng tầng nào ngoài Domain và Api; Diagnostics và Infrastructure không dùng Logic hay Composition; Api và Logic không dùng Infrastructure, Logic không dùng Composition.
  - Chỗ vi phạm có từ trước thì ghi vào danh sách `KnownDebt` của test (hiện trống). Danh sách này chỉ được giảm: sửa xong chỗ nào thì xoá mục đó.

`Tools~/compile-matrix.sh` biên dịch mọi assembly bằng Roslyn của đúng bản Unity của project, không cần mở Unity:

- Các cấu hình: Editor, iOS và Android (có Adjust), không Firebase (không Adjust), Input System, tắt HDC (HDCAdjust có và không có Adjust), assembly Editor theo từng build target, và assembly test.
- Chạy sau mỗi thay đổi có `#if`. Mất khoảng 15 giây.
- Mặc định tìm Unity theo đường dẫn Unity Hub trên macOS. Máy khác thì đặt `UNITY_EDITOR_DIR`.

### Scene test

`HDCAds/Tests/Scenes` có hai scene dựng như một game thật dùng HDCLib, để thử thư viện trong chính project của game, với config và Remote Config của project đó:

- `HDCAdsTestBoot` là scene đầu. Nó có prefab `HDCAdsSetup` (lấy config, khởi tạo ads, chạy app launch rồi mở scene sau), prefab `HDCAdjust` và bảng debug (giữ qua các scene).
  - `HDCAdsSetup` ở đây bật `Debug Log` và `Google Test Ads`: máy thành test device của Google, ad unit thật nhận quảng cáo test.
- `HDCAdsTestGame` có component `HDCAdsTestScene`: mỗi kênh một tab, mỗi nút gọi đúng một hàm của API public, ví dụ `HDCAds.ForceAd.Show(position)`.
  - Tên group và position lấy từ config đang dùng (`HDCAds.Testing.Groups` / `Positions`).
  - Màn hình có log kết quả của từng lời gọi, doanh thu (`HDCAds.Revenue`), custom key, attribution của Adjust, và nút mở bảng debug.
  - Scene có prefab `HDCBreakAd`, nên break ad chạy khi ad core có `break_ads_config`. Tab Break ad có `Start`, `Stop`, `Restart` và log các trạng thái.
- Assembly `HDC.Ads.TestScenes` không được thấy phần internal của thư viện, nên scene chỉ gọi được API public, giống code game.

Cách dùng:

1. `HDC > Test Scenes > Add to Build Settings` đưa hai scene lên đầu Build Settings. Scene boot mở scene game theo tên, nên cần bước này cả khi chạy trong Editor.
2. `HDC > Test Scenes > Open` rồi bấm Play, hoặc build ra máy.
3. Thử xong thì `HDC > Test Scenes > Remove from Build Settings`, trước khi build bản phát hành.

- Bản build không có scene test trong Build Settings thì không mang theo assembly `HDC.Ads.TestScenes`.
- Prefab `HDCAdjust` trong scene boot chưa có app token, nên Adjust không khởi động. Muốn thử Adjust thì điền token vào scene lúc thử, và không commit thay đổi đó vào HDCLib.

## Yêu cầu

- Unity 2021.3 trở lên, tới Unity 6000.6. Xem bảng bên dưới.
- Plugin Google Mobile Ads của Unity, bản 11.x (đã thử 11.4.0). Plugin này ghi App ID của AdMob vào `Info.plist` và `AndroidManifest.xml`. Nó cũng phục vụ rewarded, app open và banner view. Assembly `HDC.Ads` dùng trực tiếp các DLL của plugin.
- External Dependency Manager 1.2.151 trở lên. Bản này mới đọc được repo Maven nằm trong package.
- Adjust Unity SDK 5 (đã thử 5.4.2), nếu dùng prefab `HDCAdjust`. Không bắt buộc.
- iOS 15.0 trở lên. Post-process tự nâng deployment target của project Xcode và Podfile. Pod: GMA iOS 13.9, Meta adapter 6.22.0.0.
- Android minSdk 24.

### Android theo từng bản Unity

Thư viện Android `hdc-ads-android` không dùng Compose:

- Bytecode Java 8, minSdk 24. Không bắt compileSdk mới (`minCompileSdk` = 30).
- POM kéo `kotlin-stdlib` 2.0.21, `koin-core-jvm` 4.0.0 (kèm `stately-concurrency` và `stately-concurrent-collections` 2.1.0), `constraintlayout` 2.1.4 và `cardview` 1.0.0. Koin 4.0.0 được build bằng Kotlin 2.0 và bytecode Java 8, như thư viện.
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

Bảng trên đo với bản 0.3.x. Bản 0.4.0 (thêm Koin) mới được chạy lại trên Unity 2022.3.62 (Gradle 7.5.1, AGP 7.4.2): APK thật qua External Dependency Manager, Koin lấy từ mavenCentral, D8 và R8 qua, chạy đủ native banner, popup, native full-screen, interstitial và native sau interstitial trên emulator Android 13. Koin 4.0.0 chỉ là thư viện JVM (bytecode Java 8, không kéo AndroidX), nên không mong có giới hạn mới ở các bản Unity khác, nhưng chưa đo lại.

Các giới hạn sau đến từ GMA, Meta và AndroidX, không phải từ HDC:

- Unity 2021.3 trước bản .41 (AGP 4.x): Jetifier đời cũ không đọc được class Java 17 trong AndroidX mà GMA kéo theo (`activity` 1.8.1, `annotation-experimental` 1.4.0). Cần đặt `android.enableJetifier=false` trong `gradleTemplate.properties`, hoặc nâng Unity lên 2021.3.41+.
- Meta Audience Network (adapter `com.google.ads.mediation:facebook`):
  - 6.22.0.0 kéo `androidx.browser` 1.9.0, yêu cầu AGP 8.9.1 và compileSdk 36. Chỉ dùng được từ Unity 6000.0.61 / 6000.2 trở lên.
  - 6.21.0.0: D8 của AGP 7.4.2 lỗi NullPointerException khi dex Meta SDK 6.21.0. Dùng được từ Unity 6000.0 trở lên.
  - Unity 2021.3 và 2022.3 dùng tối đa 6.20.0.0.
- GMA 25.x tham chiếu API Android 35. Từ AGP 8 trở lên cần compileSdk ≥ 35 (Target API Level 35+), nếu không R8 báo thiếu class `android.media.LoudnessCodecController`.

### iOS

- Post-process tự thêm `HDCAds.xcframework` và `HDCAdsBridge.mm` vào project Xcode, nên không phụ thuộc cách từng bản Unity xử lý plugin `.xcframework`.
- Post-process link `AppTrackingTransparency.framework`. Nếu `NSUserTrackingUsageDescription` còn trống, post-process ghi câu mặc định (xem [Consent và ATT](#consent-và-att)).
- Post-process đặt `CADisableMinimumFrameDurationOnPhone = true` trong Info.plist. Quảng cáo native (full-screen, popup, banner) vẽ bằng Compose Multiplatform, và Compose dừng app ngay lần đầu hiện quảng cáo nếu key này thiếu hoặc là `false`. Unity ghi `false` khi Player Settings > iOS > Enable ProMotion đang tắt.
- Post-process nhúng vào app mọi framework động mà pod mang theo, ví dụ `FBAudienceNetwork` của Meta, `AppLovinSDK` của adapter AppLovin và `AdjustSigSdk` của Adjust:
  - Podfile của Unity gắn pod vào target UnityFramework, nên CocoaPods không tự nhúng các framework này vào app. Framework động không được nhúng thì app không mở được, vì thiếu thư viện lúc chạy.
  - Script chạy lúc Xcode build. Nó tìm trong `XCFrameworkIntermediates` của pod các framework có binary là thư viện động, rồi copy vào thư mục `Frameworks` của app và ký lại những framework mà app chưa có. Framework tĩnh và framework đã được nhúng (ví dụ bởi post-process của Adjust) được bỏ qua.
  - Export kiểu Append thay build phase cũ của HDC bằng bản mới, nên không có hai phase cùng tên.
- Export kiểu Append đôi khi liệt kê phase Run Script của target GameAssembly hai lần, và Xcode dừng với `Unexpected duplicate tasks`. Bước post-process cuối cùng (`PostProcessBuild(1000)`) bỏ dòng trùng trong danh sách build phase của mọi target.
- Thiết lập build iOS riêng cho máy này ở `HDC > iOS > Local build settings`, như hệ thống cũ. Thiết lập lưu trong thư mục `UserSettings` của project. Thư mục này không commit, nên mỗi máy và mỗi project có giá trị riêng. Thiết lập áp dụng từ lần export Xcode tiếp theo:
  - Local signing: `Team ID` và `Bundle ID` của máy này, ví dụ một personal team miễn phí, vốn không ký được bundle ID của publisher.
    - Có team thì export chuyển target app, UnityFramework và Unity-iPhone Tests sang ký tự động: `Automatic`, `Apple Development`, bỏ provisioning profile thủ công.
    - Bundle ID chỉ đổi cho target app. Ô để trống thì giữ Player Settings.
    - `HDC > iOS > Clear local signing` xoá cả hai.
  - Run on My Mac (Designed for iPad):
    - `Xcode default` giữ nguyên như Unity export. Với Unity 2022.3.62 và Xcode 26.4.1, project export ra đã cho chạy trên My Mac, vì Unity không đặt `SUPPORTS_MAC_DESIGNED_FOR_IPHONE_IPAD` và Xcode mặc định là `YES`.
    - `On` cho Xcode chạy app trên Mac chip Apple. Chỉ áp dụng cho export Device SDK.
    - `Off` bỏ My Mac khỏi danh sách đích chạy của Xcode, giống mặc định của hệ thống cũ.
  - Khác hệ thống cũ: hệ thống cũ lưu thiết lập chung cho cả máy (mọi project), và không có chỗ nhập team hay bundle ID trong Unity.
- Build cho simulator:
  - Unity link UnityFramework bằng `-all_load`, để engine (ở simulator là thư viện động) tìm được IL2CPP theo tên. Nhưng `-all_load` nạp mọi phần của HDCAds, và các thư viện Skia bên trong lặp object nên link lỗi trùng symbol (HarfBuzz).
  - Post-process thay cờ đó bằng `-force_load` cho riêng `libil2cpp.a`, `libGameAssembly.a` và `baselib.a`.
  - HDCAds chỉ có slice simulator arm64. Từ Unity 2022.3, đặt Player Settings > iOS > Simulator Architecture = ARM64. Unity 2021.3 chỉ build simulator x86_64, nên ở bản này HDC chỉ chạy trên máy thật.
- Đã kiểm chứng bằng Unity 2022.3.62, với External Dependency Manager chạy `pod install`:
  - Simulator SDK: Xcode build xong. Trên simulator, SDK khởi tạo, lấy được hash test device của Meta và load được interstitial test của Google.
  - Device SDK: Xcode build bản Release cho máy thật (không ký) xong, không còn symbol nào của HDCAds chưa link.
  - Thiết lập build iOS riêng của máy, với Xcode 26.4.1. Export Device SDK hai lần, Replace rồi Append, với team, bundle ID và My Mac = `On`:
    - Project Xcode có đúng team và bundle ID, có `SUPPORTS_MAC_DESIGNED_FOR_IPHONE_IPAD = YES`, và mỗi build phase của HDC chỉ có một bản.
    - `xcodebuild` (không ký) build xong. App có đủ các framework động mà UnityFramework link tới: `AdjustSigSdk` và `FBAudienceNetwork`.
  - Script nhúng framework động, chạy trên thư mục build có sẵn của một project có 24 framework pod: chỉ 3 framework động (`AdjustSigSdk`, `AppLovinSDK`, `FBAudienceNetwork`) được nhúng. Chạy lần hai thì không copy lại.

## Trạng thái

- Giai đoạn 1: interstitial.
- Giai đoạn 2:
  - Native fullscreen, popup và banner trên cả hai nền tảng.
  - Meta test mode.
  - Sự kiện impression/click/paid chung cho mọi định dạng.
- Giai đoạn 3:
  - Rewarded, app open, banner view (FullBottom và MREC) qua plugin Google Mobile Ads.
  - Retry khi load lỗi, test device.
- Giai đoạn 4: tầng kênh `HDCAds` với config từ Firebase Remote Config (schema cũ).
- Giai đoạn 5: chạy trên mọi bản Unity từ 2021.3 đến 6000.6.
  - Thư viện Android mới `hdc-ads-android` không dùng Compose. Version GMA và Meta do game quyết định.
  - iOS: post-process tự thêm framework và bridge vào project Xcode.
  - Cài được như package (UPM).
  - Hỗ trợ tắt domain reload.
- Menu `HDC` riêng trên thanh menu: bật/tắt Ads, sửa config mặc định trong cửa sổ có kiểm tra JSON, thiết lập build iOS riêng của máy.
- Bảng debug `HDCAdsDebugPanel`, bốn trang:
  - Ads: chọn kênh, group và vị trí theo config rồi gọi init/show/hide; group đang chọn hiện từng ad unit với trạng thái, mã lỗi kèm giải thích và số liệu. Native banner và popup cũng báo mã lỗi thật của SDK (từ thư viện Android 0.3.1 và framework iOS cùng đợt).
  - Remote Config: config lấy từ đâu, kiểm tra lỗi config, xem JSON theo Remote, Default, Applied và mọi key Remote Config.
  - Events: mọi sự kiện quảng cáo, lọc và giải thích. Device: build, test ads, thiết bị, mạng, Adjust.
- Prefab `HDCAdsSetup` cho scene đầu: lấy config, khởi tạo ads, chạy app launch rồi mở scene tiếp theo.
- Bộ test Edit Mode: bảng debug, tracker, ID test, post-process iOS, hợp đồng API, luật tầng, doanh thu. Kèm script chạy test và biên dịch nhiều cấu hình.
- Tầng API riêng: game chỉ thấy `HDCAds`, tám kênh qua interface, `HDCAds.Revenue` và `HDCAds.Testing`. SDK, config và sự kiện thô là internal.
- Kiến trúc theo tầng (Api, Logic, Domain, Diagnostics, Ports, Infrastructure, Composition), có test kiểm luật phụ thuộc:
  - Mạng quảng cáo và partner mediation nằm sau port.
  - Mỗi kênh mang module debug và luật config của riêng nó.
  - Xem [ARCHITECTURE.md](ARCHITECTURE.md).
