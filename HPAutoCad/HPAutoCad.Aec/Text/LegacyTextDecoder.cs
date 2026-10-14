using System.Text;
using VNConvert;

namespace HPAutoCad.Aec.Text;

/// <summary>
///     Turns text drawn with a legacy Vietnamese font back into Unicode. AutoCAD (DWGCODEPAGE ANSI_1252) stores each legacy byte as the
///     code unit of the same value — VNI <c>ĐƠN</c> arrives as <c>ÑÔN</c> (0xD1 0xD4 0x4E) — and the 0x80–0x9F bytes as their
///     Windows-1252 glyphs (U+20AC…). Those code units become bytes again and go through the vendored converter; any other character
///     (a real Unicode <c>≥</c> typed in later) is kept as it is.
/// </summary>
public static class LegacyTextDecoder
{
    /// <summary>Windows-1252 code points for bytes 0x80–0x9F (0 = byte undefined in 1252).</summary>
    private static readonly char[] Cp1252High =
    [
        '€', '\0', '‚', 'ƒ', '„', '…', '†', '‡', 'ˆ', '‰', 'Š', '‹', 'Œ', '\0', 'Ž', '\0',
        '\0', '‘', '’', '“', '”', '•', '–', '—', '˜', '™', 'š', '›', 'œ', '\0', 'ž', 'Ÿ',
    ];

    private static readonly Dictionary<char, byte> Cp1252ToByte = Cp1252High
        .Select((ch, i) => (ch, b: (byte)(0x80 + i)))
        .Where(pair => pair.ch != '\0')
        .ToDictionary(pair => pair.ch, pair => pair.b);

    /// <summary>
    ///     True when the text holds a code unit a legacy font draws as a Vietnamese letter (anything above ASCII that maps back to a byte)
    ///     and no letter only Unicode Vietnamese has (ă đ ơ ư ĩ ũ, U+1EA0–U+1EF9): such text is converted already — run twice, or typed in
    ///     Unicode under a legacy style — and converting it again would corrupt its Latin-1 letters (Ê is a VNI tone byte).
    /// </summary>
    public static bool HasLegacyBytes(string text)
    {
        var legacy = false;
        foreach (var ch in text)
        {
            if (IsUnicodeVietnamese(ch)) return false;
            if (ch > '\u007F' && TryByte(ch, out _)) legacy = true;
        }

        return legacy;
    }

    private static bool IsUnicodeVietnamese(char ch) =>
        ch is >= 'Ạ' and <= 'ỹ' or 'Ă' or 'ă' or 'Đ' or 'đ' or 'Ĩ' or 'ĩ' or 'Ũ' or 'ũ' or 'Ơ' or 'ơ' or 'Ư' or 'ư';

    /// <summary>
    ///     Converts <paramref name="text"/> from <paramref name="charset"/> to Unicode; <paramref name="upperCase"/> for fonts that draw
    ///     only capitals (.VnTimeH), whose lower-case codes render as capitals. Pure ASCII stays byte-identical (and upper-cased when asked).
    /// </summary>
    public static string Decode(string text, LegacyCharset charset, bool upperCase)
    {
        if (charset == LegacyCharset.None || text.Length == 0) return text;
        var output = new StringBuilder(text.Length);
        var pending = new List<byte>(text.Length);
        foreach (var ch in text)
        {
            if (TryByte(ch, out var b))
            {
                pending.Add(b);
                continue;
            }

            Flush(pending, charset, upperCase, output);
            output.Append(ch);
        }

        Flush(pending, charset, upperCase, output);
        return output.ToString();
    }

    private static void Flush(List<byte> pending, LegacyCharset charset, bool upperCase, StringBuilder output)
    {
        if (pending.Count == 0) return;
        var options = VnConvOptions.Default;
        options.ToUpper = upperCase ? 1 : 0;
        var unicode = VnConverter.Convert(pending.ToArray(), Id(charset), VnCharsetId.Unicode, options);
        output.Append(Encoding.Unicode.GetString(unicode));
        pending.Clear();
    }

    private static bool TryByte(char ch, out byte value)
    {
        if (ch <= 'ÿ')
        {
            value = (byte)ch;
            return true;
        }

        return Cp1252ToByte.TryGetValue(ch, out value);
    }

    private static VnCharsetId Id(LegacyCharset charset) => charset == LegacyCharset.Tcvn3 ? VnCharsetId.Tcvn3 : VnCharsetId.VniWin;
}
