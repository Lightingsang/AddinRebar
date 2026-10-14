using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using HPAutoCad.Aec.Cad;
using HPAutoCad.Aec.Model;
using Xunit;

namespace HPAutoCad.Aec.Tests.VnConvert;

/// <summary>
///     Both Vietnamese-text envelopes at their caps, every string at its cut length and in 3-byte UTF-8 (the bridge writes Vietnamese
///     unescaped), stay under the bridge's 64 KB result cap with room to spare.
/// </summary>
public sealed class VnTextEnvelopeTests
{
    private const int Budget = 60_000;

    /// <summary>The bridge's own options: camelCase, nulls dropped, non-ASCII written raw (UnsafeRelaxedJsonEscaping).</summary>
    private static readonly JsonSerializerOptions Bridge = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static string Wide(int length) => new('ữ', length);

    [Fact]
    public void A_full_detect_page_stays_under_the_cap()
    {
        var item = new VnTextItem("FFFFFFFF", "AttributeDefinition", Wide(AecTools.VnNameChars) + "…", true, Wide(AecTools.VnNameChars) + "…", Wide(AecTools.VnNameChars) + "…",
            99, "cell[999,999]", Wide(AecTools.VnPreviewChars) + "…", Wide(AecTools.VnPreviewChars) + "…");
        var result = new AnalysisResult<VnTextItem>
        {
            Items = Enumerable.Repeat(item, AecTools.MaxVnTextLimit).ToArray(),
            Count = 100_000,
            Truncated = true,
            Summary = new
            {
                examined = 100_000,
                legacyStyles = Enumerable.Range(0, 30).Select(i => new { name = Wide(30), font = "VNI-Helve-Condense", charset = "VNI-Win", upperCaseOnly = false, isShx = false, targetFace = "Arial Narrow" }),
            },
        };
        for (var i = 0; i < 30; i++) result.Warn($"style '{Wide(30)}' uses the VNI-Win font 'VNI-Univer' with no default Unicode face: pass fontMap {{\"VNI-Univer\": \"Arial\"}}.");

        Assert.InRange(Bytes(result), 1, Budget);
    }

    [Fact]
    public void A_whole_drawing_conversion_envelope_stays_under_the_cap()
    {
        var result = new EditResult();
        for (var i = 0; i < 20_000; i++) result.Modified(((long)0x10000 + i).ToString("X"));
        var outcomes = Enumerable.Range(0, VnTextWriteService.MaxListedItems)
            .Select(i => new ItemOutcome(i, false, "FFFFFF", "AttributeReference", Error: ToolError.ForHandle(ToolErrorCode.LayerLocked, "FFFFFF", $"Entity FFFFFF is on locked layer '{Wide(60)}'.")))
            .ToArray();
        result.Settle(outcomes);
        result.Summary = new
        {
            byType = new { MText = 20_000 },
            blockDefinitions = Enumerable.Range(0, VnTextWriteService.MaxListedBlocks).ToDictionary(i => Wide(60) + i, i => i),
            stylesRemapped = Enumerable.Range(0, 30).Select(i => new { style = Wide(30), from = "VNI-Helve-Condense", to = "Arial Narrow", file = "arialn.ttf" }),
        };

        VnTextWriteService.TrimAffected(result, VnTextWriteService.MaxAffectedHandles);

        Assert.Equal(VnTextWriteService.MaxAffectedHandles, result.AffectedHandles.Count);
        Assert.Equal(20_000, result.ModifiedCount);
        Assert.Equal("14E1F", result.AffectedHandles[^1]); // the last handles (the styles) are kept
        Assert.InRange(Bytes(result), 1, Budget);
    }

    private static int Bytes(object value) => Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(value, Bridge));
}
