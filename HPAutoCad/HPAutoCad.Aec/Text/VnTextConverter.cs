using System.Text;

namespace HPAutoCad.Aec.Text;

/// <summary>The result of converting one text value: the new text, whether anything changed, and the fonts met on the way.</summary>
public sealed record VnTextConversion(string Text, bool Changed, int RunsConverted, IReadOnlyList<string> LegacyFonts, IReadOnlyList<string> UnmappedFonts)
{
    public static VnTextConversion Unchanged(string text) => new(text, false, 0, [], []);
}

/// <summary>
///     Converts the legacy-font parts of a text value to Unicode. Plain text (DBText, attributes) is one run in the style's font. MText
///     contents (MText, dimension overrides, multileaders, table cells) is walked code by code: every format code is copied verbatim
///     (<c>\P</c>, <c>\H…;</c>, <c>\U+00B0</c>, <c>\\</c>, <c>{</c>/<c>}</c>…), each text run is converted in the font in force at that
///     point (the style's, or the last <c>\f</c> inside the current <c>{}</c> group), and a <c>\f</c> naming a legacy font is rewritten to
///     its Unicode face with its flags kept. A run in Symbol stays untouched (<c>f</c> is φ there), and so does a run in any font the rules
///     do not know — unless the caller forces <paramref name="forced"/>.
/// </summary>
public sealed class VnTextConverter(VnFontRules rules, LegacyCharset forced = LegacyCharset.None)
{
    private VnFontRules Rules => rules;

    public VnTextConversion ConvertPlain(string text, VnFontInfo styleFont)
    {
        var (charset, upper) = Effective(styleFont);
        if (!NeedsConversion(text, charset, upper)) return VnTextConversion.Unchanged(text);
        var converted = LegacyTextDecoder.Decode(text, charset, upper);
        return new VnTextConversion(converted, converted != text, 1, [], []);
    }

    /// <summary>
    ///     A run changes when it holds legacy bytes — or, under a capitals-only font (.VnTimeH), any lower-case letter: the font draws it as
    ///     a capital, and once the style becomes a normal Unicode face it would show in lower case.
    /// </summary>
    private static bool NeedsConversion(string run, LegacyCharset charset, bool upper) =>
        charset != LegacyCharset.None && (LegacyTextDecoder.HasLegacyBytes(run) || (upper && run.Any(char.IsLower)));

    public VnTextConversion ConvertMText(string contents, VnFontInfo styleFont)
    {
        var walk = new Walk(this, styleFont);
        var i = 0;
        while (i < contents.Length)
        {
            var ch = contents[i];
            if (ch == '\\' && i + 1 < contents.Length) i = walk.Code(contents, i);
            else if (ch == '{') i = walk.Open(i);
            else if (ch == '}') i = walk.Close(i);
            else i = walk.Text(contents, i);
        }

        return walk.Finish(contents);
    }

    private (LegacyCharset Charset, bool Upper) Effective(VnFontInfo font) => font.Kind switch
    {
        VnFontKind.Legacy => (font.Charset, font.UpperCaseOnly),
        VnFontKind.Unknown when forced != LegacyCharset.None => (forced, false),
        _ => (LegacyCharset.None, false),
    };

    /// <summary>The state of one MText walk: output, the font stack of <c>{}</c> groups, and what was converted.</summary>
    private sealed class Walk(VnTextConverter owner, VnFontInfo styleFont)
    {
        private readonly StringBuilder _output = new();
        private readonly Stack<VnFontInfo> _groups = new();
        private readonly SortedSet<string> _legacyFonts = new(StringComparer.OrdinalIgnoreCase);
        private readonly SortedSet<string> _unmapped = new(StringComparer.OrdinalIgnoreCase);
        private VnFontInfo _font = styleFont;
        private int _runs;

        public int Open(int i)
        {
            _groups.Push(_font);
            _output.Append('{');
            return i + 1;
        }

        public int Close(int i)
        {
            if (_groups.Count > 0) _font = _groups.Pop();
            _output.Append('}');
            return i + 1;
        }

        public int Text(string contents, int i)
        {
            // The first character is always consumed: it may be a lone backslash ending the contents, which no code follows.
            var end = i + 1;
            while (end < contents.Length && contents[end] is not ('\\' or '{' or '}')) end++;
            var run = contents[i..end];
            var (charset, upper) = owner.Effective(_font);
            if (NeedsConversion(run, charset, upper))
            {
                _output.Append(LegacyTextDecoder.Decode(run, charset, upper));
                _runs++;
            }
            else _output.Append(run);

            return end;
        }

        public int Code(string contents, int i)
        {
            var letter = contents[i + 1];
            var end = letter switch
            {
                '\\' or '{' or '}' or 'P' or 'N' or '~' or 'L' or 'l' or 'O' or 'o' or 'K' or 'k' or 'X' => i + 2,
                'U' when Follows(contents, i + 2, "+") => Math.Min(contents.Length, i + 7),
                'M' when Follows(contents, i + 2, "+") => Math.Min(contents.Length, i + 8),
                'f' or 'F' or 'A' or 'H' or 'W' or 'Q' or 'T' or 'C' or 'c' or 'p' or 'S' => UntilSemicolon(contents, i),
                _ => i + 2,
            };
            var code = contents[i..end];
            _output.Append(letter is 'f' or 'F' ? Font(code) : code);
            return end;
        }

        /// <summary><c>\fFace|b0|i0|c0|p34;</c> (TrueType) or <c>\Ffile.shx|…;</c>: switches the run font, rewritten when it is a legacy face.</summary>
        private string Font(string code)
        {
            var body = code.Length > 2 ? code[2..].TrimEnd(';') : "";
            var bar = body.IndexOf('|');
            var name = bar < 0 ? body : body[..bar];
            var flags = bar < 0 ? "" : body[bar..];
            _font = owner.Rules.Classify(name, name);
            if (!_font.IsLegacy) return code;
            _legacyFonts.Add(name);
            if (_font.TargetFace is not { } target)
            {
                _unmapped.Add(name);
                return code;
            }

            // \F names an SHX file; its Unicode target is a TrueType face, which MText writes as \f with flags.
            return code[1] == 'F' || flags.Length == 0 ? $"\\f{target}|b0|i0|c0|p34;" : $"\\f{target}{flags};";
        }

        public VnTextConversion Finish(string original)
        {
            var text = _output.ToString();
            return new VnTextConversion(text, text != original, _runs, _legacyFonts.ToArray(), _unmapped.ToArray());
        }

        private static bool Follows(string s, int at, string what) => at + what.Length <= s.Length && string.CompareOrdinal(s, at, what, 0, what.Length) == 0;

        private static int UntilSemicolon(string s, int i)
        {
            var semicolon = s.IndexOf(';', i + 2);
            return semicolon < 0 ? s.Length : semicolon + 1;
        }
    }
}
