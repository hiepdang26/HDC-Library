using System;
using HDC.Ads.Domain;

namespace HDC.Ads.Ports
{
    /// <summary>One full-screen ad: an interstitial, a rewarded ad, an app open ad or a native full-screen ad.</summary>
    internal interface IFullscreenAd
    {
        /// <summary>The instance id its events carry.</summary>
        string Id { get; }

        /// <summary>One of the <see cref="HDCAdFormat"/> values.</summary>
        string Format { get; }

        string AdUnitId { get; }

        bool IsReady { get; }

        /// <summary>This ad's events: loaded, failed, shown, rewarded, closed and the others.</summary>
        event Action<HDCAdEvent> Event;

        void Load();

        /// <summary>Starts the show, whose events tell how it goes. False when it could not start.</summary>
        bool Show();

        void Destroy();
    }
}
