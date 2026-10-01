using System;
using System.Collections.Generic;
using System.Text;

namespace HDC.Ads.DebugUI
{
    /// <summary>
    /// JSON for reading: indented, with top-level keys that fold to one line, and colored as rich text.
    /// </summary>
    internal static class HDCJsonText
    {
        private const string Indent = "  ";

        /// <summary>A top-level key whose value is an object or an array, by its lines in the indented JSON.</summary>
        internal sealed class Section
        {
            internal string Key;
            internal int StartLine;
            internal int EndLine;
            internal char Kind;
            internal bool Comma;
        }

        /// <summary>Indents JSON two spaces per level. Text that is not an object or an array comes back trimmed.</summary>
        internal static string Pretty(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return string.Empty;
            string source = json.Trim();
            if (source[0] != '{' && source[0] != '[')
                return source;

            var text = new StringBuilder(source.Length * 2);
            int depth = 0;
            bool inString = false;
            bool escaped = false;
            for (int i = 0; i < source.Length; i++)
            {
                char c = source[i];
                if (inString)
                {
                    text.Append(c);
                    if (escaped)
                        escaped = false;
                    else if (c == '\\')
                        escaped = true;
                    else if (c == '"')
                        inString = false;
                    continue;
                }

                switch (c)
                {
                    case '"':
                        inString = true;
                        text.Append(c);
                        break;
                    case '{':
                    case '[':
                        int next = NextNonSpace(source, i + 1);
                        if (next < source.Length && (source[next] == '}' || source[next] == ']'))
                        {
                            // An empty object or array stays on its line.
                            text.Append(c).Append(source[next]);
                            i = next;
                            break;
                        }

                        text.Append(c);
                        NewLine(text, ++depth);
                        break;
                    case '}':
                    case ']':
                        depth = Math.Max(0, depth - 1);
                        NewLine(text, depth);
                        text.Append(c);
                        break;
                    case ',':
                        text.Append(c);
                        NewLine(text, depth);
                        break;
                    case ':':
                        text.Append(": ");
                        break;
                    default:
                        if (!char.IsWhiteSpace(c))
                            text.Append(c);
                        break;
                }
            }

            return text.ToString();
        }

        /// <summary>The top-level keys of indented JSON whose values span several lines.</summary>
        internal static List<Section> Sections(string prettyJson)
        {
            var sections = new List<Section>();
            if (string.IsNullOrEmpty(prettyJson))
                return sections;

            string[] lines = prettyJson.Split('\n');
            var depthAfter = new int[lines.Length];
            var depthBefore = new int[lines.Length];
            int depth = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                depthBefore[i] = depth;
                depth = ScanDepth(lines[i], depth);
                depthAfter[i] = depth;
            }

            for (int i = 0; i < lines.Length; i++)
            {
                if (depthBefore[i] != 1 || depthAfter[i] != 2 || !TryKey(lines[i], out string key, out char kind))
                    continue;
                int end = i;
                while (end < lines.Length && depthAfter[end] != 1)
                    end++;
                if (end >= lines.Length)
                    continue;
                sections.Add(new Section
                {
                    Key = key,
                    StartLine = i,
                    EndLine = end,
                    Kind = kind,
                    Comma = lines[end].TrimEnd().EndsWith(",", StringComparison.Ordinal),
                });
            }

            return sections;
        }

        /// <summary>Indented JSON with the <paramref name="folded"/> top-level keys on one line each.</summary>
        internal static string Fold(string prettyJson, List<Section> sections, ICollection<string> folded)
        {
            if (string.IsNullOrEmpty(prettyJson) || sections.Count == 0 || folded.Count == 0)
                return prettyJson;

            string[] lines = prettyJson.Split('\n');
            var text = new StringBuilder(prettyJson.Length);
            int line = 0;
            foreach (Section section in sections)
            {
                while (line < section.StartLine)
                    text.Append(lines[line++]).Append('\n');
                if (!folded.Contains(section.Key))
                    continue;
                text.Append(Indent).Append('"').Append(section.Key).Append("\": ")
                    .Append(section.Kind == '[' ? "[...]" : "{...}").Append(section.Comma ? "," : string.Empty).Append('\n');
                line = section.EndLine + 1;
            }

            while (line < lines.Length)
                text.Append(lines[line++]).Append('\n');
            return text.ToString().TrimEnd('\n');
        }

        /// <summary>Rich text of indented JSON: keys, strings, numbers and literals each in a color.</summary>
        internal static string Colored(string json)
        {
            if (string.IsNullOrEmpty(json))
                return string.Empty;

            var text = new StringBuilder(json.Length + json.Length / 2);
            int i = 0;
            while (i < json.Length)
            {
                char c = json[i];
                if (c == '"')
                {
                    int end = StringEnd(json, i);
                    string token = json.Substring(i, end - i + 1);
                    int after = NextNonSpace(json, end + 1);
                    bool key = after < json.Length && json[after] == ':';
                    text.Append("<color=").Append(key ? HDCDebugStyle.AccentHex : "#86EFAC").Append('>').Append(token).Append("</color>");
                    i = end + 1;
                }
                else if (c == '-' || char.IsDigit(c))
                {
                    int end = i;
                    while (end < json.Length && (char.IsDigit(json[end]) || "+-.eE".IndexOf(json[end]) >= 0))
                        end++;
                    text.Append("<color=#FCD34D>").Append(json, i, end - i).Append("</color>");
                    i = end;
                }
                else if (char.IsLetter(c))
                {
                    int end = i;
                    while (end < json.Length && char.IsLetter(json[end]))
                        end++;
                    text.Append("<color=").Append(HDCDebugStyle.InfoHex).Append('>').Append(json, i, end - i).Append("</color>");
                    i = end;
                }
                else
                {
                    text.Append(c);
                    i++;
                }
            }

            return text.ToString();
        }

        private static void NewLine(StringBuilder text, int depth)
        {
            text.Append('\n');
            for (int i = 0; i < depth; i++)
                text.Append(Indent);
        }

        private static int NextNonSpace(string text, int start)
        {
            int i = start;
            while (i < text.Length && char.IsWhiteSpace(text[i]))
                i++;
            return i;
        }

        // The index of the quote closing the string that opens at start, or the last index if it never closes.
        private static int StringEnd(string text, int start)
        {
            bool escaped = false;
            for (int i = start + 1; i < text.Length; i++)
            {
                if (escaped)
                    escaped = false;
                else if (text[i] == '\\')
                    escaped = true;
                else if (text[i] == '"')
                    return i;
            }

            return text.Length - 1;
        }

        private static int ScanDepth(string line, int depth)
        {
            bool inString = false;
            bool escaped = false;
            foreach (char c in line)
            {
                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (c == '\\')
                    escaped = true;
                else if (c == '"')
                    inString = !inString;
                else if (!inString && (c == '{' || c == '['))
                    depth++;
                else if (!inString && (c == '}' || c == ']') && depth > 0)
                    depth--;
            }

            return depth;
        }

        // A line like  "key": {  or  "key": [
        private static bool TryKey(string line, out string key, out char kind)
        {
            key = string.Empty;
            kind = '\0';
            string trimmed = line.TrimStart();
            if (!trimmed.StartsWith("\"", StringComparison.Ordinal))
                return false;
            int close = StringEnd(trimmed, 0);
            int colon = NextNonSpace(trimmed, close + 1);
            if (colon >= trimmed.Length || trimmed[colon] != ':')
                return false;
            int value = NextNonSpace(trimmed, colon + 1);
            if (value >= trimmed.Length || (trimmed[value] != '{' && trimmed[value] != '['))
                return false;
            key = trimmed.Substring(1, close - 1);
            kind = trimmed[value];
            return key.Length > 0;
        }
    }
}
