namespace HDC.Ads.Domain
{
    internal sealed class HDCDeviceRegion
    {
        public string SystemLanguage = "";
        public string[] LanguageCodes = new string[0];
        public string[] Regions = new string[0];
        public string TimezoneName = "";
        public double UtcOffsetHours;
        public string SimCountry = "";
        public string NetworkCountry = "";
    }
}
