using System;
using HDC.Ads.Ports;

namespace HDC.Ads.Infrastructure
{
    internal sealed class HDCUnityMainThread : IMainThread
    {
        public event Action Ticked
        {
            add
            {
                HDCMainThread.EnsureCreated();
                HDCMainThread.Ticked += value;
            }
            remove => HDCMainThread.Ticked -= value;
        }

        public event Action LateTicked
        {
            add
            {
                HDCMainThread.EnsureCreated();
                HDCMainThread.LateTicked += value;
            }
            remove => HDCMainThread.LateTicked -= value;
        }

        public event Action<bool> ApplicationPaused
        {
            add
            {
                HDCMainThread.EnsureCreated();
                HDCMainThread.ApplicationPaused += value;
            }
            remove => HDCMainThread.ApplicationPaused -= value;
        }
    }
}
