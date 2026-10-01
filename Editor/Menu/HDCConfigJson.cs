using System;
using System.Text;
using UnityEngine;

namespace HDC.Ads.Editor
{
    /// <summary>Checks and formats the JSON of the ads configs for the config window.</summary>
    internal static class HDCConfigJson
    {
        internal enum Kind
        {
            Ads,
            Core,
        }

        internal readonly struct Result
        {
            internal Result(bool isValid, string message)
            {
                IsValid = isValid;
                Message = message;
            }

            internal bool IsValid { get; }
            internal string Message { get; }
        }

        /// <summary>Parses <paramref name="json"/> and sums up what it configures.</summary>
        internal static Result Check(string json, Kind kind, bool mayBeEmpty)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return mayBeEmpty
                    ? new Result(true, "Empty: the Android config is used.")
                    : new Result(false, "Empty. Write a JSON object, at least {}.");
            }

            try
            {
                return kind == Kind.Ads ? SumUpAds(json) : SumUpCore(json);
            }
            catch (ArgumentException exception)
            {
                return new Result(false, exception.Message);
            }
        }

        /// <summary>Indents the JSON by two spaces per level; strings are kept as they are.</summary>
        internal static string Format(string json)
        {
            var text = new StringBuilder();
            int indent = 0;
            bool inString = false;
            bool escaped = false;
            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];
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
                        char close = c == '{' ? '}' : ']';
                        int next = NextNonSpace(json, i + 1);
                        if (next < json.Length && json[next] == close)
                        {
                            text.Append(c).Append(close);
                            i = next;
                            break;
                        }

                        text.Append(c);
                        NewLine(text, ++indent);
                        break;
                    case '}':
                    case ']':
                        NewLine(text, --indent);
                        text.Append(c);
                        break;
                    case ',':
                        text.Append(c);
                        NewLine(text, indent);
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

        private static Result SumUpAds(string json)
        {
            AdsSummary ads = JsonUtility.FromJson<AdsSummary>(json);
            return new Result(true, $"Valid. Ad core config key (selectedAdCoreName): '{ads.selectedAdCoreName}'.");
        }

        private static Result SumUpCore(string json)
        {
            CoreSummary core = JsonUtility.FromJson<CoreSummary>(json);
            int forceAdGroups = core.forceAdGroups?.Length ?? 0;
            int popupGroups = core.popupGroups?.Length ?? 0;
            return new Result(true, $"Valid. {forceAdGroups} force ad groups, {popupGroups} popup groups.");
        }

        private static int NextNonSpace(string json, int from)
        {
            while (from < json.Length && char.IsWhiteSpace(json[from]))
                from++;
            return from;
        }

        private static void NewLine(StringBuilder text, int indent) =>
            text.Append('\n').Append(' ', Math.Max(indent, 0) * 2);

#pragma warning disable 0649 // Assigned by JsonUtility.
        [Serializable]
        private sealed class AdsSummary
        {
            public string selectedAdCoreName;
        }

        [Serializable]
        private sealed class CoreSummary
        {
            public Named[] forceAdGroups;
            public Named[] popupGroups;
        }

        [Serializable]
        private sealed class Named
        {
            public string groupName;
        }
#pragma warning restore 0649
    }
}
