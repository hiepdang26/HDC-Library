using System;
using UnityEngine;
using UnityEngine.UI;

namespace HDC.Ads.DebugUI
{
    public sealed class HDCAdsDebugViewer : MonoBehaviour
    {
        private const float CopiedSeconds = 1.5f;

        [SerializeField] private GameObject modalRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private ScrollRect bodyScroll;
        [SerializeField] private Button copyButton;
        [SerializeField] private Button closeButton;
        [Tooltip("Seconds between refreshes of the text.")]
        [SerializeField] private float refreshSeconds = 1f;
        [SerializeField] private int fontSize = 24;
        [Tooltip("Font size of the large view, for reading on a phone.")]
        [SerializeField] private int largeFontSize = 32;

        private Func<string> body;
        private float nextRefresh;
        private float copiedUntil;

        public bool IsOpen => modalRoot.activeSelf;

        private void Awake()
        {
            copyButton.onClick.AddListener(Copy);
            closeButton.onClick.AddListener(Close);
            modalRoot.SetActive(false);
        }

        public void Open(string title, Func<string> bodyBuilder, bool large = false)
        {
            titleText.text = string.IsNullOrEmpty(title) ? "Viewer" : title;
            bodyText.fontSize = large ? largeFontSize : fontSize;
            body = bodyBuilder;
            modalRoot.SetActive(true);
            Render();
            if (bodyScroll != null)
                bodyScroll.verticalNormalizedPosition = 1f;
        }

        public void Close()
        {
            body = null;
            modalRoot.SetActive(false);
        }

        private void Update()
        {
            if (!IsOpen)
                return;
            if (copiedUntil > 0f && Time.unscaledTime >= copiedUntil)
            {
                copiedUntil = 0f;
                HDCDebugStyle.SetLabel(copyButton, "Copy");
            }

            if (Time.unscaledTime >= nextRefresh)
                Render();
        }

        private void Render()
        {
            nextRefresh = Time.unscaledTime + Mathf.Max(0.2f, refreshSeconds);
            string text;
            try
            {
                text = body?.Invoke();
            }
            catch (Exception exception)
            {
                text = "[debug read failed] " + exception.Message;
            }

            bodyText.text = string.IsNullOrEmpty(text) ? "(empty)" : text;
        }

        private void Copy()
        {
            GUIUtility.systemCopyBuffer = HDCDebugStyle.StripTags(bodyText.text);
            copiedUntil = Time.unscaledTime + CopiedSeconds;
            HDCDebugStyle.SetLabel(copyButton, "Copied");
        }
    }
}
