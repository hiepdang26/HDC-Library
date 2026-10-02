namespace HDC.Ads.Tests
{
    /// <summary>
    /// Configs for the Editor simulation, where an ad unit id containing "fail" fails with no fill and the
    /// others load: native_gameplay fails then falls back to its AdMob backup, native_ui loads, the popup fails.
    /// </summary>
    internal static class HDCTestConfigs
    {
        internal const string Ads = @"{ ""selectedAdCoreName"": ""core"",
  ""appLaunchChannel"": { ""isEnabled"": false }, ""appResumeChannel"": { ""isEnabled"": false },
  ""forceAdChannel"": { ""isEnabled"": true, ""positionConfigs"": [
     { ""positionName"": ""gameplay"", ""canShow"": true, ""cappingTime"": 0 },
     { ""positionName"": ""ui"", ""canShow"": true, ""cappingTime"": 0 },
     { ""positionName"": ""orphan"", ""canShow"": true } ],
     ""breakAdConfig"": { ""isEnabled"": true, ""positionName"": ""ui"", ""notificationLeadTimeSeconds"": 1 } },
  ""rewardedChannel"": { ""isEnabled"": true },
  ""bannerChannel"": { ""isEnabled"": true, ""fullBottom"": { ""isEnabled"": true } },
  ""mrecChannel"": { ""isEnabled"": true },
  ""popupChannel"": { ""isEnabled"": true, ""positionConfigs"": [ { ""isEnabled"": true, ""positionName"": ""popup"" } ] } }";

        internal const string Core = @"{
  ""forceAdLayoutConfig"": { ""layoutGroups"": [ { ""groupName"": ""cls"", ""layouts"": [ { ""layout"": ""fs_single_cls_01"", ""layoutTime"": 5 } ] } ] },
  ""forceAdGroups"": [
    { ""mediationPriority"": 1, ""useBackup"": true, ""groupName"": ""native_gameplay"", ""positionNames"": [ ""gameplay"" ],
      ""admobUnit"": { ""id"": ""ca-app-pub-3940256099942544/1033173712"" },
      ""androidUnit"": { ""id"": ""native-fail-unit"", ""layoutGroupName"": ""cls"" } },
    { ""mediationPriority"": 1, ""groupName"": ""native_ui"", ""positionNames"": [ ""ui"" ], ""androidUnit"": { ""id"": ""native-ok"", ""layoutGroupName"": ""cls"" } } ],
  ""bannerUnit"": { ""fullBottom"": { ""mediationPriority"": 1, ""androidUnit"": { ""id"": ""banner-ok"", ""layouts"": [ ""bn_single_transparent_01"" ] } } },
  ""popupGroups"": [ { ""groupName"": ""popup"", ""positionNames"": [ ""popup"" ], ""androidUnit"": { ""id"": ""popup-fail-unit"", ""layout"": ""popup_single_manual_01"", ""timeShow"": 3 } } ] }";
    }
}
