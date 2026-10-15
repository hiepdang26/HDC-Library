using UnityEngine;
using UnityEngine.UI;

namespace HDC.Ads.DebugUI
{
    public sealed class HDCDebugFoldout : MonoBehaviour
    {
        [Tooltip("The header button that opens and closes the card.")]
        [SerializeField] private Button toggleButton;
        [Tooltip("What the card shows while it is open.")]
        [SerializeField] private GameObject body;

        public bool Expanded => body != null && body.activeSelf;

        private void Awake()
        {
            toggleButton.onClick.AddListener(() => SetExpanded(!Expanded));
            ShowLabel();
        }

        public void SetExpanded(bool expanded)
        {
            Apply(expanded);
            HDCDebugPage page = GetComponentInParent<HDCDebugPage>();
            if (page != null)
                page.Refresh();
        }

        internal void Apply(bool expanded)
        {
            if (body.activeSelf != expanded)
                body.SetActive(expanded);
            ShowLabel();
        }

        private void ShowLabel() => HDCDebugStyle.SetLabel(toggleButton, Expanded ? "Collapse" : "Expand");
    }
}
