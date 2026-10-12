using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HDC.Ads.Diagnostics;
using HDC.Ads.Infrastructure;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HDC.Ads.DebugUI
{
    public sealed class HDCDebugAreaEditor : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler,
        IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        internal const string SavedKeyPrefix = "HDCAds.Debug.Area.";

        private const float DefaultMarginPoints = 16f;
        private const float CaptionGap = 8f;

        private static readonly Vector2 DefaultSizePoints = new Vector2(320f, 280f);
        private static readonly Color IdleFill = new Color(0.31f, 0.275f, 0.898f, 0.18f);
        private static readonly Color EditingFill = new Color(0.31f, 0.275f, 0.898f, 0.34f);
        private static readonly Color IdleFrame = new Color(0.647f, 0.706f, 0.988f, 0.55f);
        private static readonly Color EditingFrame = new Color(0.647f, 0.706f, 0.988f, 1f);

        [SerializeField] private Image fill;
        [SerializeField] private Image[] frame = new Image[0];
        [SerializeField] private Text label;
        [SerializeField] private RectTransform caption;
        [SerializeField] private Text captionText;
        [SerializeField] private GameObject[] handles = new GameObject[0];
        [Tooltip("Faded while the area moves or changes size, so the game behind the panel shows.")]
        [SerializeField] private CanvasGroup fadedWhileEditing;
        [Tooltip("Seconds a finger rests on the area before the area follows it.")]
        [SerializeField, Range(0.2f, 1.5f)] private float holdSeconds = 0.4f;
        [SerializeField, Range(0f, 1f)] private float fadedAlpha = 0.2f;
        [Tooltip("Seconds between the placements sent to the ad while the area moves.")]
        [SerializeField, Range(0f, 0.5f)] private float placeSeconds = 0.05f;

        private readonly List<RaycastResult> hits = new List<RaycastResult>();
        private HDCDebugArea area;
        private string key = string.Empty;
        private Action<string> record;
        private Mode mode;
        private bool pressed;
        private bool held;
        private bool dragged;
        private float pressedTime;
        private Vector2 grabPoint;
        private Rect grabRect;
        private Vector2 corner;
        private float nextPlace;
        private Vector2Int fittedScreen;
        private Rect fittedParent;

        private enum Mode
        {
            None,
            Moving,
            Resizing,
        }

        internal RectTransform Box => (RectTransform)transform;

        internal bool IsEditing => mode != Mode.None;

        internal float HoldSeconds
        {
            get => holdSeconds;
            set => holdSeconds = value;
        }

        private RectTransform Parent => (RectTransform)transform.parent;

        private float PixelsPerUnit => Mathf.Max(0.0001f, Parent.lossyScale.x);

        private string SavedKey => SavedKeyPrefix + key + (Screen.width > Screen.height ? ".Landscape" : ".Portrait");

        internal void Bind(HDCDebugArea shown, string channelKey, Action<string> onPlaced)
        {
            if (shown == null)
            {
                Hide();
                return;
            }

            bool sameChannel = gameObject.activeSelf && key == (channelKey ?? string.Empty);
            area = shown;
            key = channelKey ?? string.Empty;
            record = onPlaced;
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
            Anchor();
            foreach (GameObject handle in handles.Where(each => each != null && each.activeSelf != area.Resizable))
                handle.SetActive(area.Resizable);
            if (mode == Mode.None)
                Fit(Wanted(), !sameChannel);
            Describe();
        }

        internal void Hide()
        {
            End();
            pressed = false;
            area = null;
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        internal void BeginResize(Vector2 handleCorner, Vector2 screenPoint)
        {
            if (area == null || !area.Resizable || mode != Mode.None)
                return;
            corner = handleCorner;
            Begin(Mode.Resizing, screenPoint);
        }

        internal void Resize(Vector2 screenPoint)
        {
            if (mode != Mode.Resizing)
                return;
            Vector2 delta = Local(screenPoint) - Local(grabPoint);
            SetLocalRect(Resized(grabRect, corner, delta, MinLocalSize(), ScreenBounds()));
            PlaceSoon();
        }

        internal void EndResize()
        {
            if (mode == Mode.Resizing)
                End();
        }

        internal Rect ScreenRect()
        {
            var corners = new Vector3[4];
            Box.GetWorldCorners(corners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            return Rect.MinMaxRect(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y), Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y));
        }

        internal static Rect Clamp(Rect rect, Rect bounds)
        {
            float width = Mathf.Min(rect.width, bounds.width);
            float height = Mathf.Min(rect.height, bounds.height);
            return new Rect(Mathf.Clamp(rect.x, bounds.xMin, bounds.xMax - width), Mathf.Clamp(rect.y, bounds.yMin, bounds.yMax - height), width, height);
        }

        internal static Rect Resized(Rect start, Vector2 handleCorner, Vector2 delta, Vector2 min, Rect bounds)
        {
            float xMin = start.xMin;
            float xMax = start.xMax;
            float yMin = start.yMin;
            float yMax = start.yMax;
            if (handleCorner.x > 0f)
                xMax = Mathf.Min(Mathf.Max(xMax + delta.x, xMin + min.x), bounds.xMax);
            else
                xMin = Mathf.Max(Mathf.Min(xMin + delta.x, xMax - min.x), bounds.xMin);
            if (handleCorner.y > 0f)
                yMax = Mathf.Min(Mathf.Max(yMax + delta.y, yMin + min.y), bounds.yMax);
            else
                yMin = Mathf.Max(Mathf.Min(yMin + delta.y, yMax - min.y), bounds.yMin);

            var size = new Vector2(Mathf.Max(xMax - xMin, min.x), Mathf.Max(yMax - yMin, min.y));
            float x = handleCorner.x > 0f ? xMin : xMax - size.x;
            float y = handleCorner.y > 0f ? yMin : yMax - size.y;
            return Clamp(new Rect(x, y, size.x, size.y), bounds);
        }

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            if (area == null || mode != Mode.None)
                return;
            pressed = true;
            held = false;
            dragged = false;
            pressedTime = Time.unscaledTime;
            grabPoint = eventData.position;
        }

        void IInitializePotentialDragHandler.OnInitializePotentialDrag(PointerEventData eventData) => eventData.useDragThreshold = true;

        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
        {
            if (mode == Mode.Moving)
            {
                Follow(eventData.position);
                return;
            }

            pressed = false;
            dragged = true;
            GameObject below = Below<IBeginDragHandler>(eventData);
            if (below == null)
                return;
            eventData.pointerDrag = below;
            ExecuteEvents.Execute(below, eventData, ExecuteEvents.initializePotentialDrag);
            ExecuteEvents.Execute(below, eventData, ExecuteEvents.beginDragHandler);
        }

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            if (mode == Mode.Moving)
                Follow(eventData.position);
        }

        void IEndDragHandler.OnEndDrag(PointerEventData eventData)
        {
            if (mode == Mode.Moving)
                End();
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
        {
            pressed = false;
            if (mode == Mode.Moving)
                End();
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            if (held || dragged || mode != Mode.None)
                return;
            GameObject below = Below<IPointerClickHandler>(eventData);
            if (below != null)
                ExecuteEvents.Execute(below, eventData, ExecuteEvents.pointerClickHandler);
        }

        void IScrollHandler.OnScroll(PointerEventData eventData)
        {
            GameObject below = Below<IScrollHandler>(eventData);
            if (below != null)
                ExecuteEvents.Execute(below, eventData, ExecuteEvents.scrollHandler);
        }

        private void Update()
        {
            if (area == null)
                return;
            if (mode == Mode.None && (fittedScreen != new Vector2Int(Screen.width, Screen.height) || fittedParent != Parent.rect))
                Fit(Wanted(), true);
            if (pressed && !dragged && mode == Mode.None && Time.unscaledTime - pressedTime >= holdSeconds)
            {
                held = true;
                Begin(Mode.Moving, grabPoint);
            }
        }

        private void OnDisable()
        {
            End();
            pressed = false;
        }

        private void Begin(Mode next, Vector2 screenPoint)
        {
            mode = next;
            pressed = false;
            grabPoint = screenPoint;
            grabRect = LocalRect();
            nextPlace = 0f;
            Paint(true);
        }

        private void Follow(Vector2 screenPoint)
        {
            Vector2 delta = Local(screenPoint) - Local(grabPoint);
            SetLocalRect(Clamp(new Rect(grabRect.position + delta, grabRect.size), ScreenBounds()));
            PlaceSoon();
        }

        private void End()
        {
            if (mode == Mode.None)
                return;
            mode = Mode.None;
            Paint(false);
            if (area == null || Near(LocalRect(), grabRect))
                return;
            Rect placed = ScreenRect();
            Save(placed);
            string call = area.Place(placed);
            if (!string.IsNullOrEmpty(call))
                record?.Invoke(call);
            Describe();
        }

        private void PlaceSoon()
        {
            Describe();
            if (Time.unscaledTime < nextPlace)
                return;
            nextPlace = Time.unscaledTime + placeSeconds;
            area?.Place(ScreenRect());
        }

        private void Paint(bool editing)
        {
            if (fill != null)
                fill.color = editing ? EditingFill : IdleFill;
            foreach (Image bar in frame.Where(each => each != null))
                bar.color = editing ? EditingFrame : IdleFrame;
            if (fadedWhileEditing != null)
                fadedWhileEditing.alpha = editing ? fadedAlpha : 1f;
        }

        private void Describe()
        {
            if (area == null)
                return;
            Vector2 points = LocalRect().size * PixelsPerUnit / HDCAdsSdk.PixelsPerPoint;
            if (captionText != null)
                captionText.text = $"{area.Label} · {points.x:0} × {points.y:0} pt";
            if (label != null)
                label.text = area.Resizable ? area.Label + "\nGiữ để kéo · kéo góc để đổi cỡ" : area.Label + "\nGiữ để kéo";
            PlaceCaption();
        }

        private void PlaceCaption()
        {
            if (caption == null)
                return;
            float thickness = frame.Length > 0 && frame[0] != null ? frame[0].rectTransform.rect.height : 0f;
            float gap = thickness + CaptionGap;
            bool above = LocalRect().yMax + gap + caption.rect.height <= ScreenBounds().yMax;
            caption.anchorMin = caption.anchorMax = new Vector2(0f, above ? 1f : 0f);
            caption.pivot = new Vector2(0f, above ? 0f : 1f);
            caption.anchoredPosition = new Vector2(-thickness, above ? gap : -gap);
        }

        private Rect Wanted()
        {
            float scale = HDCAdsSdk.PixelsPerPoint;
            Rect safeArea = Screen.safeArea;
            Rect rect = area.Locate(scale, safeArea) ?? Saved(scale) ?? Default(scale, safeArea);
            Vector2 min = area.SizePoints * scale;
            Vector2 size = area.Resizable ? Vector2.Max(rect.size, min) : min;
            return new Rect(rect.center - size / 2f, size);
        }

        private Rect Default(float scale, Rect safeArea)
        {
            Vector2 size = Vector2.Max(DefaultSizePoints, area.SizePoints) * scale;
            return new Rect(safeArea.center.x - size.x / 2f, safeArea.yMin + DefaultMarginPoints * scale, size.x, size.y);
        }

        private Rect? Saved(float scale)
        {
            string[] parts = PlayerPrefs.GetString(SavedKey, string.Empty).Split(',');
            if (parts.Length != 4)
                return null;
            var numbers = new float[4];
            for (int i = 0; i < numbers.Length; i++)
            {
                if (!float.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out numbers[i]))
                    return null;
            }

            Vector2 size = new Vector2(numbers[2], numbers[3]) * scale;
            var center = new Vector2(numbers[0] * Screen.width, numbers[1] * Screen.height);
            return new Rect(center - size / 2f, size);
        }

        private void Save(Rect screenRect)
        {
            if (Screen.width <= 0 || Screen.height <= 0)
                return;
            float scale = HDCAdsSdk.PixelsPerPoint;
            float[] numbers = { screenRect.center.x / Screen.width, screenRect.center.y / Screen.height, screenRect.width / scale, screenRect.height / scale };
            PlayerPrefs.SetString(SavedKey, string.Join(",", numbers.Select(number => number.ToString("0.#####", CultureInfo.InvariantCulture))));
            PlayerPrefs.Save();
        }

        private void Fit(Rect screenRect, bool force)
        {
            fittedScreen = new Vector2Int(Screen.width, Screen.height);
            fittedParent = Parent.rect;
            Rect local = Clamp(LocalOf(screenRect), ScreenBounds());
            if (force || !Near(local, LocalRect()))
                SetLocalRect(local);
        }

        private void Anchor()
        {
            RectTransform box = Box;
            if (box.anchorMin == Vector2.zero && box.anchorMax == Vector2.zero && box.pivot == Vector2.zero)
                return;
            Rect screenRect = ScreenRect();
            box.anchorMin = Vector2.zero;
            box.anchorMax = Vector2.zero;
            box.pivot = Vector2.zero;
            SetLocalRect(LocalOf(screenRect));
        }

        private Vector2 MinLocalSize() => area == null ? Vector2.zero : area.SizePoints * HDCAdsSdk.PixelsPerPoint / PixelsPerUnit;

        private Rect LocalRect() => new Rect(Parent.rect.min + Box.anchoredPosition, Box.sizeDelta);

        private void SetLocalRect(Rect rect)
        {
            Box.anchoredPosition = rect.position - Parent.rect.min;
            Box.sizeDelta = rect.size;
            PlaceCaption();
        }

        private Rect ScreenBounds() => LocalOf(new Rect(0f, 0f, Screen.width, Screen.height));

        private Vector2 Local(Vector2 screenPoint)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Parent, screenPoint, null, out Vector2 local);
            return local;
        }

        private Rect LocalOf(Rect screenRect)
        {
            Vector2 min = Local(screenRect.min);
            Vector2 max = Local(screenRect.max);
            return Rect.MinMaxRect(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y), Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y));
        }

        private static bool Near(Rect a, Rect b) =>
            Mathf.Abs(a.x - b.x) < 0.5f && Mathf.Abs(a.y - b.y) < 0.5f && Mathf.Abs(a.width - b.width) < 0.5f && Mathf.Abs(a.height - b.height) < 0.5f;

        private GameObject Below<T>(PointerEventData eventData) where T : IEventSystemHandler
        {
            if (EventSystem.current == null)
                return null;
            hits.Clear();
            EventSystem.current.RaycastAll(eventData, hits);
            foreach (RaycastResult hit in hits)
            {
                if (hit.gameObject == null || hit.gameObject.transform.IsChildOf(transform))
                    continue;
                return ExecuteEvents.GetEventHandler<T>(hit.gameObject);
            }

            return null;
        }
    }
}
