using System.Text;

namespace HDC.Ads.Tests
{
    /// <summary>Reads C# source as code only, for rules that search it.</summary>
    internal static class HDCCSharpText
    {
        /// <summary>
        /// The source with comments, strings and character literals blanked out. Every other character, line
        /// breaks included, stays where it was, so line numbers still match the file. Holes in interpolated
        /// strings are blanked too.
        /// </summary>
        internal static string CodeOnly(string source)
        {
            var code = new StringBuilder(source.Length);
            int i = 0;
            while (i < source.Length)
            {
                int end = LiteralOrCommentEnd(source, i);
                if (end == i)
                {
                    code.Append(source[i]);
                    i++;
                    continue;
                }

                for (; i < end; i++)
                    code.Append(source[i] == '\n' ? '\n' : ' ');
            }

            return code.ToString();
        }

        // Where the comment or literal starting at start ends, or start when none starts there.
        private static int LiteralOrCommentEnd(string source, int start)
        {
            char c = source[start];
            char next = start + 1 < source.Length ? source[start + 1] : '\0';
            if (c == '/' && next == '/')
            {
                int lineEnd = source.IndexOf('\n', start);
                return lineEnd < 0 ? source.Length : lineEnd;
            }

            if (c == '/' && next == '*')
            {
                int commentEnd = source.IndexOf("*/", start + 2, System.StringComparison.Ordinal);
                return commentEnd < 0 ? source.Length : commentEnd + 2;
            }

            if (c == '\'')
                return QuotedEnd(source, start + 1, '\'', false);

            // "...", @"...", $"...", $@"..." and @$"...".
            int quote = start;
            bool verbatim = false;
            while (quote < source.Length && quote - start < 2 && (source[quote] == '@' || source[quote] == '$'))
            {
                verbatim |= source[quote] == '@';
                quote++;
            }

            if (quote < source.Length && source[quote] == '"' && (quote == start || !IsIdentifierPart(start > 0 ? source[start - 1] : ' ')))
                return QuotedEnd(source, quote + 1, '"', verbatim);
            return start;
        }

        private static int QuotedEnd(string source, int from, char quote, bool verbatim)
        {
            for (int i = from; i < source.Length; i++)
            {
                char c = source[i];
                if (!verbatim && c == '\\')
                {
                    i++;
                    continue;
                }

                if (!verbatim && c == '\n')
                    return i;
                if (c != quote)
                    continue;
                if (verbatim && i + 1 < source.Length && source[i + 1] == quote)
                {
                    i++;
                    continue;
                }

                return i + 1;
            }

            return source.Length;
        }

        private static bool IsIdentifierPart(char c) => char.IsLetterOrDigit(c) || c == '_';
    }
}
