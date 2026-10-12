using System;
using System.Text.RegularExpressions;

namespace HDC.Onboarding
{
    public enum HDCIntroType
    {
        AR = 0,
        AL = 1,
        PT = 2,
        FORCE_AD = 5,
    }

    public enum HDCIntroPreload
    {
        Popup,
        ForceAd,
    }

    [Serializable]
    public sealed class HDCIntroConfig
    {
        public string nextSceneName;
        public HDCIntroPage[] Configs = new HDCIntroPage[0];

        internal HDCIntroConfig Normalized()
        {
            Configs = Configs ?? new HDCIntroPage[0];
            foreach (HDCIntroPage page in Configs)
            {
                if (page != null)
                    page.AdConfig = page.AdConfig ?? new HDCIntroAd();
            }

            return this;
        }

        internal static string EnumsAsNumbers(string json)
        {
            json = Regex.Replace(json, "\"IntroType\"\\s*:\\s*\"([A-Za-z_]+)\"",
                match => "\"IntroType\":" + (Enum.TryParse(match.Groups[1].Value, true, out HDCIntroType type) ? (int)type : -1));
            return Regex.Replace(json, "\"PreloadType\"\\s*:\\s*\"([A-Za-z_]+)\"",
                match => Enum.TryParse(match.Groups[1].Value, true, out HDCIntroPreload preload) ? "\"PreloadType\":" + (int)preload : match.Value);
        }
    }

    [Serializable]
    public sealed class HDCIntroPage
    {
        public HDCIntroType IntroType;
        public bool EnableCompleteIntro;
        public HDCIntroAd AdConfig = new HDCIntroAd();
        public float TimeDelayPreload;
        public float IntroInterval;
        public string IntroDescription;
        public string IntroBackgroundImage;
        public string IntroCoverImage;
        public float DelayShowContinue;
    }

    [Serializable]
    public sealed class HDCIntroAd
    {
        public HDCIntroPreload PreloadType;
        public string GroupName;
    }
}
