using System.Linq;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>
/// The settings file of the three-tab dialog: the drawing settings, the joint defaults of tab "Thép mặc định", the beam
/// options and shop settings kept for later, and what each of them changes in the bars.
/// </summary>
public sealed class KataSettingsFileTests
{
    private static KataSettingsFile Edited() => new(
        KataSettings.Default with
        {
            CrankSlope = 8.0,
            JointBeam = KataJointRebarSettings.Default with { Stirrups = "4f10a100", Hanger = "2f14", HangerAngleDegrees = 60 },
            JointColumn = KataJointRebarSettings.Default with { HangerEnabled = false, StirrupCountSpec = "W+H-1", StirrupsThroughJoint = true }
        },
        KataBeamOptions.Default with { AlwaysBend = true, AlwaysBendFactor = 12.0, TopNotAnchoredIntoColumnBelow = false },
        KataShopSettings.Default with { MaxBarLengthMm = 11000.0, TopLapZoneFromCentre = true, UnitMassTable = "10:0.617;12:0.888", LapTable = "16:480,640,400,560" });

    [Fact]
    public void Every_group_survives_a_round_trip()
    {
        var file = Edited();

        Assert.Equal(file, KataSettingsJson.ReadFile(KataSettingsJson.WriteFile(file)));
    }

    [Fact]
    public void A_file_of_the_previous_version_keeps_its_values_and_gains_the_new_defaults()
    {
        // Version 2 with a leg the user chose (15 d was the version-1 default, which an unversioned file migrates away).
        var file = KataSettingsJson.ReadFile("{ \"SettingsVersion\": 2, \"MinimumLegFactor\": 15, \"SideBarAnchorageFactor\": 12 }");

        Assert.Equal(15.0, file.Drawing.MinimumLegFactor);
        Assert.Equal(12.0, file.Drawing.SideBarAnchorageFactor);
        Assert.Equal(3, file.Drawing.SettingsVersion);
        Assert.Equal(KataJointRebarSettings.Default, file.Drawing.JointBeam);
        Assert.Equal(KataShopSettings.Default, file.Shop);
    }

    [Fact]
    public void Wrong_notations_and_table_rows_fall_back_or_drop()
    {
        var file = KataSettingsJson.ReadFile(
            "{ \"JointBeam.Stirrups\": \"5f8\", \"JointBeam.Hanger\": \"two\", \"JointBeam.HangerAngleDegrees\": 30,"
            + " \"Shop.UnitMassTable\": \"10:0.617;x:1;12:-2;10:9\", \"Shop.LapTable\": \"16:1,2,3\", \"Shop.MaxBarLengthMm\": -5 }");

        Assert.Equal(KataJointRebarSettings.Default, file.Drawing.JointBeam);
        Assert.Equal("10:0.617", file.Shop.UnitMassTable);
        Assert.Equal("", file.Shop.LapTable);
        Assert.Equal(11700.0, file.Shop.MaxBarLengthMm);
    }

    [Fact]
    public void The_beam_options_and_shop_settings_never_reach_the_rules()
    {
        var spec = KataDamSheetParser.Parse(KataB01DrawingTests.Sheet());
        var plain = KataSettingsFile.Default;
        var other = plain with { Pending = Edited().Pending, Shop = Edited().Shop };

        var a = KataSettingsJson.ReadFile(KataSettingsJson.WriteFile(plain)).Drawing;
        var b = KataSettingsJson.ReadFile(KataSettingsJson.WriteFile(other)).Drawing;

        Assert.Equal(a, b);
        Assert.Equal(KataDetailingRuleBuilder.Build(spec, a).JointBeam, KataDetailingRuleBuilder.Build(spec, b).JointBeam);
    }

    private static KataRebarLayoutResult B01WithLoads(KataSettings settings)
    {
        var spec = KataDamSheetParser.Parse(KataB01DrawingTests.Sheet());
        var spans = spec.Spans.ToList();
        spans[0] = spans[0] with { Loads = new[] { new KataSpanLoad(4900.0, 400.0, 900.0, false) } };
        spans[1] = spans[1] with { Loads = new[] { new KataSpanLoad(3600.0, 400.0, 0.0, true) } };
        return KataRebarCalculator.Calculate(spec with { Spans = spans }, settings);
    }

    [Fact]
    public void The_default_joint_stirrups_are_5_of_8_mm_at_50_numbered_apart_from_the_hoops()
    {
        var layout = B01WithLoads(KataSettings.Default);
        var joint = layout.StirrupZones.Where(z => KataJointStirrups.IsJointZone(z.ZoneName)).ToList();
        var hoops = layout.StirrupZones.Where(z => !KataJointStirrups.IsJointZone(z.ZoneName)).ToList();

        Assert.All(joint, z => { Assert.Equal(8.0, z.Diameter); Assert.Equal(5, z.Count); Assert.Equal(50.0, z.NominalSpacing); });
        Assert.DoesNotContain(joint.Select(z => z.BarNumber), n => hoops.Any(h => h.BarNumber == n));
        Assert.All(layout.IndividualStirrups.Where(s => joint.Any(z => z.Stations.Contains(s.Polyline.Points[0].X))), s => Assert.Equal(8.0, s.Diameter));
    }

    [Fact]
    public void Each_load_kind_takes_its_own_joint_stirrups_and_hanger_bars()
    {
        var layout = B01WithLoads(Edited().Drawing);

        // Crossing beam in span 1: 4f10a100 each side, 2Ø14 hangers at 60°; stub column in span 2: 5f8a50, no hangers.
        var beam = layout.StirrupZones.Where(z => z.SpanIndex == 0 && KataJointStirrups.IsJointZone(z.ZoneName)).ToList();
        Assert.Equal(2, beam.Count);
        Assert.All(beam, z => { Assert.Equal(4, z.Count); Assert.Equal(10.0, z.Diameter); Assert.Equal(100.0, z.NominalSpacing); });
        var column = layout.StirrupZones.Where(z => z.SpanIndex == 1 && KataJointStirrups.IsJointZone(z.ZoneName)).ToList();
        Assert.All(column, z => { Assert.Equal(5, z.Count); Assert.Equal(8.0, z.Diameter); });

        Assert.All(layout.HangerBars, b => { Assert.Equal(0, b.HostSpanIndex); Assert.Equal(14.0, b.Diameter); });
        var p = layout.HangerBars[0].Polyline.Points;
        double run = p[2].X - p[1].X, rise = p[1].Z - p[2].Z;
        Assert.InRange(rise / run, System.Math.Tan(59.0 * System.Math.PI / 180.0), System.Math.Tan(61.0 * System.Math.PI / 180.0));
    }

    [Fact]
    public void The_joint_stirrup_tag_carries_its_own_diameter()
    {
        var spec = KataDamSheetParser.Parse(KataB01DrawingTests.Sheet());
        var layout = B01WithLoads(KataSettings.Default);

        var tags = KataBarTagBuilder.Build(spec, layout, 10.0).Where(t => t.Kind == KataTagKind.Stirrups).Select(t => t.Text).ToList();

        Assert.Contains("Ø8a50", tags);
        Assert.Contains(tags, t => t.StartsWith("Ø10a"));
    }

    [Fact]
    public void A_beam_and_a_column_close_together_keep_each_side_one_evenly_spaced_zone()
    {
        // Beam at a50 and column at a100 whose joint stirrups meet: one group, the beam side a50, the column side a100.
        var spec = KataDamSheetParser.Parse(KataB01DrawingTests.Sheet());
        var spans = spec.Spans.ToList();
        spans[0] = spans[0] with { Loads = new[] { new KataSpanLoad(4000.0, 400.0, 900.0, false), new KataSpanLoad(4900.0, 400.0, 0.0, true) } };
        var settings = KataSettings.Default with { JointColumn = KataJointRebarSettings.Default with { Stirrups = "5f8a100" } };

        var joint = KataRebarCalculator.Calculate(spec with { Spans = spans }, settings).StirrupZones
            .Where(z => z.SpanIndex == 0 && KataJointStirrups.IsJointZone(z.ZoneName)).OrderBy(z => z.StartStationX).ToList();

        // Between the loads the beam's a50 and the column's a100 stirrups meet: each run stays one zone, never a zone per stirrup.
        Assert.All(joint, z => Assert.True(z.Count >= 2, string.Join(" | ", joint.Select(j => $"{j.ZoneName} {j.Count}@{j.NominalSpacing}"))));
        Assert.Equal(50.0, joint.First().NominalSpacing);
        Assert.Equal(100.0, joint.Last().NominalSpacing);
        Assert.All(joint, z => Assert.Equal(z.Stations.Count > 1 ? z.Stations[1] - z.Stations[0] : z.NominalSpacing, z.NominalSpacing, 1));
    }

    [Theory]
    [InlineData("5f8a50", true)]
    [InlineData("0f8a50", true)]
    [InlineData("5f8a5", false)]
    [InlineData("5f2a50", false)]
    [InlineData("500f8a50", false)]
    [InlineData("5f8", false)]
    public void A_joint_stirrup_notation_reads_only_when_it_makes_sense(string text, bool reads)
    {
        Assert.Equal(reads, KataJointNotation.TryParseStirrups(text, out _, out _, out _));
    }

    [Fact]
    public void A_bad_table_row_never_takes_the_place_of_a_good_one()
    {
        Assert.Equal("10:0.617", KataShopTables.NormalizeMass("10:x;10:0.617"));
    }

    [Fact]
    public void The_crank_slope_comes_from_the_settings()
    {
        // (100 − 18) / 500 = 0.164: cranked at 1:6 over 600, cut at 1:8, cranked at 1:5 over 500.
        int Runs(double slope) => KataRebarCalculator.Calculate(KataDamSheetParser.Parse(KataDy14DrawingTests.Sheet(500.0)),
            KataSettings.Default with { CrankSlope = slope }).MainBottomBars.Count(b => b.TransverseY < 0);

        Assert.Equal(2, Runs(6.0));
        Assert.Equal(3, Runs(8.0));

        var bar = KataRebarCalculator.Calculate(KataDamSheetParser.Parse(KataDy14DrawingTests.Sheet(500.0)), KataSettings.Default with { CrankSlope = 5.0 })
            .MainBottomBars.Where(b => b.TransverseY < 0).OrderBy(b => b.Polyline.Points.Min(q => q.X)).First().Polyline.Points;
        var crank = bar.Zip(bar.Skip(1), (a, b) => (a, b)).Single(s => System.Math.Abs(s.a.Z - s.b.Z) > 1.0 && s.a.X > 1000.0 && s.b.X < 13000.0);
        Assert.Equal(500.0, crank.b.X - crank.a.X, 1);
    }
}
