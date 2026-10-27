using System.Text.Json;
using System.Text.Json.Nodes;
using HPRebar.Core.KataExport.Settings;
using HPRebar.Core.KataRebar.Models;
using Xunit;

namespace HPRebar.Core.Tests.KataExport;

/// <summary>
/// Settings file ↔ settings page: the page shows what the file holds, accepting it writes the same file back, and a
/// saved choice outside its list is shown as the default with a notice. The fixtures are what the page's tests open.
/// </summary>
public sealed class KataSettingsFormMapperTests
{
    /// <summary>Every value off its default, both tables filled: a file the round trip cannot pass by accident.</summary>
    private static readonly KataSettingsFile Edited = new(
        KataSettings.Default with
        {
            ClosedStirrupHookAngle = 90,
            ClosedStirrupHookFactor = 6,
            CrossTieHookAngle = 135,
            CrossTieHookFactor = 8,
            RoundCutExtraMm = 20,
            SideBarAnchorageFactor = 12,
            LayerTieMinBarCount = 4,
            CrankMinDiameter = 20,
            CrankSlope = 10,
            CurtailedExtensionMm = 300,
            DenseZoneHeightFactor = 1.5,
            EndZoneFraction = 0.3,
            BottomExtraCutFraction = 0.2,
            MinimumLegFactor = 10,
            LayerClearGap = 25,
            RoundLegMm = 10,
            SideBarRequiredHeight = 600,
            ShowBarEndMarks = false,
            CreateKataDrawings = false,
            JointBeam = new KataJointRebarSettings { Stirrups = "3f10a60", StirrupCountSpec = "W+H-1", StirrupsThroughJoint = true, HangerEnabled = false, Hanger = "2f18", HangerTopLengthMm = 200, HangerAngleDegrees = 60 },
            JointColumn = new KataJointRebarSettings { Stirrups = "4f8a100", StirrupCountSpec = "H1", Hanger = "1f14", HangerTopLengthMm = 0 },
        },
        new KataBeamOptions
        {
            BottomLayerNoBend = true,
            TopNotAnchoredIntoColumnBelow = false,
            AlwaysBend = true,
            AlwaysBendFactor = 15,
            CutContinuousBarsAtSpanEnds = true,
            DifferentNumbersForIdenticalBars = true,
        },
        KataShopSettings.Default with
        {
            CouplerMinDiameter = 25,
            MaxBarLengthMm = 11000,
            MinLengthForCuttingMm = 6000,
            MinBarLengthFactor = 50,
            RoundLapMm = 10,
            TopLapAtMidspan = false,
            TopLapAtSupport = true,
            BottomLapAtSupport = false,
            BottomLapAtMidspan = true,
            TopLapZoneFraction = 0.3,
            TopLapZoneFromCentre = true,
            BottomLapZoneFraction = 0.1,
            BottomLapZoneFromCentre = true,
            PreferFewerLaps = true,
            CompressionLapFactor = 35,
            AllowTensionZoneLap = true,
            TensionLapFactor = 45,
            CrankToLapEdgeMm = 50,
            MinLapGapMm = 30,
            CuttingToleranceMm = 20,
            UseUnitMassTable = true,
            UnitMassTable = "10:0.617;16:1.578",
            UseLapTable = true,
            LapTable = "16:480,640,400,500;20:600,800,500,640",
        });

    [Fact]
    public void The_defaults_open_with_no_notice_and_pass_the_rules()
    {
        var snapshot = KataSettingsFormMapper.Snapshot(KataSettingsFile.Default);

        Assert.Null(snapshot.Notice);
        Assert.Empty(KataSettingsRules.Validate(snapshot.Form));
        Assert.Equal(KataSettingsFile.Default, KataSettingsFormMapper.ToFile(snapshot.Form));
    }

    [Fact]
    public void Accepting_what_was_opened_writes_the_same_file()
    {
        var snapshot = KataSettingsFormMapper.Snapshot(Edited);

        Assert.Equal(Edited, KataSettingsFormMapper.ToFile(snapshot.Form));
        Assert.Equal(2, snapshot.Form.Special.LapRows.Count);
        Assert.True(snapshot.Form.Detail.TopLapZoneFromCentre);
    }

    [Fact]
    public void Blank_table_rows_are_left_out_and_rows_are_written_in_diameter_order()
    {
        var form = KataSettingsFormMapper.Snapshot(KataSettingsFile.Default).Form;
        form = form with
        {
            Special = form.Special with
            {
                MassRows = new[] { new KataMassRowForm(16, 1.578), new KataMassRowForm(null, null), new KataMassRowForm(10, 0.617) },
            },
        };

        Assert.Equal("10:0.617;16:1.578", KataSettingsFormMapper.ToFile(form).Shop.UnitMassTable);
    }

    [Fact]
    public void An_emptied_count_spec_goes_back_to_none_and_notations_are_trimmed()
    {
        var form = KataSettingsFormMapper.Snapshot(KataSettingsFile.Default).Form;
        form = form with { Joints = form.Joints with { Beam = form.Joints.Beam with { StirrupCountSpec = "  ", Stirrups = " 5f8a50 " } } };

        var beam = KataSettingsFormMapper.ToFile(form).Drawing.JointBeam;

        Assert.Equal("None", beam.StirrupCountSpec);
        Assert.Equal("5f8a50", beam.Stirrups);
    }

    [Fact]
    public void A_saved_choice_outside_its_list_shows_the_default_and_says_so()
    {
        var file = KataSettingsFile.Default with
        {
            Drawing = KataSettings.Default with { CrankMinDiameter = 15, ClosedStirrupHookAngle = 150 },
            Shop = KataShopSettings.Default with { CouplerMinDiameter = 31 },
        };

        var snapshot = KataSettingsFormMapper.Snapshot(file);

        Assert.Equal(KataSettings.Default.CrankMinDiameter, snapshot.Form.Detail.CrankMinDiameter);
        Assert.Equal(KataSettings.Default.ClosedStirrupHookAngle, snapshot.Form.Detail.ClosedStirrupHookAngle);
        Assert.Equal(KataShopSettings.Default.CouplerMinDiameter, snapshot.Form.Detail.CouplerMinDiameter);
        Assert.NotNull(snapshot.Notice);
        Assert.Contains("Ø bẻ cổ chai 15", snapshot.Notice);
        Assert.Contains("góc móc đai kín 150°", snapshot.Notice);
        Assert.Contains("Ø coupler 31", snapshot.Notice);
    }

    [Fact]
    public void The_form_the_page_sends_back_reads_as_the_same_settings()
    {
        var sent = JsonSerializer.Serialize(KataSettingsFormMapper.Snapshot(Edited).Form, KataPageFixtures.Wire);

        var form = JsonSerializer.Deserialize<KataSettingsForm>(sent, KataPageFixtures.Wire)!;

        Assert.Equal(Edited, KataSettingsFormMapper.ToFile(form));
    }

    [Fact]
    public void An_emptied_input_arrives_as_null_and_is_refused_by_name()
    {
        var page = JsonNode.Parse(JsonSerializer.Serialize(KataSettingsFormMapper.Snapshot(KataSettingsFile.Default).Form, KataPageFixtures.Wire))!;
        page["detail"]!["maxBarLengthMm"] = null;
        page["special"]!["massRows"] = new JsonArray(new JsonObject { ["diameter"] = 10, ["kgPerM"] = null });

        var form = page.Deserialize<KataSettingsForm>(KataPageFixtures.Wire)!;

        Assert.Equal(new[] { "detail.maxBarLengthMm", "special.massRows[0].kgPerM" }, KataSettingsRules.Validate(form).Select(e => e.Field));
    }

    [Fact]
    public void The_page_fixtures_match_the_mapper()
    {
        var notice = KataSettingsFile.Default with { Drawing = KataSettings.Default with { CrankSlope = 5 } };
        var fixtures = new Dictionary<string, object>
        {
            ["kata-settings-default.json"] = KataSettingsFormMapper.Snapshot(KataSettingsFile.Default),
            ["kata-settings-edited.json"] = KataSettingsFormMapper.Snapshot(Edited),
            ["kata-settings-notice.json"] = KataSettingsFormMapper.Snapshot(notice),
        };

        KataPageFixtures.AssertUpToDate("kataSettings", fixtures, "KataSettingsFormMapper");
    }
}
