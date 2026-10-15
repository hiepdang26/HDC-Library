using System.Collections.Generic;
using HDC.Ads.Diagnostics;
using HDC.Ads.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace HDC.Ads.DebugUI
{
    public sealed class HDCEventRows : MonoBehaviour, ILayoutElement, ILayoutGroup
    {
        private const int WithChannel = 1;
        private const int Explained = 2;
        private const int Counted = 4;

        private struct Entry
        {
            internal object Key;
            internal HDCTrackedEvent Tracked;
            internal HDCAdEvent Event;
            internal int Count;
            internal int Look;
        }

        private sealed class Row
        {
            internal RectTransform Rect;
            internal object Key;
            internal int Look = -1;
            internal bool Measured;
            internal float Height;
        }

        [Tooltip("A row with a \"Time\" text, a \"Badge\" image with a \"Label\" text, and \"Main\" with \"Title\", \"Detail\" and \"Explain\" texts. The list copies it for each event.")]
        [SerializeField] private GameObject rowTemplate;
        [Tooltip("Holds the rows. The list stacks them itself, so a new event lays out only its own row.")]
        [SerializeField] private RectTransform rowsRoot;
        [SerializeField] private float spacing = 10f;

        private readonly List<Entry> entries = new List<Entry>();
        private readonly List<Row> rows = new List<Row>();
        private readonly List<Row> order = new List<Row>();
        private readonly List<Row> picked = new List<Row>();
        private readonly HashSet<Row> claimed = new HashSet<Row>();
        private readonly Dictionary<object, Row> byKey = new Dictionary<object, Row>();
        private float measuredWidth = -1f;
        private float measuredScale = -1f;
        private float height;
        private Canvas canvas;

        float ILayoutElement.minWidth => 0f;

        float ILayoutElement.preferredWidth => 0f;

        float ILayoutElement.flexibleWidth => -1f;

        float ILayoutElement.minHeight => height;

        float ILayoutElement.preferredHeight => height;

        float ILayoutElement.flexibleHeight => -1f;

        int ILayoutElement.layoutPriority => 0;

        internal void Begin()
        {
            if (rowTemplate.activeSelf)
                rowTemplate.SetActive(false);
            entries.Clear();
        }

        internal void Event(HDCTrackedEvent tracked, bool withChannel, bool explain) =>
            entries.Add(new Entry { Key = tracked, Tracked = tracked, Event = tracked.Event, Look = Look(withChannel, explain) });

        internal void Count(int count, HDCAdEvent adEvent, bool explain) =>
            entries.Add(new Entry { Key = adEvent.type + "|" + adEvent.id, Event = adEvent, Count = count, Look = Counted | Look(true, explain) | count << 3 });

        internal void End()
        {
            claimed.Clear();
            picked.Clear();
            foreach (Entry entry in entries)
                picked.Add(byKey.TryGetValue(entry.Key, out Row kept) && claimed.Add(kept) ? kept : null);

            bool changed = picked.Count != order.Count;
            int free = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                Row row = picked[i];
                if (row == null)
                {
                    row = Free(ref free);
                    claimed.Add(row);
                    Rekey(row, entries[i].Key);
                    picked[i] = row;
                }

                if (row.Look != entries[i].Look)
                {
                    Fill(row.Rect, entries[i]);
                    row.Look = entries[i].Look;
                    row.Measured = false;
                    changed = true;
                }

                if (!row.Rect.gameObject.activeSelf)
                {
                    row.Rect.gameObject.SetActive(true);
                    changed = true;
                }

                changed |= i < order.Count && order[i] != row;
            }

            foreach (Row row in rows)
            {
                if (claimed.Contains(row) || !row.Rect.gameObject.activeSelf)
                    continue;
                row.Rect.gameObject.SetActive(false);
                changed = true;
            }

            order.Clear();
            order.AddRange(picked);
            if (changed)
                SetDirty();
        }

        void ILayoutElement.CalculateLayoutInputHorizontal()
        {
        }

        void ILayoutElement.CalculateLayoutInputVertical()
        {
            if (canvas == null)
                canvas = GetComponentInParent<Canvas>();
            float width = ((RectTransform)transform).rect.width;
            float scale = canvas != null ? canvas.rootCanvas.scaleFactor : 1f;
            bool resized = !Mathf.Approximately(width, measuredWidth) || !Mathf.Approximately(scale, measuredScale);
            measuredWidth = width;
            measuredScale = scale;
            height = 0f;
            for (int i = 0; i < order.Count; i++)
            {
                Row row = order[i];
                if (resized || !row.Measured)
                    Measure(row);

                height += (i > 0 ? spacing : 0f) + row.Height;
            }
        }

        void ILayoutController.SetLayoutHorizontal()
        {
        }

        void ILayoutController.SetLayoutVertical()
        {
            float top = 0f;
            foreach (Row row in order)
            {
                row.Rect.anchoredPosition = new Vector2(0f, -top);
                row.Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, row.Height);
                top += row.Height + spacing;
            }
        }

        private static void Measure(Row row)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(row.Rect);
            row.Height = LayoutUtility.GetPreferredHeight(row.Rect);
            row.Measured = true;
            if (Mathf.Approximately(row.Rect.rect.height, row.Height))
                return;
            row.Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, row.Height);
            LayoutRebuilder.ForceRebuildLayoutImmediate(row.Rect);
        }

        private void OnEnable() => SetDirty();

        private void OnDisable() => SetDirty();

        private void OnRectTransformDimensionsChange()
        {
            if (!CanvasUpdateRegistry.IsRebuildingLayout())
                SetDirty();
        }

        private void SetDirty() => LayoutRebuilder.MarkLayoutForRebuild((RectTransform)transform);

        private static int Look(bool withChannel, bool explain) => (withChannel ? WithChannel : 0) | (explain ? Explained : 0);

        private Row Free(ref int next)
        {
            while (next < rows.Count && claimed.Contains(rows[next]))
                next++;
            if (next < rows.Count)
                return rows[next++];
            GameObject made = Instantiate(rowTemplate, rowsRoot, false);
            made.name = "Event " + rows.Count;
            var row = new Row { Rect = (RectTransform)made.transform };
            rows.Add(row);
            next = rows.Count;
            return row;
        }

        private void Rekey(Row row, object key)
        {
            if (row.Key != null && byKey.TryGetValue(row.Key, out Row owner) && owner == row)
                byKey.Remove(row.Key);
            row.Key = key;
            row.Look = -1;
            byKey[key] = row;
        }

        private static void Fill(Transform row, Entry entry)
        {
            row.Find("Time").GetComponent<Text>().text = entry.Tracked != null ? HDCEventText.Time(entry.Tracked) : entry.Count + "×";
            row.Find("Badge").GetComponent<Image>().color = HDCEventText.TypeColor(entry.Event.type);
            row.Find("Badge/Label").GetComponent<Text>().text = entry.Event.type;
            row.Find("Main/Title").GetComponent<Text>().text = HDCEventText.Subject(entry.Event, (entry.Look & WithChannel) != 0);
            Optional(row.Find("Main/Detail"), HDCEventText.Detail(entry.Event));
            Optional(row.Find("Main/Explain"), (entry.Look & Explained) != 0 ? HDCEventText.Explain(entry.Event) : null);
        }

        private static void Optional(Transform line, string text)
        {
            bool shown = !string.IsNullOrEmpty(text);
            HDCDebugStyle.SetVisible(line, shown);
            if (shown)
                line.GetComponent<Text>().text = text;
        }
    }
}
