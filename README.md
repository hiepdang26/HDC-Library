# HDCLib

Thư viện quảng cáo cho Unity. HDCLib gọi thư viện Kotlin Multiplatform của dự án AdsMultiplatform (nhánh `migrate/HDC-Lib`) trên Android và iOS.

Có hai nguồn quảng cáo:

- **Thư viện KMP** (native): interstitial, native fullscreen, popup, native banner. Logic load, waterfall ad unit, hiển thị, pause Unity và bắn sự kiện nằm trong KMP. Phía Unity chỉ gọi một hàm lệnh và nhận một luồng sự kiện.
- **Plugin Google Mobile Ads của Unity**: rewarded, app open, banner view (banner thường và MREC). HDCLib tự quản lý load, retry, show và sự kiện cho các định dạng này.

Game chỉ gọi `HDCAds` (thư mục `Runtime/Api`):

- Bảy kênh quảng cáo chạy theo config lấy từ Firebase Remote Config: force ad theo vị trí có capping, rewarded, app launch, app resume, banner, MREC, popup.
- Sự kiện doanh thu `HDCAds.Revenue` và các công tắc test `HDCAds.Testing`.

Mọi thứ bên dưới (SDK, cầu nối native, config, sự kiện thô của từng quảng cáo) là internal: game không thấy và không phụ thuộc vào chúng. Xem phần [API cho game](#api-cho-game).

Muốn sửa thư viện thì đọc [ARCHITECTURE.md](ARCHITECTURE.md): các tầng, luật phụ thuộc, và các bước thêm mạng quảng cáo, partner mediation, kênh hay nút debug.

## Cấu trúc

```
Runtime/                       Assembly HDC.Ads, chỉ compile khi có define HDC_ADS. Mỗi thư mục là một tầng:
  Api/                         (HDC.Ads) API cho game: HDCAds, interface của 7 kênh, HDCAdRevenue, các enum
  Logic/                       (HDC.Ads.Logic) Kênh và group: khi nào load, show, dùng ad unit dự phòng
    Channels/                  7 kênh (IAdChannel), mỗi kênh một class, cùng module debug của nó trong file *.Debug.cs
    Groups/                    Group fallback full-screen và banner, cùng các nguồn ad của chúng
    HDCAdsContext.cs           Thứ các kênh dùng chung: config, cờ gỡ quảng cáo, group, các port
    HDCAdGroups.cs             Kế hoạch của từng slot: mạng nào làm ad nào, theo thứ tự ưu tiên; tạo group từ đó
    HDCPriorityOrder.cs        mediationPriority 0/1/2 và thứ tự backup, viết thành dữ liệu
  Ports/                       (HDC.Ads.Ports) Interface Logic gọi, Infrastructure hiện thực:
                               mạng quảng cáo (IAdNetwork), ad full-screen, view, popup, đồng hồ, lưu trữ, main thread, log, SDK
  Domain/                      (HDC.Ads.Domain) Config, tuỳ chọn hiển thị, sự kiện của SDK
  Diagnostics/                 (HDC.Ads.Diagnostics) Cho bảng debug: trạng thái từng ad, nguồn config, mediation,
                               interface module debug của kênh (IChannelDiagnostics) và luật config (IConfigRule)
  Infrastructure/              (HDC.Ads.Infrastructure) HDCAdsSdk, main thread, retry, ID test của Google
    Native/                    Mạng native (HDCNativeNetwork) và ad của nó; cầu nối iOS (DllImport), Android (JNI), Editor (giả lập)
    Gma/                       Mạng AdMob qua plugin GMA (HDCAdMobNetwork): interstitial, rewarded, app open, banner view, MREC
    Mediation/                 Partner mediation có cài đặt riêng (IMediationPartner), hiện có Meta Audience Network
  Composition/                 (HDC.Ads.Composition) HDCAdsRuntime: lắp Infrastructure vào các port, tạo context và 7 kênh
  Firebase/                    HDCRemoteConfig (assembly HDC.Ads.Firebase, cần define HDC_FIREBASE)
  Settings/                    HDCAdsSettings: config mặc định (assembly HDC.Ads.Settings, luôn được biên dịch)
Plugins/iOS/                   HDCAds.xcframework, HDCAdsBridge.mm (post-process Xcode tự thêm vào project)
Plugins/Android/Repository~/   Maven repo chứa thư viện Android hdc-ads-android (Unity bỏ qua thư mục có đuôi "~")
Editor/                        Menu HDC (bật/tắt, sửa config), post-process Xcode, mẫu Dependencies.xml
Setup/                         HDCAdsSetup.prefab: khởi động ads ở scene đầu (assembly HDC.Ads.Setup, luôn được biên dịch)
Adjust/                        HDCAdjust.prefab: khởi động Adjust, đọc attribution, gửi doanh thu quảng cáo lên Adjust
                               (assembly HDC.Ads.Adjust, luôn được biên dịch)
Debug/                         Bảng debug HDCAdsDebugPanel.prefab (assembly HDC.Ads.Debug, cần define HDC_ADS)
Demo/                          HDCAdsDemo: mỗi kênh một hàng nút, gọi đúng API như code game
Tests/Editor/                  Test Edit Mode (assembly HDC.Ads.Tests, chỉ có trong Editor) và PublicApi.txt
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
- `googleTestAdUnits` (mặc định tắt): thay mọi ad unit trong config bằng ad unit mẫu của Google (`ca-app-pub-3940256099942544/...`, theo định dạng và nền tảng), nên mọi vị trí đều ra quảng cáo test kể cả khi unit thật không có quảng cáo (ví dụ bản build ký bằng bundle ID khác). Áp dụng từ lần load đầu tiên; quảng cáo đã load trước đó giữ unit cũ. Nút `Test Ad Units` ở trang Device của bảng debug bật tắt việc này lúc đang chạy, và nhớ lựa chọn cho các lần mở app sau.
- Cả hai tuỳ chọn trên chỉ dành cho bản test: khi bật, log có cảnh báo; nhớ tắt trước khi phát hành.
- `onAdsReady`, `onFinished`: sự kiện khi HDCAds khởi tạo xong, và ngay trước khi mở scene tiếp.

Assembly của prefab luôn được biên dịch. Khi HDC tắt, prefab chỉ mở scene tiếp theo, nên scene đầu không bị kẹt. Nếu HDCAds đã khởi tạo rồi (ví dụ quay lại scene đầu), prefab bỏ qua bước khởi tạo.

Scene chạy riêng mà không có prefab, như scene test, thì gọi `HDCAdsSetup.InitializeAds()`.

## Adjust: prefab HDCAdjust

Prefab `Adjust/HDCAdjust.prefab` thay cho prefab Adjust của SDK. Nó khởi động Adjust bằng cài đặt trên inspector, đọc attribution và tự gửi doanh thu quảng cáo của HDC lên Adjust. Cần Adjust Unity SDK 5 trong project (đã thử 5.4.2).

Cách dùng:

1. Kéo prefab vào scene đầu tiên, cạnh `HDCAdsSetup`, hoặc dùng `HDC > Adjust > Add to open scene`.
2. Điền app token Android và iOS trên instance của prefab trong scene. Đừng sửa prefab gốc trong HDCLib.
3. Xoá prefab Adjust của SDK khỏi scene nếu còn. Nếu prefab đó vẫn tự khởi động Adjust, HDCAdjust để nó khởi động với cài đặt của nó và log cảnh báo; HDCAdjust vẫn gửi doanh thu và đọc attribution.

Prefab tự giữ mình qua các scene (`DontDestroyOnLoad`). Khi scene đầu được mở lại, bản thứ hai tự huỷ, nên Adjust chỉ khởi động một lần.

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

### Custom key của game

Ngoài ads_config và ad core config, game khai báo được key Remote Config của riêng mình, như danh sách custom remote config trong configs SO của hệ thống cũ:

- Khai báo ở `HDC > Edit configs > Custom keys`. Mỗi key có giá trị mặc định cho Android và cho iOS; iOS để trống thì dùng giá trị Android.
  - Cửa sổ cảnh báo key không tên, key trùng, key trùng với key HDC đang đọc (`ads_config`, `adcore_main_android`, `adcore_main_ios`), và tên sai quy tắc của Remote Config (chữ, số và `_`, không bắt đầu bằng số).
  - Thay đổi được lưu vào asset khi bấm `Save`, hoặc khi cửa sổ mất focus.
- Game đọc giá trị bằng `HDCCustomConfig.Get("key")`, kết quả là chuỗi; giá trị JSON thì game tự parse, ví dụ bằng `JsonUtility`. Gọi trên main thread.
  - `TryGet` trả `false` cho key chưa khai báo. Key chưa khai báo đọc ra chuỗi rỗng, và log cảnh báo một lần.
  - `HDCCustomConfig.Keys` liệt kê các key đã khai báo.
- Giá trị lấy theo thứ tự: Remote Config, giá trị lần trước lưu trên máy, rồi giá trị mặc định. Giống hệ thống cũ, chuỗi rỗng trên Remote Config được coi là chưa có giá trị.
  - Giá trị lưu trên máy nằm ở PlayerPrefs `HDCAds.Custom.<key>`. Hệ thống cũ lưu dưới đúng tên key, nên có thể đè PlayerPrefs trùng tên của game.
  - Trong Editor chỉ dùng giá trị mặc định, giống ads_config, trừ khi bật `HDCRemoteConfig.FetchInEditor`.
- `HDCRemoteConfig` đọc các key này cùng lúc với ads_config, trước khi khởi tạo HDCAds. Vì vậy trong `onAdsReady` của HDCAdsSetup và ở scene sau, giá trị đã là giá trị mới.
  - `HDCCustomConfig.IsReady` cho biết đã đọc xong. Sự kiện `HDCCustomConfig.Updated` chạy mỗi lần đọc xong; handler ném lỗi thì các handler khác vẫn chạy.
  - Trước khi đọc xong, `Get` trả giá trị lần trước lưu trên máy, hoặc giá trị mặc định.
  - Project không có Firebase (`HDC_FIREBASE` tắt) thì HDCAdsSetup dùng giá trị lưu trên máy hoặc giá trị mặc định.
- `HDCCustomConfig` nằm trong assembly `HDC.Ads.Settings`, luôn được biên dịch, nên game gọi được kể cả khi tắt HDC ads.

### Chế độ quốc gia

Giống hệ thống cũ, máy ở nước đích (mặc định Việt Nam) có thể nhận bộ config riêng của project thay cho giá trị trên Remote Config:

- Config quốc gia sửa ở `HDC > Edit configs > Country configs`, gồm 4 trang:
  - `Country ads` và `Country core`, mỗi loại cho Android và cho iOS. Trang iOS để trống thì dùng bản Android.
  - Trang Android để trống thì máy ở nước đích vẫn dùng config thường.
- Giá trị quốc gia của custom key nằm ở trường `country` trong trang `Custom keys`. Để trống thì dùng giá trị mặc định của key.
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
// Nếu tự làm bằng code: gọi HDCAdsSetup.InitializeAds(), HDCAds.AppLaunch.Initialize(), rồi chờ HDCAds.AppLaunch.Completed.

HDCAds.ForceAd.Show("gameplay", onDone: ResumeGame);
HDCAds.Rewarded.Show("shop", onRewarded: GiveCoins);
HDCAds.Banner.Show();                               // slot FullBottom
HDCAds.Popup.Move("popup", popupArea);              // RectTransform; popup chỉ show sau khi đặt vị trí
HDCAds.Popup.Show("popup");
HDCAds.SetAdsRemoved(true);                         // mua gỡ quảng cáo: chặn mọi kênh trừ rewarded

// Doanh thu của từng lượt hiển thị, kèm kênh và vị trí, để gửi lên analytics.
HDCAds.Revenue += revenue => Debug.Log(revenue);    // "ForceAd gameplay fullscreen 0.0012 USD from AdMob Network"

HDCAds.Testing.EnableTestDevice();                  // chỉ cho bản test
```

- `HDCAds` có:
  - `Initialize`, `IsInitialized`, sự kiện `Initialized`, `IsAdsRemoved`, `SetAdsRemoved`.
  - Bảy kênh: `ForceAd`, `Rewarded`, `AppLaunch`, `AppResume`, `Banner`, `Mrec`, `Popup`. Mỗi kênh là một interface (`IForceAds`, `IRewardedAds`…), game chỉ gọi hàm của interface.
- `HDCAds.Revenue` báo mọi sự kiện paid dưới dạng `HDCAdRevenue`:
  - `Channel` (kênh), `Position` (vị trí trong game; slot với banner; rỗng với app launch, app resume, MREC).
  - `Format` (một giá trị của `HDCAdFormat`), `Network` (hiện luôn là `"AdMob"`), `AdSource` (nguồn mediation, ví dụ `"Meta Audience Network"`), `AdUnitId`.
  - `Value` theo đơn vị `Currency`, và `Precision` (0 không rõ, 1 ước tính, 2 publisher cung cấp, 3 chính xác).
  - Handler của game ném lỗi thì lỗi được log, các handler khác vẫn nhận.
- `HDCAds.Testing`: `DebugLog`, `EnableTestDevice()` / `IsTestDevice`, `UseTestAdUnits`, Meta test mode. Chỉ dùng cho bản test.
- Gọi API từ main thread của Unity. Callback và sự kiện cũng luôn tới trên main thread.
- Danh sách đầy đủ của API nằm trong `Tests/Editor/PublicApi.txt`. Test hợp đồng API giữ file này luôn đúng với code.

### Bên dưới API

- Load lỗi được thử lại sau 2, 4, 8, 16, 32 rồi 64 giây, và reset khi load được:
  - Rewarded và app open: sau mỗi lần show sẽ load ad mới. Show lúc chưa có ad thì load ngay, trừ khi đang chờ retry.
  - Interstitial: KMP load mọi ad unit cùng lúc và dừng khi tất cả lỗi, nên HDCLib gọi load lại.
  - Banner view: chỉ retry lần load đầu. Sau khi có ad, view tự refresh theo cấu hình ad unit.
- Rewarded và app open có chế độ `preload`: plugin tự giữ sẵn 1–5 ad và tự load lại.
- App open quá 4 giờ kể từ lúc load thì coi là chưa sẵn sàng.
- `HDCAds.Testing.EnableTestDevice()` đăng ký máy đang chạy là test device của Google Mobile Ads, áp dụng cho mọi định dạng.
- Trên iOS, sự kiện tới ngay cả khi quảng cáo fullscreen đang pause Unity. Trên Android, sự kiện phát ra trong lúc quảng cáo fullscreen che game sẽ tới khi Unity chạy lại.
- Hỗ trợ tắt domain reload (Enter Play Mode Options, mặc định của project Unity 6.6 mới). Mọi state tĩnh của HDCLib (ad, callback, subscriber, kênh) được reset mỗi lần vào Play Mode.
- Trong Editor, SDK được giả lập:
  - Load mất 0,5 giây. Ad unit có chữ `fail` trong ID thì load lỗi với mã 3 (no fill).
  - Show bắn `Shown`, `Impression`, `Paid`.
  - Interstitial và fullscreen đóng sau 1 giây, popup sau 3 giây. Banner giữ đến khi ẩn. Popup bị `Hide` thì giữ quảng cáo như trên máy thật.
  - Native sau interstitial hiện cùng lúc với interstitial như trên Android, khi build target là Android.
  - Mọi quảng cáo giả lập có nguồn `editor` (`adSourceId`), nên `adSourceGroups` và `adSourceLayouts` có `editor` thì thử được layout theo nguồn trong Editor.
  - Rewarded, app open và banner view dùng quảng cáo mẫu có sẵn của plugin GMA trong Editor.

## Config và hành vi các kênh

Config giữ nguyên key và schema JSON của hệ thống cũ, nên dùng lại được giá trị Remote Config sẵn có:

- `ads_config` quy định từng kênh:
  - Có bật không, có tự load không (`autoInit`).
  - Force ad: vị trí, capping, `launchCappingTime`, mức giảm capping theo lượt hiển thị, break ad.
  - Banner: 6 slot, `autoShowOnLoad`.
  - Popup: vị trí.
- Config core nằm dưới key do `selectedAdCoreName` chỉ định:
  - Group force ad theo vị trí, ad unit theo thứ tự ưu tiên (`mediationPriority`, `useBackup`), `maxShowCount`, `disablePostInitReload`.
  - Layout group của native fullscreen, asset config.
  - `comebackChannel`: launch dùng force ad hay app open.
- `admobUnit` chạy qua plugin Google Mobile Ads. `androidUnit` chạy qua thư viện native trên cả Android và iOS:
  - Native fullscreen dùng layout ngẫu nhiên trong layout group, không lặp cho tới khi dùng hết.
  - Nếu bật `switchToInterstitialAndroid` thì dùng interstitial.
  - Native sau interstitial (`androidInterstitials.useNativeAfterInterstitial`, `nativeAfterInterstitialId`, `nativeAfterInterstitialLayout`) chỉ áp dụng cho group force ad dùng interstitial, và chỉ trên Android, giống hệ thống cũ. Trên iOS, Config Check ghi chú là phần này không chạy.
- Quảng cáo native luôn hiện đúng layout mà config ghi. Layout mặc định chỉ dùng khi config không ghi layout, hoặc ghi tên layout không tồn tại:
  - Popup (`popupGroups[].androidUnit.layout`): `popup_single_manual_01` tới `15`. Tên cũ `mrec_single_manual_NN` hiện layout `popup_single_manual_NN`. Mặc định là `popup_single_manual_01`.
  - Fullscreen (`forceAdLayoutConfig`): các layout `fs_single_*` của thư viện native. Tên có đuôi `_left` hoặc `_right` (ví dụ `fs_single_cls_03_left`, `fs_single_nav_01_right`) hiện layout gốc. Mặc định là `fs_single_universal_01`.
  - Tên không tồn tại hiện thành lỗi ở thẻ Config Check. Dòng `Layout` của popup trên trang Ads cho biết layout đang dùng.
- Layout theo nguồn quảng cáo (ad source, ví dụ Meta Audience Network hay AdMob Network), như hệ thống cũ:
  - Fullscreen: một layout group có thể có `adSourceGroups`, mỗi phần tử gồm `adSourceIds` và `layouts`. Lúc show, HDCLib lấy `adSourceId` của quảng cáo đã load (thư viện native gửi kèm sự kiện Loaded) rồi chọn ngẫu nhiên một layout của nhóm có nguồn đó. Không nhóm nào có nguồn đó thì dùng `layouts` của layout group. Layout group không có `layouts` thì dùng nhóm nguồn đầu tiên có layout.
  - Popup: `androidUnit.adSourceLayouts` gồm các phần tử `adSources` và `layout`. Thư viện native chọn layout cho từng quảng cáo theo nguồn của nó, kể cả quảng cáo load lại trong lúc popup đang hiện. Nguồn không có trong danh sách thì dùng `layout`. Tên cũ `mrec_single_manual_NN` cũng được nhận.
  - Khác hệ thống cũ: popup trên iOS cũng chọn theo nguồn (hệ thống cũ chỉ làm trên Android), và mỗi quảng cáo load lại dùng layout của nguồn mới.
  - Vùng popup giữ chiều cao tối thiểu của layout cao nhất mà popup có thể hiện.
  - Sự kiện Shown của popup ghi layout và nguồn của quảng cáo đang hiện. Thẻ của ad unit trên trang Ads có dòng `Layout` là layout thật đã hiện. Nhóm popup có thêm dòng `Layouts By Ad Source`.
  - Config Check báo lỗi tên layout không tồn tại trong `adSourceGroups` và `adSourceLayouts`.
  - Bật Google test ad units không đổi layout: chỉ ad unit được thay.
- Trên máy thật, `HDCRemoteConfig` lấy giá trị theo thứ tự: Remote Config, giá trị máy lưu từ lần trước, rồi config mặc định. Nó luôn fetch mới, timeout mặc định 10 giây. Firebase không chạy được (kể cả khi thiếu thư viện native) thì nó cũng dùng giá trị máy lưu rồi config mặc định. Trong Editor nó dùng config mặc định, trừ khi bật `HDCRemoteConfig.FetchInEditor`. Trang Remote Config của bảng debug cho thấy từng key lấy từ nguồn nào.
- Giá trị Remote Config là `{}` vẫn được coi là có giá trị. Một ad core config `{}` nghĩa là không có ad unit nào.

Hành vi các kênh:

- Force ad:
  - Chỉ show khi thời gian từ lúc đóng fullscreen gần nhất đạt capping của vị trí. Capping là `cappingTime` trừ số lượt đã hiển thị × mức giảm, không thấp hơn mức tối thiểu. Force ad đầu phiên phải chờ thêm `launchCappingTime`.
  - Số lượt hiển thị theo vị trí lưu trong PlayerPrefs (`fa_count_<vị trí>`).
  - `StartBreakAd()` chạy đồng hồ break ad. Đồng hồ reset mỗi khi có fullscreen mở. Có các event `BreakAdNotice`, `BreakAdShown`, `BreakAdClosed`, `BreakAdShowFailed`.
  - Native sau interstitial (Android) chạy như hệ thống cũ:
    - Khi interstitial hiện, native full-screen (instance `fa_naf_<group>`, layout lấy ngẫu nhiên trong `nativeAfterInterstitialLayout`) hiện ngay bên dưới nó. Lúc interstitial đóng, người chơi thấy native, và đồng hồ của native đã chạy trong lúc interstitial hiện.
    - Lệnh show native được gọi thẳng trên luồng Android lúc interstitial báo đã hiện, không chờ Unity, vì lúc đó Unity đang bị pause.
    - Native chưa load thì lần interstitial đầu bắt đầu load nó, và native hiện ngay khi load xong. Sau mỗi lần đóng, native tự load lại để sẵn cho lần sau. Native load lại xong không tự hiện: nó chỉ hiện khi có interstitial hiện.
    - Doanh thu của native tính cho force ad, đúng position của lần show đó. `onDone` của `Show` vẫn chạy khi interstitial đóng.
    - Trong lúc native đang trên màn hình, app resume không hiện quảng cáo.
    - Layout group không có thì native hiện layout mặc định `fs_single_universal_01`, và Config Check báo. Theo code của hệ thống cũ, trường hợp này native không hiện.
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
- Popup:
  - Phải gọi `Move` (đặt vùng popup) trước khi `Show`. `Move` lúc popup đang ẩn thì lần hiện sau dùng vị trí mới.
  - `Hide` chỉ ẩn popup và giữ quảng cáo. `Show` sau đó hiện lại đúng quảng cáo đó, không load thêm và không tính thêm impression.
  - Quảng cáo bị đóng hẳn (nút X trên popup, hoặc `timeShow` hết khi bật tự đóng) thì không dùng lại được. `Show` hoặc `Initialize` lần sau load quảng cáo mới; `Show` hiện popup ngay khi load xong và trả `true`.
  - `Show` lúc quảng cáo đang load cũng trả `true` và hiện khi load xong. `Hide` huỷ lần hiện đang chờ đó.
  - `disablePostInitReload` (dòng `Reload After Show` trên bảng debug) chỉ quyết định popup có tự đổi quảng cáo mới theo `reloadTime` trong lúc đang hiện hay không. Load quảng cáo mới sau khi đóng luôn chờ game gọi `Show` hoặc `Initialize`.
- Rewarded vẫn hiện khi đã gỡ quảng cáo. Các kênh còn lại đều bị chặn.
- Chưa hỗ trợ: collapsible banner. HDCLib không tự gửi doanh thu lên Firebase; game nhận `HDCAds.Revenue` rồi tự gửi. Doanh thu lên Adjust do prefab `HDCAdjust` gửi.

## Bảng debug

Prefab `Debug/HDCAdsDebugPanel.prefab` là bảng debug nằm đè lên game. Chức năng bám theo bảng debug của hệ thống cũ (ad systems, configs, tracking, diagnostics, Adjust, build/device/network), giao diện làm mới, và thêm trạng thái của từng ad unit.

- Thêm vào scene: `HDC > Debug panel > Add to open scene`, hoặc kéo prefab vào scene.
- Bảng ẩn khi vào scene. Mở bằng cách chạm nhanh 3 lần vào góc trên bên trái (14% chiều rộng và chiều cao, trong 0,9 giây), hoặc nhấn F10; đóng bằng nút `Close`.
  - Chạy được với Input Manager cũ lẫn Input System mới. Scene thiếu EventSystem thì bảng tự tạo, với input module hợp loại input của project.
  - Bảng nằm trong safe area, và tự đổi tỉ lệ khi màn hình nằm ngang.
  - Các tùy chọn trên prefab: góc mở (`activationCorner`), số lần chạm, `startOpen`, `keepAcrossScenes`.
- Đầu bảng: trạng thái SDK và ad core đang dùng, nút `Refresh`, `Close`, và `Init SDK` khi HDCAds chưa khởi tạo (mở thẳng scene không có HDCAdsSetup; nút gọi `HDCAdsSetup.InitializeAds()`). Bốn trang: Ads, Remote Config, Events, Device.

### Trang Ads

- Tab kênh, mỗi kênh của HDCAds một tab: AL (app launch), AR (app resume), RW (rewarded), FA (force ad), BN (banner), MREC, PU (popup). Chấm màu trên tab là trạng thái chung của kênh: xanh lá có ad sẵn sàng, xanh dương đang hiện, cam đang load, đỏ đang lỗi, xám chưa chạy. Tab, nút, thông tin và phần kiểm tra config đều lấy từ module debug của từng kênh, nên kênh mới có tab ngay mà không phải sửa bảng.
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
  - Mỗi custom key có một dòng `Custom: <key>`, gồm nguồn (`Remote`, `Saved`, `Default`) và đầu giá trị đang dùng.
- Thẻ Config Check: lỗi và cảnh báo của config, bằng tiếng Việt: Firebase không chạy, fetch lỗi, key không có trên Remote Config, ad core config rỗng `{}`, kênh bật mà không có ad unit, position không thuộc group nào, layout group không tồn tại, tên layout không tồn tại, position trùng tên...
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
- HDC Ads: thư viện native, Remote Config có bật không (`HDC_FIREBASE`), trạng thái khởi tạo, ads removed, `Google Test Device` và `Google Test Ad Units`. Các nút:
  - `Debug Log`.
  - `Make Test Device`: máy này thành test device. Request vẫn dùng ad unit thật, Google trả quảng cáo test, áp dụng cho các lần load sau.
  - `Test Ad Units: On/Off`: bật tắt việc thay mọi ad unit bằng ad unit mẫu của Google (`HDCAds.Testing.UseTestAdUnits`). Lựa chọn được lưu trên máy (PlayerPrefs `HDCAds.Debug.TestAdUnits`) và áp dụng từ lúc app mở, trước scene đầu tiên, cho tới khi tắt.
  - `Restart App` hiện ra sau khi đổi `Test Ad Units`, vì quảng cáo đã load vẫn giữ ad unit cũ. Trên Android nút mở lại app từ đầu; trong Editor nút thoát rồi vào lại Play Mode. iOS không cho app tự mở lại, nên nút thành `Quit App`: app thoát, rồi mở lại bằng tay.
  - `Clear Data & Restart` (iOS: `Clear Data & Quit`) luôn có sẵn. Nút xoá toàn bộ dữ liệu của app rồi mở lại app như lần cài đầu. Phải bấm hai lần trong 4 giây; lần đầu chỉ đổi chữ thành `Tap Again To Clear`.
    - Những gì bị xoá: PlayerPrefs, gồm cả Remote Config đã lưu và số lượt force ad; file và cache của app; dữ liệu của các SDK (Firebase, Google Mobile Ads, consent…).
    - Android: mọi thứ trong thư mục dữ liệu của app và thư mục ngoài (`Android/data/<package>`), trừ thư viện native. App tự mở lại.
    - iOS: toàn bộ `NSUserDefaults` của app, cùng `Documents`, `Library` và `tmp`. Keychain không bị xoá, giống như khi cài lại app. iOS không cho app tự mở lại, nên app thoát và phải mở lại bằng tay.
    - Editor: PlayerPrefs của project, `persistentDataPath` và `temporaryCachePath`, rồi vào lại Play Mode.
    - Công tắc `Test Ad Units` được giữ lại.
  - Khi `HDCAdsSetup > Google Test Ad Units` đang bật, prefab bật lại test ad unit mỗi lần mở app, nên nút trên bảng chỉ tắt được cho lần chạy hiện tại.
- Mediation:
  - Số adapter mediation Google Mobile Ads đã khởi tạo xong, lấy từ kết quả `MobileAds.Initialize`.
  - Từng partner có cài đặt riêng (hiện là Meta Audience Network): adapter sẵn sàng chưa, độ trễ hoặc lý do lỗi, test mode và device hash. Nút `Meta Test Mode On/Off`.
  - Các adapter khác trong build: tên và trạng thái.
- Device: model, hệ điều hành, CPU, RAM, GPU, màn hình, safe area, pin.
- Network: kết nối, và IP công khai, quốc gia, nhà mạng (tra từ ipwho.is khi mở trang lần đầu hoặc bấm `Check Public IP`), để biết điều kiện quốc gia của Remote Config nhận máy là ở đâu.
- Adjust:
  - Trạng thái HDCAdjust: ai khởi động Adjust, ở môi trường nào, `HDCAdjust.Network`, số lần gửi doanh thu, doanh thu gần nhất và purchase token. Thiếu prefab hay thiếu app token thì dòng đầu báo vàng hoặc đỏ.
  - Phiên bản SDK, adid và attribution, đọc thẳng từ Adjust SDK qua reflection.

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

Khi chỉ thư viện Android đổi, chỉ cần publish phần Android:

```bash
./gradlew :unityAndroid:exportUnityAndroidPlugin -PunityPluginsDir=<HDCLib>/Plugins
```

## Kiểm thử

Test Edit Mode nằm trong `Tests/Editor` (assembly `HDC.Ads.Tests`). Assembly này chỉ compile khi project có package Test Framework và đang bật HDC (define `HDC_ADS`), và không bao giờ vào bản build của game.

- Chạy trong Editor: Window > General > Test Runner > EditMode > Run All.
- Chạy bằng dòng lệnh: `Tools~/run-tests.sh <project>`.
  - Unity không mở được project đang mở trong Editor, nên hãy chạy trên một bản copy (`cp -c -R` trên APFS copy tức thì).
  - Kết quả ghi vào `Logs/hdc-tests.xml` của project đó.
- Test cần mạng (lấy Remote Config thật của project) có `[Explicit]` và category `Network`. Run All không chạy test này; muốn chạy thì chọn đích danh.

Các nhóm test:

- Logic các kênh (`HDCLogicTests`): chạy trên port giả (`Tests/Editor/Fakes`), không cần Play Mode, mỗi test vài mili giây.
  - Có: mạng giả load và show, capping (launch, giảm theo lượt, mức tối thiểu), backup khi mạng đầu lỗi, timeout của app launch, gỡ quảng cáo, thứ tự ưu tiên, doanh thu.
  - Thêm mạng hay kênh mới thì viết test kiểu này trước: `HDCFakeAds` dựng runtime với đồng hồ, lưu trữ, SDK và mạng giả do test điều khiển.
- Bảng debug, tracker, ID test của Google, post-process iOS: chạy Play Mode với phần giả lập của Editor. Trong giả lập, ad unit có chữ `fail` trong ID load lỗi no fill.
  - Một kênh giả (`HDCFakeChannel`) đăng ký từ test hiện đủ tab, nút, ad unit, trạng thái, cảnh báo config và bản đồ ad unit: bảng debug không có code riêng cho kênh nào.
- Adjust (`HDCAdjustTests`): chuẩn hoá network, chọn môi trường, chuyển doanh thu sang Adjust, attribution, giá trị mặc định của prefab, define `HDC_ADJUST`. Một test Play Mode kiểm tra doanh thu của quảng cáo tới được HDCAdjust.
- Hợp đồng API (`HDCPublicApiTests`):
  - So mọi type và member public của các assembly runtime với `Tests/Editor/PublicApi.txt`.
  - Type hay member public mà file chưa có làm test fail, mục trong file mà code không còn cũng vậy.
  - Khi cố ý đổi API, cập nhật file trong cùng commit: chạy test một lần với `HDC_ACCEPT_API=1`, hoặc sửa tay.
- Luật tầng (`HDCLayerRulesTests`):
  - Chỉ Infrastructure được gọi thẳng Google Mobile Ads, Firebase, `PlayerPrefs`, JNI hay `DllImport`.
  - Không tầng nào phụ thuộc ngược lên: Domain và Ports không dùng tầng nào ngoài Domain và Api; Diagnostics và Infrastructure không dùng Logic hay Composition; Api và Logic không dùng Infrastructure, Logic không dùng Composition.
  - Chỗ vi phạm có từ trước thì ghi vào danh sách `KnownDebt` của test (hiện trống). Danh sách này chỉ được giảm: sửa xong chỗ nào thì xoá mục đó.

`Tools~/compile-matrix.sh` biên dịch mọi assembly bằng Roslyn của đúng bản Unity của project, không cần mở Unity:

- Các cấu hình: Editor, iOS và Android (có Adjust), không Firebase (không Adjust), Input System, tắt HDC (HDCAdjust có và không có Adjust), assembly Editor theo từng build target, và assembly test.
- Chạy sau mỗi thay đổi có `#if`. Mất khoảng 15 giây.
- Mặc định tìm Unity theo đường dẫn Unity Hub trên macOS. Máy khác thì đặt `UNITY_EDITOR_DIR`.

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
  - Ads: chọn kênh, group và vị trí theo config rồi gọi init/show/hide; group đang chọn hiện từng ad unit với trạng thái, mã lỗi kèm giải thích và số liệu. Native banner và popup cũng báo mã lỗi thật của SDK (từ thư viện Android 0.3.1 và framework iOS cùng đợt).
  - Remote Config: config lấy từ đâu, kiểm tra lỗi config, xem JSON theo Remote, Saved, Default, Applied và mọi key Remote Config.
  - Events: mọi sự kiện quảng cáo, lọc và giải thích. Device: build, test ads, thiết bị, mạng, Adjust.
- Prefab `HDCAdsSetup` cho scene đầu: lấy config, khởi tạo ads, chạy app launch rồi mở scene tiếp theo.
- Bộ test Edit Mode: bảng debug, tracker, ID test, post-process iOS, hợp đồng API, luật tầng, doanh thu. Kèm script chạy test và biên dịch nhiều cấu hình.
- Tầng API riêng: game chỉ thấy `HDCAds`, bảy kênh qua interface, `HDCAds.Revenue` và `HDCAds.Testing`. SDK, config và sự kiện thô là internal.
- Kiến trúc theo tầng (Api, Logic, Domain, Diagnostics, Ports, Infrastructure, Composition), có test kiểm luật phụ thuộc:
  - Mạng quảng cáo và partner mediation nằm sau port.
  - Mỗi kênh mang module debug và luật config của riêng nó.
  - Xem [ARCHITECTURE.md](ARCHITECTURE.md).
