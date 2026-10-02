using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace HDC.Ads.Infrastructure
{
    internal sealed class HDCMainThread : MonoBehaviour
    {
        private static readonly ConcurrentQueue<Action> Queued = new ConcurrentQueue<Action>();
        private static readonly List<KeyValuePair<float, Action>> Delayed = new List<KeyValuePair<float, Action>>();
        private static HDCMainThread instance;

        internal static event Action Ticked;

        internal static event Action<bool> ApplicationPaused;

        internal static void EnsureCreated()
        {
            if (instance != null)
                return;

            var host = new GameObject("HDCAds") { hideFlags = HideFlags.HideInHierarchy };
            if (Application.isPlaying)
                DontDestroyOnLoad(host);
            instance = host.AddComponent<HDCMainThread>();
        }

        internal static void ResetStatics()
        {
            while (Queued.TryDequeue(out _))
            {
            }

            Delayed.Clear();
            Ticked = null;
            ApplicationPaused = null;
            if (instance != null)
                Destroy(instance.gameObject);
            instance = null;
        }

        internal static void Post(Action action) => Queued.Enqueue(action);

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
