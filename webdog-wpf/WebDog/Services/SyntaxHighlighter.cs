using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Documents;
using System.Windows.Media;

namespace WebDog.Services
{
    /// <summary>
    /// Builds a colored FlowDocument for JSON / XML / HTML response bodies.
    /// Uses token-based coloring without external dependencies.
    /// </summary>
    public static class SyntaxHighlighter
    {
        private static readonly Brush CKey = Brush("#79C0FF");
        private static readonly Brush CString = Brush("#A5D6FF");
        private static readonly Brush CNumber = Brush("#79C0FF");
        private static readonly Brush CBoolNull = Brush("#FF7B72");
        private static readonly Brush CPunct = Brush("#8B949E");
        private static readonly Brush CTag = Brush("#7EE787");
        private static readonly Brush CAttr = Brush("#79C0FF");
        private static readonly Brush CComment = Brush("#6E7681");
        private static readonly Brush CDefault = Brush("#C9D1D9");
        private static readonly Brush CError = Brush("#F87171");

        private static Brush Brush(string hex)
        {
            var c = (Color)ColorConverter.ConvertFromString(hex);
            var b = new SolidColorBrush(c);
            b.Freeze();
            return b;
        }

        public enum Language { Json, Xml, Html, Plain }

        public static Language Detect(string contentType)
        {
            if (string.IsNullOrEmpty(contentType)) return Language.Plain;
            var ct = contentType.ToLowerInvariant();
            if (ct.Contains("json")) return Language.Json;
            if (ct.Contains("html")) return Language.Html;
            if (ct.Contains("xml")) return Language.Xml;
            return Language.Plain;
        }

        public static FlowDocument Build(string text, Language lang)
        {
            var doc = new FlowDocument
            {
                FontFamily = new System.Windows.Media.FontFamily("Cascadia Code, Consolas"),
                FontSize = 13,
                Foreground = CDefault,
                PagePadding = new System.Windows.Thickness(16, 12, 16, 12),
            };

            if (string.IsNullOrEmpty(text))
            {
                doc.Blocks.Add(new Paragraph(new Run("")));
                return doc;
            }

            // Limit body size for syntax highlighting (prevent crash on huge responses)
            const int maxLen = 500_000;
            if (text.Length > maxLen)
            {
                AppendPlain(doc, text);
                return doc;
            }

            try
            {
                switch (lang)
                {
                    case Language.Json: AppendJson(doc, text); break;
                    case Language.Xml: AppendMarkup(doc, text); break;
                    case Language.Html: AppendMarkup(doc, text); break;
                    default: AppendPlain(doc, text); break;
                }
            }
            catch
            {
                AppendPlain(doc, text);
            }

            return doc;
        }

        // ---- JSON ----
        private static void AppendJson(FlowDocument doc, string text)
        {
            var para = new Paragraph();
            // Tokenize the JSON while preserving structure.
            var reader = new JsonReader(text);
            while (reader.Read(out var tokenType, out var value))
            {
                switch (tokenType)
                {
                    case JsonReader.TokenType.Key:
                        para.Inlines.Add(new Run(value) { Foreground = CKey });
                        break;
                    case JsonReader.TokenType.String:
                        para.Inlines.Add(new Run(value) { Foreground = CString });
                        break;
                    case JsonReader.TokenType.Number:
                        para.Inlines.Add(new Run(value) { Foreground = CNumber });
                        break;
                    case JsonReader.TokenType.BoolNull:
                        para.Inlines.Add(new Run(value) { Foreground = CBoolNull });
                        break;
                    case JsonReader.TokenType.Punct:
                        para.Inlines.Add(new Run(value) { Foreground = CPunct });
                        break;
                    case JsonReader.TokenType.Whitespace:
                        para.Inlines.Add(new Run(value));
                        break;
                }
            }
            doc.Blocks.Add(para);
        }

        // ---- XML / HTML ----
        private static void AppendMarkup(FlowDocument doc, string text)
        {
            var para = new Paragraph();
            // Match: comments, tags (open/close/self), text in between.
            const string pattern = @"(?<comment><!--[\s\S]*?-->)|(?<tag></?[A-Za-z][^>]*?>)|(?<text>[^<]+)";
            foreach (Match m in Regex.Matches(text, pattern))
            {
                if (m.Groups["comment"].Success)
                {
                    para.Inlines.Add(new Run(m.Value) { Foreground = CComment });
                }
                else if (m.Groups["tag"].Success)
                {
                    AppendTag(para, m.Value);
                }
                else
                {
                    para.Inlines.Add(new Run(m.Value) { Foreground = CDefault });
                }
            }
            doc.Blocks.Add(para);
        }

        private static void AppendTag(Paragraph para, string tag)
        {
            // Color the angle brackets + tag name, then color attributes.
            para.Inlines.Add(new Run("<") { Foreground = CPunct });
            var inner = tag.TrimStart('<').TrimEnd('>').TrimEnd('/');
            var hadSlash = tag.EndsWith("/>");
            if (tag.StartsWith("</")) inner = "/" + inner.TrimStart('/');

            // Split tag name and attributes
            int spaceIdx = inner.IndexOf(' ');
            string name = spaceIdx < 0 ? inner : inner[..spaceIdx];
            string attrs = spaceIdx < 0 ? "" : inner[(spaceIdx + 1)..];

            para.Inlines.Add(new Run(name) { Foreground = CTag });

            // Attributes: name="value"
            const string attrPattern = @"(?<name>[A-Za-z_:][A-Za-z0-9_:.\-]*)(\s*=\s*""(?<val>[^""]*)"")?";
            foreach (Match am in Regex.Matches(attrs, attrPattern))
            {
                para.Inlines.Add(new Run(" ") { Foreground = CPunct });
                para.Inlines.Add(new Run(am.Groups["name"].Value) { Foreground = CAttr });
                if (am.Groups["val"].Success)
                {
                    para.Inlines.Add(new Run("=\"") { Foreground = CPunct });
                    para.Inlines.Add(new Run(am.Groups["val"].Value) { Foreground = CString });
                    para.Inlines.Add(new Run("\"") { Foreground = CPunct });
                }
            }

            para.Inlines.Add(new Run(hadSlash ? "/>" : ">") { Foreground = CPunct });
        }

        private static void AppendPlain(FlowDocument doc, string text)
        {
            var para = new Paragraph();
            foreach (var line in text.Split('\n'))
            {
                para.Inlines.Add(new Run(line) { Foreground = CDefault });
                para.Inlines.Add(new Run("\n"));
            }
            doc.Blocks.Add(para);
        }

        // ---- Minimal JSON tokenizer (string-aware, preserves whitespace) ----
        private struct JsonReader
        {
            public enum TokenType { Key, String, Number, BoolNull, Punct, Whitespace }
            private readonly string _s;
            private int _i;

            public JsonReader(string s) { _s = s; _i = 0; }

            public bool Read(out TokenType type, out string value)
            {
                if (_i >= _s.Length) { type = default; value = null; return false; }
                char c = _s[_i];

                if (char.IsWhiteSpace(c))
                {
                    int start = _i;
                    while (_i < _s.Length && char.IsWhiteSpace(_s[_i])) _i++;
                    type = TokenType.Whitespace; value = _s[start.._i]; return true;
                }

                if (c == '"')
                {
                    var sb = new StringBuilder();
                    sb.Append('"'); _i++;
                    while (_i < _s.Length && _s[_i] != '"')
                    {
                        if (_s[_i] == '\\' && _i + 1 < _s.Length) { sb.Append(_s[_i]); sb.Append(_s[_i + 1]); _i += 2; }
                        else { sb.Append(_s[_i]); _i++; }
                    }
                    if (_i < _s.Length) { sb.Append('"'); _i++; }
                    value = sb.ToString();

                    // Determine if this string is a key (followed by optional ws then ':')
                    int j = _i;
                    while (j < _s.Length && char.IsWhiteSpace(_s[j])) j++;
                    type = (j < _s.Length && _s[j] == ':') ? TokenType.Key : TokenType.String;
                    return true;
                }

                if (c == '{' || c == '}' || c == '[' || c == ']' || c == ':' || c == ',')
                {
                    _i++; type = TokenType.Punct; value = c.ToString(); return true;
                }

                // Number or literal (true/false/null)
                int st = _i;
                while (_i < _s.Length && !char.IsWhiteSpace(_s[_i]) && _s[_i] != ',' && _s[_i] != '}' && _s[_i] != ']' && _s[_i] != '"')
                    _i++;
                var tok = _s[st.._i];
                if (tok == "true" || tok == "false" || tok == "null")
                { type = TokenType.BoolNull; value = tok; return true; }
                type = TokenType.Number; value = tok; return true;
            }
        }
    }
}
