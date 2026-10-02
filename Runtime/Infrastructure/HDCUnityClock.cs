using HDC.Ads.Ports;
using UnityEngine;

namespace HDC.Ads.Infrastructure
{
    /// <summary>Unity's <see cref="Time"/>.</summary>
    internal sealed class HDCUnityClock : IClock
    {
        public float RealTime => Time.realtimeSinceStartup;

        public float UnscaledTime => Time.unscaledTime;

        public float DeltaTime => Time.deltaTime;

        public int Frame => Time.frameCount;
    }
}
