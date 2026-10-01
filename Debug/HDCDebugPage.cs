using UnityEngine;

namespace HDC.Ads.DebugUI
{
    /// <summary>A page of the debug panel. While shown, it redraws every second and when it asks to.</summary>
    public abstract class HDCDebugPage : MonoBehaviour
    {
        [Tooltip("Seconds between redraws while the page shows.")]
        [SerializeField] private float refreshSeconds = 1f;

        private float nextRefresh;
        private bool dirty;

        /// <summary>Redraws the page now.</summary>
        public void Refresh()
        {
            dirty = false;
            nextRefresh = Time.unscaledTime + Mathf.Max(0.2f, refreshSeconds);
            Redraw();
        }

        protected abstract void Redraw();

        /// <summary>Redraws on the next frame.</summary>
        protected void MarkDirty() => dirty = true;

        protected virtual void OnEnable() => Refresh();

        protected virtual void Update()
        {
            if (dirty || Time.unscaledTime >= nextRefresh)
                Refresh();
        }
    }
}
