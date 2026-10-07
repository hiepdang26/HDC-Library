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
- Chế độ quốc gia như hệ thống cũ: máy ở nước đích (mặc định `vn`) dùng config quốc gia của project thay cho Remote Config.
  - Config quốc gia: ads, ad core và giá trị quốc gia của custom key, sửa ở `HDC > Edit configs > Country configs` và `Custom keys`.
  - Cách nhận nước đích sửa ở `Country check`, gồm ngôn ngữ hệ thống, ngôn ngữ và vùng của locale, tên múi giờ, độ lệch UTC, và SIM/mạng trên Android. Locale và múi giờ đọc thẳng từ hệ điều hành.
  - Mặc định không tính theo UTC+7, vì múi giờ này cũng là của nước khác.
  - Máy test (`debugDevices` trong project, hoặc key Remote Config `devices`) không vào chế độ quốc gia. Trang Device hiện ID của máy, trang Remote Config hiện trạng thái chế độ quốc gia.
- Thiết lập build iOS riêng cho từng máy ở `HDC > iOS > Local build settings`, như hệ thống cũ:
  - Local signing: team ID và bundle ID của máy này, ký tự động.
  - Run on My Mac (Designed for iPad): `Xcode default`, `On` hoặc `Off`.
  - Thiết lập lưu trong `UserSettings` của project, không commit. Có thêm menu `HDC > iOS > Clear local signing`.
- Banner `fullBottom` có `useBackup` đổi sang unit dự phòng khi banner đang hiện refresh lỗi, như hệ thống cũ:
  - Unit dự phòng load, hoặc load quảng cáo mới nếu quảng cáo của nó đã hiện quá 10 giây, rồi thay chỗ banner đang lỗi. Native banner có `reloadTime` dưới 20 giây được lỗi một lần trước khi bị thay.
  - Native banner im lặng quá `max(30, reloadTime + 5)` giây thì tính là refresh lỗi. Banner AdMob không bị tính như vậy, khác hệ thống cũ, vì chu kỳ refresh của AdMob đặt trên AdMob console.
  - Unit ưu tiên cao hơn load lại được thì lấy lại chỗ. Unit bị đổi xuống hoặc đang ẩn mà lỗi thì load lại sau 20 tới 40 giây.
  - Thẻ banner trên bảng debug có thêm dòng `Refresh Fails On Screen` và `Swaps`.
- Layout theo nguồn quảng cáo (thư viện `hdc-ads-android` 0.3.4 và framework iOS build lại):
  - Fullscreen: `adSourceGroups` trong layout group. HDCLib chọn layout theo `adSourceId` của quảng cáo đã load, giống hệ thống cũ.
  - Popup: `androidUnit.adSourceLayouts`. Thư viện native chọn layout cho từng quảng cáo theo nguồn, trên cả Android và iOS.
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

### Thay đổi

- Mã nguồn chia thành các tầng Api, Logic, Domain, Diagnostics, Ports, Infrastructure và Composition.
  - Mạng quảng cáo (AdMob và native) là adapter sau `IAdNetwork`.
  - Partner mediation nằm sau `IMediationPartner`.
  - Mỗi kênh mang module debug và luật kiểm config của riêng nó.
- Khi SDK sẵn sàng, các kênh khởi động theo thứ tự AL, AR, RW, FA, BN, MREC, PU. Trước đây FA khởi động trước RW.
- Bảng debug bỏ tab CL (collapsible banner), vì HDCLib chưa có kênh này.
- Bảng debug không còn hỏi phía native mỗi giây.
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
- Thư mục `Demo` (assembly `HDC.Ads.Demo`) được thay bằng scene test trong `Tests/Scenes`. Demo cũ tự nạp config giả, nên không thử được `HDCAdsSetup` hay config của project.
  - Assembly của scene test chỉ vào bản build khi Build Settings có scene test. Trước đây `HDC.Ads.Demo` vào mọi bản build của game có bật HDC.
- Post-process iOS nhúng mọi framework động của pod mà app chưa có, ví dụ `AppLovinSDK` và `AdjustSigSdk`. Trước đây nó chỉ nhúng `FBAudienceNetwork`. Export kiểu Append thay build phase cũ của HDC thay vì thêm phase thứ hai.

### Sửa

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
