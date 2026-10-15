using System;

namespace HDC.Ads.Logic
{
    internal sealed class HDCLoadClock
    {
        internal const float LongestCountedFrame = 1f;
        internal const float ActivationGraceSeconds = 5f;

        private readonly float timeoutSeconds;

        internal HDCLoadClock(float timeoutSeconds) => this.timeoutSeconds = Math.Max(0f, timeoutSeconds);

        internal float Running { get; private set; }

        internal bool Tick(float frameSeconds, bool activating)
        {
            Running += Math.Min(Math.Max(0f, frameSeconds), LongestCountedFrame);
            return Running >= (activating ? timeoutSeconds + ActivationGraceSeconds : timeoutSeconds);
        }
    }
}
