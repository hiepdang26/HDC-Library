using System;

namespace HDC.Ads
{
    /// <summary>
    /// The ad shown while the app starts: the force ad group or app open ad picked by the ad core config's
    /// comeback channel. Once the launch clock starts, the ad shows as soon as it is ready and the minimum
    /// wait has passed; after the timeout the launch goes on without it. With no ad to show (channel off,
    /// ads removed, no unit) the launch completes right away. <see cref="Completed"/> fires once either way.
    /// </summary>
    public interface IAppLaunchAds
    {
        /// <summary>Right before the launch ad shows, or right before completing without one.</summary>
        event Action BeforeShow;

        /// <summary>The launch is done: the ad closed, failed, timed out, or could not show.</summary>
        event Action Completed;

        bool IgnoreAds { get; set; }

        bool IsCompleted { get; }

        bool IsBeforeShowRaised { get; }

        bool CanShow { get; }

        /// <summary>
        /// Starts the launch clock and loading, when the channel does not on its own (autoInit off). Called
        /// before the SDK is ready, it starts once the SDK is.
        /// </summary>
        void Initialize();
    }
}
