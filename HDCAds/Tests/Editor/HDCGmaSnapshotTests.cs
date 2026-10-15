using HDC.Ads.Domain;
using HDC.Ads.Infrastructure;
using NUnit.Framework;

namespace HDC.Ads.Tests
{
    public class HDCGmaSnapshotTests
    {
        [Test]
        public void AnEventCarriesTheResponseReadAtLoad()
        {
            HDCAdEvent adEvent = HDCGma.Event("fa_g", "interstitial", "Loaded", "unit", new HDCGmaResponse("response-1", "com.google.ads.mediation.facebook.FacebookMediationAdapter", "Meta Audience Network"));

            Assert.AreEqual("response-1", adEvent.responseId);
            Assert.AreEqual("com.google.ads.mediation.facebook.FacebookMediationAdapter", adEvent.adapter);
            Assert.AreEqual("Meta Audience Network", adEvent.adSource);
        }

        [Test]
        public void AnEventWithoutAResponseLeavesItsFieldsEmpty()
        {
            HDCAdEvent adEvent = HDCGma.Event("fa_g", "interstitial", "LoadFailed", "unit", null);

            Assert.IsNull(adEvent.responseId);
            Assert.IsNull(adEvent.adapter);
            Assert.IsNull(adEvent.adSource);
        }

        [Test]
        public void AFailureKeepsTheCodeAndMessageReadInTheCallback()
        {
            HDCAdEvent failed = HDCGma.Event("fa_g", "interstitial", "LoadFailed", "unit", null).WithError(new HDCGmaFailure(3, "No fill."));
            HDCAdEvent unknown = HDCGma.Event("fa_g", "interstitial", "ShowFailed", "unit", null).WithError(null);

            Assert.AreEqual(3, failed.code);
            Assert.AreEqual("No fill.", failed.message);
            Assert.AreEqual(-1, unknown.code);
        }

        [TestCase(null, false)]
        [TestCase("", false)]
        [TestCase("00000000-0000-0000-0000-000000000000", false)]
        [TestCase("6D92078A-8246-4BA4-AE5B-76104861E7DC", true)]
        public void OnlyARealAdvertisingIdIsHashed(string advertisingId, bool real) =>
            Assert.AreEqual(real, HDCGma.IsRealAdvertisingId(advertisingId));
    }
}
