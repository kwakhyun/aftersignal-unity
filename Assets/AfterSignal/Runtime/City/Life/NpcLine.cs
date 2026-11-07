using System;
using System.Globalization;
using System.Text;

namespace AfterSignal
{
    [Serializable]
    public sealed class NpcLine
    {
        public string role, content;
    }

    // Model, gateway and player text is shown in uGUI Text with rich text enabled: strip markup and control/format characters first.
    public static class NpcText
    {
        public static string Clean(string text, int max)
        {
            if (string.IsNullOrEmpty(text) || max <= 0)
                return "";
            if (text.Length > max * 4 + 64)
                text = text.Substring(0, max * 4 + 64);
            var sb = new StringBuilder(Math.Min(text.Length, max + 8));
            int breaks = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '<')
                {
                    // Drop tag-like runs (<b>, </color>, <size=9>); any bracket left over is made full-width so no tag can form.
                    char after = i + 1 < text.Length ? text[i + 1] : ' ';
                    int end = text.IndexOf('>', i + 1), next = text.IndexOf('<', i + 1);
                    if ((after == '/' || after < 128 && char.IsLetter(after)) && end > 0 && end - i <= 64 && (next < 0 || next > end)) { i = end; continue; }
                    c = '＜';
                }
                else if (c == '>') c = '＞';
                else if (c == '\t') c = ' ';
                if (c == '\n') { if (++breaks > 2) continue; sb.Append(c); continue; }
                var category = char.GetUnicodeCategory(c);
                if (category == UnicodeCategory.Control || category == UnicodeCategory.Format || category == UnicodeCategory.Surrogate || category == UnicodeCategory.PrivateUse || category == UnicodeCategory.OtherNotAssigned)
                    continue;
                breaks = 0;
                sb.Append(c);
            }
            string clean = sb.ToString().Trim();
            return clean.Length > max ? clean.Substring(0, max).TrimEnd() : clean;
        }
    }
}
