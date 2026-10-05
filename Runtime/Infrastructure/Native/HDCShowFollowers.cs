using System;
using System.Collections.Generic;
using HDC.Ads.Domain;
using UnityEngine;

namespace HDC.Ads.Infrastructure
{
    internal static class HDCShowFollowers
    {
        private const string ShowMethod = "fullscreen.show";

        private static readonly object Gate = new object();
        private static readonly Dictionary<string, string> Armed = new Dictionary<string, string>(StringComparer.Ordinal);
        private static readonly HashSet<string> Fired = new HashSet<string>(StringComparer.Ordinal);

        internal static void Arm(string leaderId, string followerArgsJson)
        {
            if (string.IsNullOrEmpty(leaderId) || string.IsNullOrEmpty(followerArgsJson))
                return;
            lock (Gate)
            {
                Armed[leaderId] = followerArgsJson;
                Fired.Remove(leaderId);
            }
        }

        internal static void Disarm(string leaderId)
        {
            if (string.IsNullOrEmpty(leaderId))
                return;
            lock (Gate)
                Armed.Remove(leaderId);
        }

        internal static bool Take(string leaderId)
        {
            if (string.IsNullOrEmpty(leaderId))
                return false;
            lock (Gate)
            {
                Armed.Remove(leaderId);
                return Fired.Remove(leaderId);
            }
        }

        internal static void OnNativeEvent(string eventJson, Func<string, string, string> call)
        {
            lock (Gate)
            {
                if (Armed.Count == 0)
                    return;
            }

            LeaderEvent leader;
            try
            {
                leader = JsonUtility.FromJson<LeaderEvent>(eventJson);
            }
            catch (Exception)
            {
                return;
            }

            if (leader == null || leader.format != HDCAdFormat.Interstitial || leader.type != HDCAdEventType.Shown || string.IsNullOrEmpty(leader.id))
                return;

            string followerArgsJson;
            lock (Gate)
            {
                if (!Armed.TryGetValue(leader.id, out followerArgsJson))
                    return;
                Armed.Remove(leader.id);
                Fired.Add(leader.id);
            }

            bool started;
            try
            {
                ShowResult result = JsonUtility.FromJson<ShowResult>(call(ShowMethod, followerArgsJson));
                started = result != null && result.ok && result.value;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[HDCAds] native after interstitial did not show: " + exception.Message);
                started = false;
            }

            if (started)
                return;
            lock (Gate)
                Fired.Remove(leader.id);
        }

        internal static void Reset()
        {
            lock (Gate)
            {
                Armed.Clear();
                Fired.Clear();
            }
        }

#pragma warning disable 0649
        [Serializable]
        private sealed class LeaderEvent
        {
            public string id;
            public string format;
            public string type;
        }

        [Serializable]
        private sealed class ShowResult
        {
            public bool ok;
            public bool value;
        }
#pragma warning restore 0649
    }
}
