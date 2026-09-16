using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HPAutoCad.Aec.Cad;
using HPAutoCad.Aec.Model;
using Xunit;

namespace HPAutoCad.Aec.Tests;

public sealed class EditResultTests
{
    private static readonly JsonSerializerOptions Bridge = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, ReferenceHandler = ReferenceHandler.IgnoreCycles, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    /// <summary>The bridge's result cap; a safety margin below it because the run envelope wraps the tool value.</summary>
    private const int ResultBudgetBytes = 60_000;

    [Fact]
    public void Counts_and_affected_handles_follow_what_happened_in_order()
    {
        var r = new EditResult();
        r.Created("A1");
        r.Modified("B2");
        r.Modified("B2"); // a second change to the same entity counts once in the handle list
        r.Deleted("C3");

        Assert.Equal(1, r.CreatedCount);
        Assert.Equal(2, r.ModifiedCount);
        Assert.Equal(1, r.DeletedCount);
        Assert.Equal(["A1", "B2", "C3"], r.AffectedHandles);
        Assert.True(r.Success);
    }

    [Fact]
    public void Settle_copies_item_errors_and_success_means_every_item_went_through()
    {
        var r = new EditResult();
        r.Created("A1");
        var refused = new ToolError(ToolErrorCode.LayerLocked, "items[1]: Layer 'X' is locked");

        r.Settle([new ItemOutcome(0, true, "A1", "LINE"), new ItemOutcome(1, false, null, Error: refused)]);

        Assert.False(r.Success);
        Assert.Equal([refused], r.Errors);
        Assert.Equal(2, r.Items.Count);
        Assert.Equal(1, r.Items[1].Index);

        var clean = new EditResult();
        clean.Settle([new ItemOutcome(0, true, "A1", "LINE", ["layer", "color"])]);
        Assert.True(clean.Success);
        Assert.Equal(["layer", "color"], clean.Items[0].Changed);
    }

    [Fact]
    public void Item_errors_are_listed_at_the_top_up_to_a_cap_then_counted()
    {
        var r = new EditResult();
        r.Settle(Enumerable.Range(0, 50).Select(i => new ItemOutcome(i, false, null, Error: ToolError.ForHandle(ToolErrorCode.LayerLocked, $"{i:X}", $"items[{i}]: locked"))).ToArray());

        Assert.Equal(EditResult.MaxListedErrors + 1, r.Errors.Count);
        Assert.Contains("30 more item(s) failed", r.Errors[^1].Message);
        Assert.All(r.Items, i => Assert.NotNull(i.Error)); // every item still carries its own error
    }

    [Fact]
    public void Shared_warnings_are_grouped_with_the_item_indices_never_repeated_per_item()
    {
        var r = new EditResult();
        for (var i = 0; i < 15; i++) r.WarnItem(i, "layer 'FROZEN' is frozen: the entity is created but not visible until the layer is thawed.");
        r.WarnItem(3, "block 'X' has no attribute 'MARC'.");
        r.Warn("plain");
        r.Warn("plain");

        r.Settle([new ItemOutcome(0, true, "A", "LINE")]);

        Assert.Equal(3, r.Warnings.Count);
        Assert.Contains(r.Warnings, w => w.StartsWith("layer 'FROZEN'") && w.EndsWith("(items 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 +5 more)"));
        Assert.Contains(r.Warnings, w => w == "block 'X' has no attribute 'MARC'. (items 3)");
        Assert.Single(r.Warnings, w => w == "plain");
    }

    [Fact]
    public void Fail_and_warn_keep_the_envelope_contract()
    {
        var r = new EditResult().Warn("frozen layer").Fail(ToolError.ForHandle(ToolErrorCode.Erased, "2A", "gone"));

        Assert.False(r.Success);
        Assert.Equal(["frozen layer"], r.Warnings);
        Assert.Equal("2A", r.Errors[0].Handle);
    }

    [Fact]
    public void Refused_is_one_outcome_one_error_nothing_counted()
    {
        var r = EditResult.Refused(new ToolError(ToolErrorCode.LayerLocked, "Layer 'X' is locked; unlock it or choose another layer."), type: "HATCH");

        Assert.False(r.Success);
        Assert.Equal(0, r.CreatedCount);
        Assert.Single(r.Items);
        Assert.Equal(ToolErrorCode.LayerLocked, r.Items[0].Error!.Code);
        Assert.Contains("\"refused\":true", JsonSerializer.Serialize(r, Bridge));
    }

    [Fact]
    public void Edit_envelope_serialises_camel_case_with_the_bridge_options()
    {
        var r = new EditResult { Summary = new { requested = 2, created = 1 } };
        r.Created("A1");
        r.Settle([new ItemOutcome(0, true, "A1", "LWPOLYLINE"), new ItemOutcome(1, false, null, Error: ToolError.Argument("items[1]: type 'blob' is not one of line, …"))]);

        var json = JsonSerializer.Serialize(r, Bridge);

        Assert.Contains("\"success\":false", json);
        Assert.Contains("\"createdCount\":1", json);
        Assert.Contains("\"modifiedCount\":0", json);
        Assert.Contains("\"affectedHandles\":[\"A1\"]", json);
        Assert.Contains("\"items\":[{\"index\":0,\"ok\":true,\"handle\":\"A1\",\"type\":\"LWPOLYLINE\"}", json);
        Assert.Contains("\"errors\":[{\"code\":\"INVALID_ARGUMENT\"", json);
        Assert.DoesNotContain("\"changed\":null", json);
    }

    [Theory]
    [InlineData("refused")]
    [InlineData("written")]
    [InlineData("failed-midway")]
    public void A_full_batch_envelope_stays_under_the_result_cap_on_every_path(string path)
    {
        // The worst cases the review measured: every item refused on a locked layer with a frozen-layer warning each, every item
        // written with a long change list, every item failing midway with the applied keys named.
        var r = new EditResult { Summary = new { requested = BatchEditService.MaxBatchItems, modified = 0, refused = true, reason = "atomic: 200 item(s) invalid, nothing modified" } };
        var items = Enumerable.Range(0, BatchEditService.MaxBatchItems).Select(i =>
        {
            r.WarnItem(i, "layer 'FROZEN' is frozen: the entity is created but not visible until the layer is thawed.");
            return path switch
            {
                "refused" => new ItemOutcome(i, false, $"{0x2A00 + i:X}", "LWPOLYLINE", Error: ToolError.ForHandle(ToolErrorCode.LayerLocked, $"{0x2A00 + i:X}", $"items[{i}]: Entity {0x2A00 + i:X} is on locked layer 'A-ANNO-TTLB-TEXT-LOCKED'. (+3 more refusal(s) on this item)")),
                "written" => new ItemOutcome(i, true, $"{0x2A00 + i:X}", "LWPOLYLINE", ["layer", "color", "linetype", "lineweight", "visible", "geometry.points", "geometry.closed", "move", "rotate", "scaleBy"]),
                _ => new ItemOutcome(i, false, $"{0x2A00 + i:X}", "LWPOLYLINE", ["layer", "color", "linetype"], ToolError.ForHandle(ToolErrorCode.Internal, $"{0x2A00 + i:X}", $"{0x2A00 + i:X} could not be modified: eOnLockedLayer (applied before the failure: layer, color, linetype).")),
            };
        }).ToArray();
        foreach (var item in items.Where(i => i.Ok)) r.Modified(item.Handle!);

        r.Settle(items);
        var bytes = Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(r, Bridge));

        Assert.True(bytes < ResultBudgetBytes, $"{path}: {bytes} bytes");
    }
}
