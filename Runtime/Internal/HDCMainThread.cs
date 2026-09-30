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

        /// <summary>Runs every frame after the queued work.</summary>
        internal static event Action Ticked;

        /// <summary>Unity's OnApplicationPause: true when the app goes to the background.</summary>
        internal static event Action<bool> ApplicationPaused;

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

        /// <summary>Drops the work and subscribers of the last Play Mode session; see HDCAds.</summary>
        internal static void ResetStatics()
        {
            while (Queued.TryDequeue(out _))
            {
            }

            Delayed.Clear();
            Ticked = null;
            ApplicationPaused = null;
            // Leaving Play Mode destroys the host; one still alive would tick twice per frame.
            if (instance != null)
                Destroy(instance.gameObject);
            instance = null;
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

            if (Ticked != null)
                Run(Ticked);
        }

        private void OnApplicationPause(bool paused)
        {
            Action<bool> handlers = ApplicationPaused;
            if (handlers != null)
                Run(() => handlers(paused));
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
