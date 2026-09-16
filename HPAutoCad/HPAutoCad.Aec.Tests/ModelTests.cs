using System.Text.Json;
using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Model;
using HPRebar.McpBridge.Core.Scripting;
using Xunit;

namespace HPAutoCad.Aec.Tests;

public sealed class ModelTests
{
    private static ScriptArgs Args(string json) => new(JsonSerializer.Deserialize<JsonElement>(json));

    [Theory]
    [InlineData("S-BEAM", "S-BEAM", true)]
    [InlineData("s-beam", "S-BEAM", true)]
    [InlineData("S-*", "S-COL", true)]
    [InlineData("S-*", "A-WALL", false)]
    [InlineData("?-WALL", "A-WALL", true)]
    [InlineData("A-WALL-*", "A-WALL", false)]
    public void Wildcards_match_like_autocad(string pattern, string value, bool expected)
    {
        Assert.Equal(expected, EntityFilter.WildcardMatch(pattern, value));
    }

    [Fact]
    public void Filter_reads_lists_singles_and_comma_lists_alike()
    {
        var filter = EntityFilter.From(Args("""{"types":["line","ARC"],"layer":"S-BEAM,S-COL","handles":"1a2b","visibleOnly":true,"space":"Layout1","bogus":1}"""), out var unknown);

        Assert.Equal(["LINE", "ARC"], filter.Types);
        Assert.Equal(["S-BEAM", "S-COL"], filter.Layers);
        Assert.Equal(["1A2B"], filter.Handles);
        Assert.True(filter.VisibleOnly);
        Assert.Equal("Layout1", filter.Space);
        Assert.Equal(["bogus"], unknown);
        Assert.False(filter.IsEmpty);
        Assert.True(EntityFilter.From(null, out _).IsEmpty);
        Assert.True(EntityFilter.Matches(filter.Layers, "s-col"));
        Assert.False(EntityFilter.Matches(filter.Layers, "A-WALL"));
        Assert.True(EntityFilter.Matches([], "anything"), "no patterns = no restriction");
    }

    [Fact]
    public void Tolerance_overrides_only_the_given_members_and_reports_unknown_keys()
    {
        var tol = GeometryTolerance.From(Args("""{"endpointConnection":25,"parallelAngle":0,"typo":3}"""), out var unknown);

        Assert.Equal(25, tol.EndpointConnection);
        Assert.Equal(GeometryTolerance.Default.ParallelAngle, tol.ParallelAngle); // 0 is not a usable tolerance: kept default and reported
        Assert.Equal(GeometryTolerance.Default.PointEquality, tol.PointEquality);
        Assert.Equal(["parallelAngle", "typo"], unknown);
        Assert.Same(GeometryTolerance.Default, GeometryTolerance.From(null, out _));
        Assert.Equal(0.25, GeometryTolerance.Default.ChordError);
    }

    [Fact]
    public void Analysis_result_serialises_camel_case_with_the_agreed_keys()
    {
        var result = new AnalysisResult<string> { Items = ["a"], Count = 1, Summary = new { total = 1 } };
        result.Warn("w").Fail(ToolError.ForHandle(ToolErrorCode.InvalidHandle, "ZZ", "Entity handle ZZ was not found."));

        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        Assert.Contains("\"success\":true", json);
        Assert.Contains("\"items\":[\"a\"]", json);
        Assert.Contains("\"count\":1", json);
        Assert.Contains("\"warnings\":[\"w\"]", json);
        Assert.Contains("\"errors\":[{\"code\":\"INVALID_HANDLE\",\"message\":\"Entity handle ZZ was not found.\",\"handle\":\"ZZ\"}]", json);
    }

    [Fact]
    public void Points_and_boxes_serialise_as_xyz_objects()
    {
        var json = JsonSerializer.Serialize(Box.Of(new Pt(1, 2, 3), new Pt(4, 5, 6)), new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        Assert.Equal("{\"min\":{\"x\":1,\"y\":2,\"z\":3},\"max\":{\"x\":4,\"y\":5,\"z\":6}}", json);
    }

    [Fact]
    public void Entity_record_exposes_geometry_only_on_request()
    {
        var record = new AecEntityRecord { Handle = "1F", Type = "LWPOLYLINE", Layer = "S-COL", Shape = PlanShape.Rectangle(Box.Of(new Pt(0, 0), new Pt(400, 600))) };

        Assert.Null(record.Geometry);
        var geometry = record.ToGeometry()!;
        Assert.True(geometry.Closed);
        Assert.Equal(240_000, geometry.AreaMm2!.Value, 6);
        Assert.Equal(2000, geometry.LengthMm, 6);
        Assert.True(geometry.Centroid!.Value.AlmostEqualsXY(new Pt(200, 300), 1e-9));

        var json = JsonSerializer.Serialize(record, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });
        Assert.DoesNotContain("\"shape\"", json);
        Assert.DoesNotContain("segments", json);
    }
}
