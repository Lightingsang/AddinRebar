using System.Text;
using System.Text.Json;
using VNConvert;
using Xunit;

namespace HPAutoCad.Aec.Tests.VnConvert;

/// <summary>
///     The vendored VNConvert port is machine-written; these tests hold it against the tables UniKey's author wrote by hand
///     (vnconv/data.cpp, turned into TestData/vn-charset-golden.json by tools/vnconvert). Every Vietnamese letter, both
///     legacy charsets, both directions.
/// </summary>
public sealed class VnCharsetGoldenTests
{
    private const int AlphabetEntries = 186; // the 27 entries after 'z' are western symbols, not letters

    public static TheoryData<string> Charsets => new() { "tcvn3", "vniWin" };

    [Fact]
    public void Golden_table_holds_every_letter_of_the_upstream_alphabet()
    {
        var letters = GoldenTable.Letters();

        Assert.Equal(AlphabetEntries, letters.Count);
        Assert.Contains(letters, entry => entry.Unicode == 'ữ');
        Assert.Contains(letters, entry => entry.Unicode == 'Đ');
    }

    [Theory]
    [MemberData(nameof(Charsets))]
    public void Every_letter_encodes_to_the_upstream_bytes(string charset)
    {
        foreach (var entry in GoldenTable.Letters().Where(entry => entry.Code(charset) != 0))
        {
            byte[] unicode = Encoding.Unicode.GetBytes(entry.Unicode.ToString());
            byte[] encoded = VnConverter.Convert(unicode, VnCharsetId.Unicode, Charset(charset), VnConvOptions.Default);

            Assert.True(entry.Bytes(charset).SequenceEqual(encoded), $"{charset} {entry.Unicode} (index {entry.Index})");
        }
    }

    [Theory]
    [MemberData(nameof(Charsets))]
    public void Every_code_decodes_to_its_letter_and_shared_codes_to_the_lowercase_letter(string charset)
    {
        foreach (var group in GoldenTable.Letters().Where(entry => entry.Code(charset) != 0).GroupBy(entry => entry.Code(charset)))
        {
            var bytes = group.First().Bytes(charset);
            string decoded = Decode(bytes, Charset(charset));
            // TCVN3 has no code for upper-case toned letters: they share the lower-case code and a dedicated
            // upper-case font (.VnTimeH) draws them as capitals. Decoding therefore yields the lower-case letter.
            var expected = group.Count() == 1 ? group.Single().Unicode : group.Select(entry => entry.Unicode).Single(char.IsLower);

            Assert.Equal(expected.ToString(), decoded);
        }
    }

    [Fact]
    public void A_sentence_round_trips_through_both_legacy_charsets()
    {
        // No upper-case toned letter: TCVN3 cannot tell those from lower case (see the test above).
        const string sentence = "Cộng hòa Xã hội Chủ nghĩa Việt Nam - bê tông cốt thép B25, thép CB400-V, Đường kính D16";

        foreach (var charset in new[] { VnCharsetId.Tcvn3, VnCharsetId.VniWin })
        {
            byte[] legacy = VnConverter.Convert(Encoding.Unicode.GetBytes(sentence), VnCharsetId.Unicode, charset, VnConvOptions.Default);

            Assert.Equal(sentence, Decode(legacy, charset));
        }
    }

    private static string Decode(byte[] bytes, VnCharsetId charset) =>
        Encoding.Unicode.GetString(VnConverter.Convert(bytes, charset, VnCharsetId.Unicode, VnConvOptions.Default));

    private static VnCharsetId Charset(string name) => name == "tcvn3" ? VnCharsetId.Tcvn3 : VnCharsetId.VniWin;

    private sealed record GoldenEntry(int Index, char Unicode, int Tcvn3, int VniWin)
    {
        public int Code(string charset) => charset == "tcvn3" ? Tcvn3 : VniWin;

        /// <summary>VNI-Win stores a word: low byte = base letter, high byte = tone/diacritic mark, written in that order.</summary>
        public byte[] Bytes(string charset)
        {
            int code = Code(charset);
            return code > 0xFF ? [(byte)(code & 0xFF), (byte)(code >> 8)] : [(byte)code];
        }
    }

    private static class GoldenTable
    {
        public static List<GoldenEntry> Letters()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "TestData", "vn-charset-golden.json");
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            return document.RootElement.GetProperty("entries").EnumerateArray()
                .Select(entry => new GoldenEntry(
                    entry.GetProperty("index").GetInt32(),
                    (char)entry.GetProperty("unicode").GetInt32(),
                    entry.GetProperty("tcvn3").GetInt32(),
                    entry.GetProperty("vniWin").GetInt32()))
                .Where(entry => entry.Index < AlphabetEntries)
                .ToList();
        }
    }
}
