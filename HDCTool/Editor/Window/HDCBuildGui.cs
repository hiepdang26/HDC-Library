using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HDC.Tool.Editor
{
    internal static class HDCBuildGui
    {
        internal const float LabelWidth = 150f;
        internal const float ColumnLabelWidth = 112f;
        internal const float ShortField = 96f;
        internal const float MediumField = 200f;
        internal const float LongField = 300f;
        internal const float SmallButton = 40f;
        internal const float ButtonWidth = 76f;
        internal const float WideWindow = 720f;

        internal static readonly Color Blue = new Color(0.30f, 0.55f, 1.00f, 1f);
        internal static readonly Color Purple = new Color(0.65f, 0.45f, 1.00f, 1f);
        internal static readonly Color Red = new Color(1.00f, 0.37f, 0.34f, 1f);
        internal static readonly Color Amber = new Color(1.00f, 0.71f, 0.28f, 1f);
        internal static readonly Color Green = new Color(0.20f, 0.76f, 0.42f, 1f);
        internal static readonly Color Teal = new Color(0.18f, 0.77f, 0.71f, 1f);
        internal static readonly Color Sky = new Color(0.31f, 0.70f, 1.00f, 1f);
        internal static readonly Color Orange = new Color(1.00f, 0.62f, 0.26f, 1f);
        internal static readonly Color Indigo = new Color(0.48f, 0.51f, 1.00f, 1f);
        internal static readonly Color Yellow = new Color(0.96f, 0.77f, 0.26f, 1f);
        internal static readonly Color Cyan = new Color(0.13f, 0.76f, 0.84f, 1f);
        internal static readonly Color Slate = new Color(0.55f, 0.60f, 0.66f, 1f);
        internal static readonly Color Gray = new Color(0.55f, 0.55f, 0.55f, 1f);

        private static readonly Color Accent = Blue;
        private static readonly Color ErrorColor = new Color(0.92f, 0.35f, 0.32f, 1f);
        private static readonly Color WarningColor = new Color(0.95f, 0.72f, 0.25f, 1f);
        private static readonly Color OkColor = new Color(0.4f, 0.78f, 0.45f, 1f);
        private static readonly Color Line = new Color(0.5f, 0.5f, 0.5f, 0.35f);

        private static readonly Dictionary<string, bool> Folds = new Dictionary<string, bool>(StringComparer.Ordinal);
        private static readonly List<string> Cards = new List<string>();

        private static GUIStyle cardStyle;
        private static GUIStyle headerStyle;
        private static GUIStyle bodyStyle;
        private static GUIStyle titleStyle;
        private static GUIStyle subtitleStyle;
        private static GUIStyle pillStyle;
        private static GUIStyle hintStyle;
        private static Texture2D roundTexture;

        internal static bool Wide => EditorGUIUtility.currentViewWidth >= WideWindow;

        internal static Color Panel => EditorGUIUtility.isProSkin ? new Color(0.19f, 0.19f, 0.19f, 1f) : new Color(0.86f, 0.86f, 0.86f, 1f);

        internal static Color Edge => EditorGUIUtility.isProSkin ? new Color(0.09f, 0.09f, 0.09f, 1f) : new Color(0.62f, 0.62f, 0.62f, 1f);

        internal static Color Stripe => EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.035f) : new Color(0f, 0f, 0f, 0.04f);

        internal static string FoldoutKey(string key) => "HDC.Build.Fold." + key;

        internal static IReadOnlyList<string> CardKeys => Cards;

        internal static bool AllOpen => Every(true);

        internal static bool AllClosed => Every(false);

        internal static bool IsOpen(string key, bool open = true)
        {
            if (!Folds.TryGetValue(key, out bool current))
                Folds[key] = current = EditorPrefs.GetBool(FoldoutKey(key), open);
            return current;
        }

        internal static void SetOpen(string key, bool open)
        {
            if (Folds.TryGetValue(key, out bool current) && current == open && EditorPrefs.HasKey(FoldoutKey(key)))
                return;
            Folds[key] = open;
            EditorPrefs.SetBool(FoldoutKey(key), open);
        }

        internal static void SetAll(bool open)
        {
            foreach (string key in Cards)
                SetOpen(key, open);
        }

        internal static void Register(string key, bool open)
        {
            if (Cards.Contains(key))
                return;
            Cards.Add(key);
            IsOpen(key, open);
        }

        internal static void Forget(string key)
        {
            Cards.Remove(key);
            Folds.Remove(key);
        }

        private static bool Every(bool open)
        {
            for (int i = 0; i < Cards.Count; i++)
            {
                if (IsOpen(Cards[i]) != open)
                    return false;
            }

            return true;
        }

        internal static CardScope Card(string key, string title, Color accent, string subtitle = null, Action header = null, bool open = true) =>
            new CardScope(key, title, accent, subtitle, header, open);

        internal sealed class CardScope : IDisposable
        {
            private readonly float oldLabelWidth;

            internal CardScope(string key, string title, Color accent, string subtitle, Action header, bool open)
            {
                Styles();
                oldLabelWidth = EditorGUIUtility.labelWidth;
                Rect card = EditorGUILayout.BeginVertical(cardStyle);
                bool repaint = Event.current.type == EventType.Repaint;
                if (repaint)
                {
                    EditorGUI.DrawRect(card, Edge);
                    EditorGUI.DrawRect(new Rect(card.x + 1f, card.y + 1f, card.width - 2f, card.height - 2f), Panel);
                }

                Register(key, open);
                bool current = IsOpen(key, open);
                Rect head = EditorGUILayout.BeginHorizontal(headerStyle, GUILayout.Height(28f));
                if (repaint)
                {
                    EditorGUI.DrawRect(new Rect(head.x + 1f, head.y + 1f, head.width - 2f, head.height - 1f), Color.Lerp(Panel, accent, 0.2f));
                    if (current)
                        EditorGUI.DrawRect(new Rect(head.x + 1f, head.yMax - 1f, head.width - 2f, 1f), Edge);
                }

                bool next = GUILayout.Toggle(current, GUIContent.none, EditorStyles.foldout, GUILayout.Width(14f));
                if (GUILayout.Button(title, titleStyle))
                    next = !current;
                if (!string.IsNullOrEmpty(subtitle))
                    GUILayout.Label(subtitle, subtitleStyle);
                GUILayout.FlexibleSpace();
                header?.Invoke();
                EditorGUILayout.EndHorizontal();
                if (repaint)
                    EditorGUI.DrawRect(new Rect(card.x, card.y, 4f, card.height), accent);

                if (next != current)
                    SetOpen(key, next);
                Open = next;
                if (!Open)
                    return;
                EditorGUILayout.BeginVertical(bodyStyle);
                EditorGUIUtility.labelWidth = LabelWidth;
            }

            internal bool Open { get; }

            public void Dispose()
            {
                if (Open)
                {
                    EditorGUIUtility.labelWidth = oldLabelWidth;
                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.EndVertical();
            }
        }

        internal readonly struct LabelScope : IDisposable
        {
            private readonly float old;

            internal LabelScope(float width)
            {
                old = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = width;
            }

            public void Dispose() => EditorGUIUtility.labelWidth = old;
        }

        internal static void Pill(string text, Color color, string tooltip = null)
        {
            Styles();
            Color oldBackground = GUI.backgroundColor;
            Color oldContent = GUI.contentColor;
            GUI.backgroundColor = new Color(color.r, color.g, color.b, EditorGUIUtility.isProSkin ? 0.3f : 0.38f);
            GUI.contentColor = EditorGUIUtility.isProSkin ? Color.Lerp(color, Color.white, 0.55f) : Color.Lerp(color, Color.black, 0.55f);
            GUILayout.Label(new GUIContent(text, tooltip), pillStyle, GUILayout.ExpandWidth(false));
            GUI.backgroundColor = oldBackground;
            GUI.contentColor = oldContent;
        }

        internal static void Label(string text) => EditorGUILayout.LabelField(text, GUILayout.Width(EditorGUIUtility.labelWidth));

        internal static void Hint(string text)
        {
            Styles();
            EditorGUILayout.LabelField(" ", text, hintStyle);
        }

        internal static void Note(string text)
        {
            Styles();
            EditorGUILayout.LabelField(text, hintStyle);
        }

        internal static void Pair(Action left, Action right)
        {
            if (!Wide)
            {
                left();
                right();
                return;
            }

            float half = Mathf.Max(200f, (EditorGUIUtility.currentViewWidth - 96f) / 2f);
            using (new EditorGUILayout.HorizontalScope())
            using (new LabelScope(ColumnLabelWidth))
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(half)))
                    left();
                GUILayout.Space(18f);
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(half)))
                    right();
            }
        }

        internal static GUILayoutOption FieldWidth(float field) => GUILayout.Width(EditorGUIUtility.labelWidth + field);

        internal static string Text(string label, string value, float field)
        {
            string current = value ?? "";
            return field > 0f ? EditorGUILayout.DelayedTextField(label, current, FieldWidth(field)) : EditorGUILayout.DelayedTextField(label, current);
        }

        internal static bool Small(string text, string tooltip = null) =>
            GUILayout.Button(new GUIContent(text, tooltip), GUILayout.Width(SmallButton), GUILayout.Height(EditorGUIUtility.singleLineHeight));

        internal static bool Segment(string label, bool selected, int position, int count, Color color)
        {
            GUIStyle style = count == 1 ? EditorStyles.miniButton
                : position == 0 ? EditorStyles.miniButtonLeft
                : position == count - 1 ? EditorStyles.miniButtonRight : EditorStyles.miniButtonMid;
            Color old = GUI.backgroundColor;
            if (selected)
                GUI.backgroundColor = color;
            bool clicked = GUILayout.Button(label, style, GUILayout.Width(110f), GUILayout.Height(22f));
            GUI.backgroundColor = old;
            return clicked;
        }

        internal static void StripeRow(Rect rect, int index)
        {
            if (Event.current.type == EventType.Repaint && index % 2 == 0)
                EditorGUI.DrawRect(rect, Stripe);
        }

        internal static bool Foldout(string key, string title, bool open = false)
        {
            bool current = EditorPrefs.GetBool(FoldoutKey(key), open);
            bool next = EditorGUILayout.Foldout(current, title, true, EditorStyles.foldoutHeader);
            if (next != current)
                EditorPrefs.SetBool(FoldoutKey(key), next);
            return next;
        }

        internal static void Row(string label, string value)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(EditorGUIUtility.labelWidth));
                EditorGUILayout.LabelField(string.IsNullOrEmpty(value) ? "-" : value, EditorStyles.wordWrappedLabel);
            }
        }

        internal static bool Selectable(string label, bool selected, float width)
        {
            Color old = GUI.backgroundColor;
            if (selected)
                GUI.backgroundColor = Accent;
            bool clicked = GUILayout.Button(label, GUILayout.Width(width));
            GUI.backgroundColor = old;
            return clicked;
        }

        internal static bool Primary(string label, float width) => Selectable(label, true, width);

        internal static void Chip(string label, bool on) => Pill(label, on ? Green : Gray, on ? "On" : "Off");

        internal static void Level(HDCCheckLevel level, float width)
        {
            Color old = GUI.color;
            GUI.color = LevelColor(level);
            GUILayout.Label(level.ToString(), EditorStyles.miniButton, GUILayout.Width(width));
            GUI.color = old;
        }

        internal static Color LevelColor(HDCCheckLevel level) =>
            level == HDCCheckLevel.Error ? ErrorColor : level == HDCCheckLevel.Warning ? WarningColor : level == HDCCheckLevel.Ok ? OkColor : Gray;

        internal static GUIContent Icon(HDCCheckLevel level) =>
            EditorGUIUtility.IconContent(level == HDCCheckLevel.Error ? "console.erroricon.sml" : level == HDCCheckLevel.Warning ? "console.warnicon.sml" : "TestPassed");

        internal static void Divider()
        {
            GUILayout.Space(5);
            EditorGUI.DrawRect(GUILayoutUtility.GetRect(1f, 1f, GUILayout.ExpandWidth(true)), Line);
            GUILayout.Space(5);
        }

        internal static void AccentDivider() => EditorGUI.DrawRect(GUILayoutUtility.GetRect(1f, 3f, GUILayout.ExpandWidth(true)), Accent);

        private static Texture2D Round()
        {
            if (roundTexture != null)
                return roundTexture;
            const int size = 18;
            const float radius = 8.5f;
            roundTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            var pixels = new Color32[size * size];
            float half = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(Mathf.Abs(x + 0.5f - half) - (half - radius), 0f);
                    float dy = Mathf.Max(Mathf.Abs(y + 0.5f - half) - (half - radius), 0f);
                    float alpha = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            roundTexture.SetPixels32(pixels);
            roundTexture.Apply();
            return roundTexture;
        }

        private static void Styles()
        {
            if (cardStyle != null && roundTexture != null)
                return;
            cardStyle = new GUIStyle { margin = new RectOffset(8, 8, 2, 12), padding = new RectOffset(0, 0, 0, 0) };
            headerStyle = new GUIStyle { padding = new RectOffset(14, 10, 5, 3), margin = new RectOffset(0, 0, 0, 0) };
            bodyStyle = new GUIStyle { padding = new RectOffset(18, 14, 10, 12), margin = new RectOffset(0, 0, 0, 0) };
            titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12, margin = new RectOffset(2, 6, 3, 0), padding = new RectOffset(0, 0, 0, 0) };
            subtitleStyle = new GUIStyle(EditorStyles.miniLabel) { margin = new RectOffset(0, 4, 5, 0) };
            subtitleStyle.normal.textColor = Gray;
            pillStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                border = new RectOffset(8, 8, 8, 8),
                padding = new RectOffset(9, 9, 1, 2),
                margin = new RectOffset(2, 4, 3, 2),
                fixedHeight = 18f,
            };
            pillStyle.normal.background = Round();
            pillStyle.normal.textColor = Color.white;
            hintStyle = new GUIStyle(EditorStyles.wordWrappedMiniLabel);
            hintStyle.normal.textColor = Gray;
        }
    }
}
