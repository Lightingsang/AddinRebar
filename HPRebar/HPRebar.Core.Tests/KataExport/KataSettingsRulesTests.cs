using HPRebar.Core.KataExport.Settings;
using HPRebar.Core.KataRebar.Models;
using Xunit;

namespace HPRebar.Core.Tests.KataExport;

/// <summary>
/// The values the settings page refuses, field by field: the ranges Kata's settings dialog took (each edge on both sides),
/// the lists of its selects, the joint notations and the two tables.
/// </summary>
public sealed class KataSettingsRulesTests
{
    private static readonly KataSettingsForm Valid = KataSettingsFormMapper.Snapshot(KataSettingsFile.Default).Form;

    private static KataSettingsForm Detail(Func<KataDetailForm, KataDetailForm> change) => Valid with { Detail = change(Valid.Detail) };

    private static KataSettingsForm Special(Func<KataSpecialForm, KataSpecialForm> change) => Valid with { Special = change(Valid.Special) };

    private static KataSettingsForm Beam(Func<KataJointForm, KataJointForm> change) =>
        Valid with { Joints = Valid.Joints with { Beam = change(Valid.Joints.Beam) } };

    private static KataSettingsForm Column(Func<KataJointForm, KataJointForm> change) =>
        Valid with { Joints = Valid.Joints with { Column = change(Valid.Joints.Column) } };

    public static TheoryData<string, string?> Cases()
    {
        var data = new TheoryData<string, string?>();
        foreach (var (name, _, field) in Table) data.Add(name, field);
        return data;
    }

    // name, form, the field refused (null = accepted).
    private static readonly (string Name, KataSettingsForm Form, string? Field)[] Table =
    {
        ("defaults", Valid, null),
        ("closed hook angle outside the list", Detail(d => d with { ClosedStirrupHookAngle = 150 }), "detail.closedStirrupHookAngle"),
        ("closed hook factor 0", Detail(d => d with { ClosedStirrupHookFactor = 0 }), "detail.closedStirrupHookFactor"),
        ("cross tie hook angle empty", Detail(d => d with { CrossTieHookAngle = null }), "detail.crossTieHookAngle"),
        ("cross tie factor negative", Detail(d => d with { CrossTieHookFactor = -1 }), "detail.crossTieHookFactor"),
        ("round cut extra 0 is off", Detail(d => d with { RoundCutExtraMm = 0 }), null),
        ("round cut extra negative", Detail(d => d with { RoundCutExtraMm = -1 }), "detail.roundCutExtraMm"),
        ("side bar anchorage 0", Detail(d => d with { SideBarAnchorageFactor = 0 }), "detail.sideBarAnchorageFactor"),
        ("layer tie 2 bars", Detail(d => d with { LayerTieMinBarCount = 2 }), null),
        ("layer tie 1 bar", Detail(d => d with { LayerTieMinBarCount = 1 }), "detail.layerTieMinBarCount"),
        ("layer tie empty", Detail(d => d with { LayerTieMinBarCount = null }), "detail.layerTieMinBarCount"),
        ("crank diameter outside the list", Detail(d => d with { CrankMinDiameter = 15 }), "detail.crankMinDiameter"),
        ("crank slope outside the list", Detail(d => d with { CrankSlope = 5 }), "detail.crankSlope"),
        ("curtailed extension negative", Detail(d => d with { CurtailedExtensionMm = -1 }), "detail.curtailedExtensionMm"),
        ("dense zone negative", Detail(d => d with { DenseZoneHeightFactor = -0.1 }), "detail.denseZoneHeightFactor"),
        ("end zone 0.5", Detail(d => d with { EndZoneFraction = 0.5 }), null),
        ("end zone over 0.5", Detail(d => d with { EndZoneFraction = 0.51 }), "detail.endZoneFraction"),
        ("end zone negative", Detail(d => d with { EndZoneFraction = -0.01 }), "detail.endZoneFraction"),
        ("bottom extra cut 0", Detail(d => d with { BottomExtraCutFraction = 0 }), null),
        ("bottom extra cut 0.5", Detail(d => d with { BottomExtraCutFraction = 0.5 }), "detail.bottomExtraCutFraction"),
        ("minimum leg negative", Detail(d => d with { MinimumLegFactor = -1 }), "detail.minimumLegFactor"),
        ("layer gap negative", Detail(d => d with { LayerClearGap = -1 }), "detail.layerClearGap"),
        ("round leg negative", Detail(d => d with { RoundLegMm = -1 }), "detail.roundLegMm"),
        ("side bar height negative", Detail(d => d with { SideBarRequiredHeight = -1 }), "detail.sideBarRequiredHeight"),
        ("always bend factor negative", Detail(d => d with { AlwaysBendFactor = -1 }), "detail.alwaysBendFactor"),
        ("coupler diameter outside the list", Detail(d => d with { CouplerMinDiameter = 31 }), "detail.couplerMinDiameter"),
        ("max bar length 0", Detail(d => d with { MaxBarLengthMm = 0 }), "detail.maxBarLengthMm"),
        ("max bar length NaN", Detail(d => d with { MaxBarLengthMm = double.NaN }), "detail.maxBarLengthMm"),
        ("min length for cutting negative", Detail(d => d with { MinLengthForCuttingMm = -1 }), "detail.minLengthForCuttingMm"),
        ("min bar length factor infinite", Detail(d => d with { MinBarLengthFactor = double.PositiveInfinity }), "detail.minBarLengthFactor"),
        ("round lap empty", Detail(d => d with { RoundLapMm = null }), "detail.roundLapMm"),
        ("top lap zone 0.5", Detail(d => d with { TopLapZoneFraction = 0.5 }), null),
        ("top lap zone over 0.5", Detail(d => d with { TopLapZoneFraction = 0.6 }), "detail.topLapZoneFraction"),
        ("bottom lap zone negative", Detail(d => d with { BottomLapZoneFraction = -0.1 }), "detail.bottomLapZoneFraction"),

        ("compression lap negative", Special(s => s with { CompressionLapFactor = -1 }), "special.compressionLapFactor"),
        ("tension lap empty", Special(s => s with { TensionLapFactor = null }), "special.tensionLapFactor"),
        ("crank to lap edge negative", Special(s => s with { CrankToLapEdgeMm = -1 }), "special.crankToLapEdgeMm"),
        ("min lap gap negative", Special(s => s with { MinLapGapMm = -1 }), "special.minLapGapMm"),
        ("cutting tolerance negative", Special(s => s with { CuttingToleranceMm = -1 }), "special.cuttingToleranceMm"),
        ("mass rows, blank rows ignored", Special(s => s with { MassRows = new[] { new KataMassRowForm(10, 0.617), new KataMassRowForm(null, null), new KataMassRowForm(0, 0) } }), null),
        ("mass row without diameter", Special(s => s with { MassRows = new[] { new KataMassRowForm(null, 0.617) } }), "special.massRows[0].diameter"),
        ("mass row negative kg", Special(s => s with { MassRows = new[] { new KataMassRowForm(10, -1) } }), "special.massRows[0].kgPerM"),
        ("mass rows same diameter", Special(s => s with { MassRows = new[] { new KataMassRowForm(10, 0.6), new KataMassRowForm(10, 0.7) } }), "special.massRows[1].diameter"),
        ("lap row negative diameter", Special(s => s with { LapRows = new[] { new KataLapRowForm(-16, 480, 640, 400, 500) } }), "special.lapRows[0].diameter"),
        ("lap row empty tension", Special(s => s with { LapRows = new[] { new KataLapRowForm(16, 480, null, 400, 500) } }), "special.lapRows[0].lapTension"),
        ("lap rows same diameter", Special(s => s with { LapRows = new[] { new KataLapRowForm(16, 1, 1, 1, 1), new KataLapRowForm(16, 2, 2, 2, 2) } }), "special.lapRows[1].diameter"),

        ("beam stirrups wrong", Beam(j => j with { Stirrups = "5f8" }), "joints.beam.stirrups"),
        ("beam hanger wrong while on", Beam(j => j with { HangerEnabled = true, Hanger = "2x16" }), "joints.beam.hanger"),
        ("beam hanger wrong while off", Beam(j => j with { HangerEnabled = false, Hanger = "2x16" }), null),
        ("column hanger length negative", Column(j => j with { HangerTopLengthMm = -1 }), "joints.column.hangerTopLengthMm"),
        ("column hanger angle 50", Column(j => j with { HangerAngleDegrees = 50 }), "joints.column.hangerAngleDegrees"),
        ("column count spec is free text", Column(j => j with { StirrupCountSpec = "2*H+1" }), null),
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Refuses_exactly_the_field_out_of_range(string name, string? field)
    {
        var form = Array.Find(Table, c => c.Name == name).Form;

        var errors = KataSettingsRules.Validate(form);

        if (field is null) Assert.Empty(errors);
        else Assert.Equal(new[] { field }, errors.Select(e => e.Field));
    }

    [Fact]
    public void Every_wrong_field_is_reported_not_only_the_first()
    {
        var form = Detail(d => d with { EndZoneFraction = 2, MaxBarLengthMm = 0 }) with
        {
            Joints = Valid.Joints with { Column = Valid.Joints.Column with { Stirrups = "" } },
        };

        var fields = KataSettingsRules.Validate(form).Select(e => e.Field);

        Assert.Equal(new[] { "detail.endZoneFraction", "detail.maxBarLengthMm", "joints.column.stirrups" }, fields);
    }

    [Fact]
    public void A_refused_form_is_never_written()
    {
        var form = Detail(d => d with { EndZoneFraction = 2 });

        var ex = Assert.Throws<ArgumentException>(() => KataSettingsFormMapper.ToFile(form));
        Assert.StartsWith("detail.endZoneFraction:", ex.Message);
    }
}
