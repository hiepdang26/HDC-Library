using UnityEngine;
using UnityEngine.UI;

namespace HDC.Ads.DebugUI
{
    public abstract class HDCDebugPage : MonoBehaviour
    {
        private const float ChangedRefreshSeconds = 0.3f;

        [Tooltip("Seconds between redraws while the page shows.")]
        [SerializeField] private float refreshSeconds = 1f;

        private float nextRefresh;
        private float lastRefresh = float.MinValue;
        private bool dirty;
        private bool shown;

        public void Refresh()
        {
            dirty = false;
            lastRefresh = Time.unscaledTime;
            nextRefresh = lastRefresh + Mathf.Max(0.2f, refreshSeconds);
            Redraw();
        }

        public bool HasFoldouts => GetComponentsInChildren<HDCDebugFoldout>(true).Length > 0;

        public void SetAllExpanded(bool expanded)
        {
            foreach (HDCDebugFoldout foldout in GetComponentsInChildren<HDCDebugFoldout>(true))
                foldout.Apply(expanded);
            Refresh();
        }

        protected abstract void Redraw();

        protected void MarkDirty() => dirty = true;

        internal void Present(bool current, bool open)
        {
            GetComponent<Canvas>().enabled = current;
            GetComponent<GraphicRaycaster>().enabled = current && open;
            if (shown == (current && open))
                return;
            shown = current && open;
            if (shown)
            {
                Shown();
                Refresh();
            }
            else
            {
                Hidden();
            }
        }

        internal virtual void Shown()
        {
        }

        internal virtual void Hidden()
        {
        }

        protected virtual void OnEnable()
        {
            if (shown)
                Refresh();
        }

        protected virtual void Update()
        {
            if (!shown)
                return;
            float now = Time.unscaledTime;
            if ((dirty && now - lastRefresh >= ChangedRefreshSeconds) || now >= nextRefresh)
                Refresh();
        }
    }
}
