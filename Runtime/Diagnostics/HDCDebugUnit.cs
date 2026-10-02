using UnityEngine;

namespace HDC.Ads.Diagnostics
{
    internal sealed class HDCDebugUnit
    {
        internal int Index;

        internal string Name;

        internal string Format;
        internal string Id;
        internal string AdUnitId;

        internal bool Created;

        internal bool Started;

        internal bool Ready;
        internal bool OnScreen;
        internal HDCAdRecord Record;

        internal string NativeState;

        internal string Status(out HDCUnitTone tone)
        {
            HDCAdState state = Record?.State ?? HDCAdState.Idle;
            if (!Created)
            {
                tone = HDCUnitTone.Idle;
                return "NOT INITIALIZED";
            }

            if (!Started && state == HDCAdState.Idle)
            {
                tone = HDCUnitTone.Idle;
                return "BACKUP · NOT STARTED";
            }

            if (OnScreen || state == HDCAdState.Showing)
            {
                tone = HDCUnitTone.Live;
                return "SHOWING";
            }

            if (Ready)
            {
                tone = HDCUnitTone.Good;
                return "READY";
            }

            switch (state)
            {
                case HDCAdState.Loading:
                    tone = HDCUnitTone.Busy;
                    return "LOADING";
                case HDCAdState.Loaded:
                    tone = HDCUnitTone.Good;
                    return "LOADED";
                case HDCAdState.LoadFailed:
                    tone = HDCUnitTone.Bad;
                    float retryIn = Record.RetryAt - Time.realtimeSinceStartup;
                    return retryIn > 0f ? $"LOAD FAILED · RETRY IN {Mathf.CeilToInt(retryIn)}s" : "LOAD FAILED";
                case HDCAdState.ShowFailed:
                    tone = HDCUnitTone.Bad;
                    return "SHOW FAILED";
                case HDCAdState.Closed:
                    tone = HDCUnitTone.Busy;
                    return "CLOSED";
                case HDCAdState.Destroyed:
                    tone = HDCUnitTone.Idle;
                    return "DESTROYED";
                default:
                    tone = HDCUnitTone.Idle;
                    return "IDLE";
            }
        }
    }
}
