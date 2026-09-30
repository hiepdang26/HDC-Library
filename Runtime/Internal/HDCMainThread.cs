using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace HDC.Ads.Internal
{
    /// <summary>Runs work handed over from native threads, or delayed work, on the Unity main thread.</summary>
    internal sealed class HDCMainThread : MonoBehaviour
    {
        private static readonly ConcurrentQueue<Action> Queued = new ConcurrentQueue<Action>();
        private static readonly List<KeyValuePair<float, Action>> Delayed = new List<KeyValuePair<float, Action>>();
        private static HDCMainThread instance;

        /// <summary>Creates the host object. Call it from the main thread.</summary>
        internal static void EnsureCreated()
        {
            if (instance != null)
                return;

            var host = new GameObject("HDCAds") { hideFlags = HideFlags.HideInHierarchy };
            if (Application.isPlaying)
                DontDestroyOnLoad(host);
            instance = host.AddComponent<HDCMainThread>();
        }

        /// <summary>Runs <paramref name="action"/> on the main thread next frame. Safe from any thread.</summary>
        internal static void Post(Action action) => Queued.Enqueue(action);

        /// <summary>Runs <paramref name="action"/> after <paramref name="seconds"/> of real time. Safe from any thread.</summary>
        internal static void PostDelayed(float seconds, Action action) =>
            Post(() => Delayed.Add(new KeyValuePair<float, Action>(Time.unscaledTime + seconds, action)));

        private void Update()
        {
            while (Queued.TryDequeue(out Action queued))
                Run(queued);

            float now = Time.unscaledTime;
            for (int i = 0; i < Delayed.Count;)
            {
                if (Delayed[i].Key > now)
                {
                    i++;
                    continue;
                }

                Action due = Delayed[i].Value;
                Delayed.RemoveAt(i);
                Run(due);
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private static void Run(Action action)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
