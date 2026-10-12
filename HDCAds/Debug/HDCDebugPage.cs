using UnityEngine;

namespace HDC.Ads.DebugUI
{
    public abstract class HDCDebugPage : MonoBehaviour
    {
        [Tooltip("Seconds between redraws while the page shows.")]
        [SerializeField] private float refreshSeconds = 1f;

        private float nextRefresh;
        private bool dirty;

        public void Refresh()
        {
            dirty = false;
            nextRefresh = Time.unscaledTime + Mathf.Max(0.2f, refreshSeconds);
            Redraw();
        }

        protected abstract void Redraw();

        protected void MarkDirty() => dirty = true;

        protected virtual void OnEnable() => Refresh();

        protected virtual void Update()
        {
            if (dirty || Time.unscaledTime >= nextRefresh)
                Refresh();
        }
    }
}
