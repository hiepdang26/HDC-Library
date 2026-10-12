using System;
using System.Collections.Generic;
using HDC.Ads.Domain;
using UnityEngine;

namespace HDC.Ads.Infrastructure
{
    internal static class HDCShowFollowers
    {
        private const string ShowMethod = "fullscreen.show";
        private const string ReadyMethod = "fullscreen.isReady";
        private static readonly object Gate = new object();
        private static readonly Dictionary<string, Follower> Armed = new Dictionary<string, Follower>(StringComparer.Ordinal);
        private static readonly Dictionary<string, Follower> Waiting = new Dictionary<string, Follower>(StringComparer.Ordinal);
        private static readonly HashSet<string> Fired = new HashSet<string>(StringComparer.Ordinal);

        internal static void Arm(string leaderId, string followerArgsJson)
        {
            if (string.IsNullOrEmpty(followerArgsJson))
                return;
            Arm(leaderId, new Follower(null, _ => followerArgsJson));
        }

        internal static void ArmUntilLoaded(string leaderId, string followerId, Func<string, string> followerArgsForSource)
        {
            if (string.IsNullOrEmpty(followerId) || followerArgsForSource == null)
                return;
            Arm(leaderId, new Follower(followerId, followerArgsForSource));
        }

        internal static void Disarm(string leaderId)
        {
            if (string.IsNullOrEmpty(leaderId))
                return;
            lock (Gate)
            {
                Armed.Remove(leaderId);
                Waiting.Remove(leaderId);
            }
        }

        internal static bool Take(string leaderId)
        {
            if (string.IsNullOrEmpty(leaderId))
                return false;
            lock (Gate)
            {
                Armed.Remove(leaderId);
                Waiting.Remove(leaderId);
                return Fired.Remove(leaderId);
            }
        }

        internal static void OnNativeEvent(string eventJson, Func<string, string, string> call)
        {
            lock (Gate)
            {
                if (Armed.Count == 0 && Waiting.Count == 0)
                    return;
            }

            NativeEvent nativeEvent;
            try
            {
                nativeEvent = JsonUtility.FromJson<NativeEvent>(eventJson);
            }
            catch (Exception)
            {
                return;
            }

            if (nativeEvent == null || string.IsNullOrEmpty(nativeEvent.id))
                return;
            if (nativeEvent.format == HDCAdFormat.Interstitial)
                OnLeaderEvent(nativeEvent, call);
            else if (nativeEvent.format == HDCAdFormat.Fullscreen && nativeEvent.type == HDCAdEventType.Loaded)
                OnFollowerLoaded(nativeEvent, call);
        }

        internal static void Reset()
        {
            lock (Gate)
            {
                Armed.Clear();
                Waiting.Clear();
                Fired.Clear();
            }
        }

        private static void Arm(string leaderId, Follower follower)
        {
            if (string.IsNullOrEmpty(leaderId))
                return;
            lock (Gate)
            {
                Armed[leaderId] = follower;
                Waiting.Remove(leaderId);
                Fired.Remove(leaderId);
            }
        }

        private static void OnLeaderEvent(NativeEvent leader, Func<string, string, string> call)
        {
            if (leader.type == HDCAdEventType.Closed || leader.type == HDCAdEventType.ShowFailed)
            {
                lock (Gate)
                    Waiting.Remove(leader.id);
                return;
            }

            if (leader.type != HDCAdEventType.Shown)
                return;
            Follower follower;
            lock (Gate)
            {
                if (!Armed.TryGetValue(leader.id, out follower))
                    return;
                Armed.Remove(leader.id);
            }

            if (follower.WaitsForLoad && !IsReady(follower.Id, call))
            {
                lock (Gate)
                    Waiting[leader.id] = follower;
                return;
            }

            lock (Gate)
                Fired.Add(leader.id);
            if (Show(follower.ArgsForSource(follower.Source), call))
                return;
            lock (Gate)
                Fired.Remove(leader.id);
        }

        private static void OnFollowerLoaded(NativeEvent loaded, Func<string, string, string> call)
        {
            string leaderId = null;
            Follower follower = null;
            lock (Gate)
            {
                foreach (KeyValuePair<string, Follower> armed in Armed)
                {
                    if (armed.Value.Id == loaded.id)
                        armed.Value.Source = loaded.adSourceId ?? "";
                }

                foreach (KeyValuePair<string, Follower> waiting in Waiting)
                {
                    if (waiting.Value.Id != loaded.id)
                        continue;
                    leaderId = waiting.Key;
                    follower = waiting.Value;
                    break;
                }

                if (leaderId == null)
                    return;
                Waiting.Remove(leaderId);
                Fired.Add(leaderId);
            }

            if (Show(follower.ArgsForSource(loaded.adSourceId ?? ""), call))
                return;
            lock (Gate)
                Fired.Remove(leaderId);
        }

        private static bool IsReady(string followerId, Func<string, string, string> call)
        {
            try
            {
                ShowResult result = JsonUtility.FromJson<ShowResult>(call(ReadyMethod, HDCJson.Args(followerId, null)));
                return result != null && result.ok && result.value;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool Show(string followerArgsJson, Func<string, string, string> call)
        {
            try
            {
                ShowResult result = JsonUtility.FromJson<ShowResult>(call(ShowMethod, followerArgsJson));
                return result != null && result.ok && result.value;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[HDCAds] native after interstitial did not show: " + exception.Message);
                return false;
            }
        }

        private sealed class Follower
        {
            internal Follower(string id, Func<string, string> argsForSource)
            {
                Id = id;
                ArgsForSource = argsForSource;
            }

            internal string Id { get; }

            internal Func<string, string> ArgsForSource { get; }

            internal string Source { get; set; } = "";

            internal bool WaitsForLoad => !string.IsNullOrEmpty(Id);
        }

#pragma warning disable 0649
        [Serializable]
        private sealed class NativeEvent
        {
            public string id;
            public string format;
            public string type;
            public string adSourceId;
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
