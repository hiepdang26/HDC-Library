using HDC.Ads.Ports;

namespace HDC.Ads.Infrastructure
{
    /// <summary>Meta Audience Network. Its test mode goes through the native library, which sets Meta's test devices.</summary>
    internal sealed class HDCMetaPartner : IMediationPartner
    {
        public string Name => "Meta Audience Network";

#if UNITY_IOS
        public string AdapterClass => "GADMediationAdapterFacebook";
#else
        public string AdapterClass => "com.google.ads.mediation.facebook.FacebookMediationAdapter";
#endif

        public bool HasTestMode => true;

        public bool IsTestMode => HDCAdsSdk.IsMetaTestMode();

        public string TestDeviceId => HDCAdsSdk.GetMetaTestDeviceHash();

        /// <summary>Meta's test creative for the next <see cref="EnableTestMode"/>; 0 is Meta's default one.</summary>
        internal int TestAdType { get; set; }

        public bool EnableTestMode(string[] extraDeviceIds) => HDCAdsSdk.EnableMetaTestMode(extraDeviceIds, TestAdType);

        public void DisableTestMode() => HDCAdsSdk.DisableMetaTestMode();
    }
}
