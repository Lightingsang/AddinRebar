using HPAutoCad.Aec.Text;
using Xunit;

namespace HPAutoCad.Aec.Tests.VnConvert;

/// <summary>
///     The legacy-text layer on strings taken code unit for code unit from a real structural-notes drawing (VNI-Avo styles, Symbol φ,
///     a Microsoft Sans Serif run holding a real Unicode ≥, a \U+00B0 escape) plus synthetic TCVN3 cases.
/// </summary>
public sealed class VnTextConverterTests
{
    private static readonly VnFontRules Rules = VnFontRules.Default;
    private static readonly VnFontInfo VniAvo = Rules.Classify("VNI-Avo", "vavon__0.TTF");
    private static readonly VnFontInfo Arial = Rules.Classify("Arial", "arial.ttf");

    private static string Bytes(params int[] codes) => new(codes.Select(c => (char)c).ToArray());

    [Theory]
    [InlineData("VNI-Times", "VTIMESN0.TTF", LegacyCharset.VniWin, "Times New Roman")]
    [InlineData("VNI-Helve-Condense", "Vhelvcn.ttf", LegacyCharset.VniWin, "Arial Narrow")]
    [InlineData("VNI-Avo", "vavon__0.TTF", LegacyCharset.VniWin, "Arial")]
    [InlineData(".VnTime", "VNTIME.TTF", LegacyCharset.Tcvn3, "Times New Roman")]
    [InlineData(".VnArial NarrowH", "VNARIALNH.TTF", LegacyCharset.Tcvn3, "Arial Narrow")]
    [InlineData("", "vn_vni.shx", LegacyCharset.VniWin, null)]
    [InlineData("", "uv-abc.shx", LegacyCharset.Tcvn3, null)]
    public void Legacy_fonts_are_recognised_by_face_or_file(string face, string file, LegacyCharset charset, string? target)
    {
        var info = Rules.Classify(face, file);

        Assert.Equal(VnFontKind.Legacy, info.Kind);
        Assert.Equal(charset, info.Charset);
        Assert.Equal(target, info.TargetFace);
    }

    [Fact]
    public void Symbol_is_never_legacy_and_arial_is_unknown()
    {
        Assert.Equal(VnFontKind.NotLegacy, Rules.Classify("Symbol", "symbol.ttf").Kind);
        Assert.Equal(VnFontKind.Unknown, Arial.Kind);
        Assert.True(Rules.Classify(".VnTimeH", "").UpperCaseOnly);
    }

    [Fact]
    public void Font_map_chooses_the_target_face_but_never_makes_a_font_legacy()
    {
        var mapped = Rules.WithFontMap([new("vn_vni.shx", "Arial"), new("Arial", "Tahoma")]);

        Assert.Equal("Arial", mapped.Classify("", "vn_vni.shx").TargetFace);
        Assert.Equal(VnFontKind.Unknown, mapped.Classify("Arial", "arial.ttf").Kind);
        Assert.Throws<ArgumentException>(() => Rules.WithFontMap([new("VNI-Avo", " ")]));
    }

    [Fact]
    public void Real_vni_mtext_with_paragraph_codes_becomes_unicode()
    {
        // "ÑÔN VÒ THI COÂNG" as stored (D1 D4 4E 20 56 D2 …), \P kept.
        var contents = "1. " + Bytes(0xD1, 0xD4) + "N V" + Bytes(0xD2) + " THI CO" + Bytes(0xC2) + "NG\\P   VE" + Bytes(0xD5) + " CH" + Bytes(0xC6) + " DA" + Bytes(0xC3) + "N";

        var result = new VnTextConverter(Rules).ConvertMText(contents, VniAvo);

        Assert.Equal("1. ĐƠN VỊ THI CÔNG\\P   VẼ CHỈ DẪN", result.Text);
        Assert.True(result.Changed);
    }

    [Fact]
    public void Inline_legacy_font_is_rewritten_with_its_flags_and_symbol_runs_stay()
    {
        var contents = "{\\fVNI-Avo|b0|i0|c0|p2;TE" + Bytes(0xC2) + "N}{\\fSymbol|b0|i0|c2|p18;f}6a500";

        var result = new VnTextConverter(Rules).ConvertMText(contents, VniAvo);

        Assert.Equal("{\\fArial|b0|i0|c0|p2;TÊN}{\\fSymbol|b0|i0|c2|p18;f}6a500", result.Text);
        Assert.Contains("VNI-Avo", result.LegacyFonts);
        Assert.Empty(result.UnmappedFonts);
    }

    [Fact]
    public void A_unicode_font_run_keeps_its_real_unicode_and_escapes_are_verbatim()
    {
        const string contents = "{\\fSymbol|b0|i0|c2|p18;\\C4;d\\fMicrosoft Sans Serif|b0|i0|c0|p34;\\H1.083x; ≥ 35mm}45\\U+00B0";

        var result = new VnTextConverter(Rules).ConvertMText(contents, VniAvo);

        Assert.Equal(contents, result.Text);
        Assert.False(result.Changed);
    }

    [Fact]
    public void A_group_restores_the_outer_font_after_it_closes()
    {
        // Inside {} the run is Arial (untouched); after } the style font (VNI) applies again.
        var contents = "{\\fArial|b0|i0|c0|p34;" + Bytes(0xC2) + "}" + "BA" + Bytes(0xC2);

        var result = new VnTextConverter(Rules).ConvertMText(contents, VniAvo);

        Assert.Equal("{\\fArial|b0|i0|c0|p34;" + Bytes(0xC2) + "}BÂ", result.Text);
    }

    [Fact]
    public void Tcvn3_upper_case_font_draws_capitals()
    {
        var vnTimeH = Rules.Classify(".VnTimeH", "");
        // TCVN3 'ư' 0xAD, 'ờ' 0xEA: ".VnTimeH" draws them as Ư Ờ.
        var text = "tr" + Bytes(0xAD, 0xEA) + "ng";

        Assert.Equal("TRƯỜNG", new VnTextConverter(Rules).ConvertPlain(text, vnTimeH).Text);
        Assert.Equal("trường", new VnTextConverter(Rules).ConvertPlain(text, Rules.Classify(".VnTime", "")).Text);
    }

    [Fact]
    public void Unknown_fonts_are_untouched_unless_a_charset_is_forced()
    {
        var text = "C" + Bytes(0xD3, 0xC4) + "T";

        Assert.False(new VnTextConverter(Rules).ConvertPlain(text, Arial).Changed);
        Assert.Equal("CỘT", new VnTextConverter(Rules, LegacyCharset.VniWin).ConvertPlain("CO" + Bytes(0xC4) + "T", Arial).Text);
        Assert.False(new VnTextConverter(Rules, LegacyCharset.VniWin).ConvertPlain("x", Rules.Classify("Symbol", "")).Changed);
    }

    [Fact]
    public void Ascii_only_text_is_unchanged_and_shx_inline_font_becomes_truetype()
    {
        var mapped = Rules.WithFontMap([new("vn_vni.shx", "Arial")]);

        Assert.False(new VnTextConverter(Rules).ConvertMText("BTCT B25 \\P D16a200", VniAvo).Changed);
        var result = new VnTextConverter(mapped).ConvertMText("{\\Fvn_vni.shx|c0;BE" + Bytes(0xC2) + "}", VniAvo);
        Assert.Equal("{\\fArial|b0|i0|c0|p34;BÊ}", result.Text);
    }

    [Fact]
    public void An_inline_legacy_font_without_a_target_is_reported_unmapped()
    {
        var result = new VnTextConverter(Rules).ConvertMText("{\\fVNI-Univer|b0|i0|c0|p2;TE" + Bytes(0xC2) + "N}", Arial);

        Assert.Equal(["VNI-Univer"], result.UnmappedFonts);
        Assert.Equal("{\\fVNI-Univer|b0|i0|c0|p2;TÊN}", result.Text);
    }

    [Fact]
    public void Converting_twice_leaves_converted_text_alone()
    {
        var converter = new VnTextConverter(Rules);
        var once = converter.ConvertMText("TE" + Bytes(0xC2) + "N CA" + Bytes(0xC1) + "U KIE" + Bytes(0xC4) + "N \\P CO" + Bytes(0xC4) + "T", VniAvo);

        Assert.Equal("TÊN CẤU KIỆN \\P CỘT", once.Text);
        Assert.False(converter.ConvertMText(once.Text, VniAvo).Changed);
    }

    [Fact]
    public void A_trailing_backslash_ends_the_walk()
    {
        // A lone backslash closing the contents follows no code: the walk must still move on (it runs on AutoCAD's main thread).
        var task = Task.Run(() => new VnTextConverter(Rules).ConvertMText("TE" + Bytes(0xC2) + "N\\", VniAvo));

        Assert.True(task.Wait(TimeSpan.FromSeconds(5)), "ConvertMText did not return");
        Assert.Equal("TÊN\\", task.Result.Text);
    }

    [Fact]
    public void Capitals_only_font_upper_cases_plain_ascii_too()
    {
        // .VnTimeH draws "ghi chu" as GHI CHU; after the style becomes Times New Roman only real capitals keep that look.
        var result = new VnTextConverter(Rules).ConvertMText("ghi chu \\P B25", Rules.Classify(".VnTimeH", ""));

        Assert.Equal("GHI CHU \\P B25", result.Text);
    }

    [Fact]
    public void A_truetype_style_is_judged_by_its_face_not_a_stale_file_name()
    {
        Assert.Equal(VnFontKind.Unknown, Rules.Classify("Arial", "VTIMESN0.TTF").Kind);
        Assert.Equal(LegacyCharset.VniWin, Rules.Classify("", "vn_vni.shx").Charset);
    }

    [Fact]
    public void Windows_1252_glyphs_map_back_to_their_bytes()
    {
        // TCVN3 0x80..0x9F are not letters, so check the mapping through HasLegacyBytes and a pass-through of real Unicode.
        Assert.True(LegacyTextDecoder.HasLegacyBytes("€"));
        Assert.False(LegacyTextDecoder.HasLegacyBytes("≥ abc"));
        Assert.Equal("a≥b", LegacyTextDecoder.Decode("a≥b", LegacyCharset.VniWin, false));
    }
}
