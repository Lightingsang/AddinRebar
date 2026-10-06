using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;

namespace HPRebar.KataExport.ViewModel.Tabs;

/// <summary>Tab "Thép mặc định": the joint stirrups and hanger bars round a crossing beam and round a stub column.</summary>
public sealed class KataJointDefaultsTabViewModel
{
    public KataJointColumnViewModel Beam { get; } = new("Dầm giao");

    public KataJointColumnViewModel Column { get; } = new("Cột cấy");

    public void Import(KataSettings drawing)
    {
        Beam.Import(drawing.JointBeam);
        Column.Import(drawing.JointColumn);
    }

    public string? Validate() => Beam.Validate() ?? Column.Validate();

    public KataSettings ExportDrawing(KataSettings start) => start with { JointBeam = Beam.Export(), JointColumn = Column.Export() };
}

/// <summary>One column of the tab: the settings of one kind of load.</summary>
public sealed partial class KataJointColumnViewModel : ObservableObject
{
    [ObservableProperty] private string _stirrups = "";
    [ObservableProperty] private string _stirrupCountSpec = "";
    [ObservableProperty] private bool _stirrupsThroughJoint;
    [ObservableProperty] private bool _hangerEnabled;
    [ObservableProperty] private string _hanger = "";
    [ObservableProperty] private double _hangerTopLengthMm;
    [ObservableProperty] private int _hangerAngleDegrees;

    public KataJointColumnViewModel(string title) => Title = title;

    public string Title { get; }

    /// <summary>Kata's choices for "Spec số lượng đai"; the box takes any other expression too.</summary>
    public IReadOnlyList<string> CountSpecOptions { get; } = new[] { "None", "H1", "H", "W", "W+H-1" };

    public IReadOnlyList<int> AngleOptions { get; } = new[] { 45, 60 };

    public void Import(KataJointRebarSettings s)
    {
        Stirrups = s.Stirrups;
        StirrupCountSpec = s.StirrupCountSpec;
        StirrupsThroughJoint = s.StirrupsThroughJoint;
        HangerEnabled = s.HangerEnabled;
        Hanger = s.Hanger;
        HangerTopLengthMm = s.HangerTopLengthMm;
        HangerAngleDegrees = s.HangerAngleDegrees;
    }

    public string? Validate()
    {
        if (!KataJointNotation.TryParseStirrups(Stirrups, out _, out _, out _))
            return $"{Title}: đai gia cường '{Stirrups}' phải dạng số lượng f Ø a bước, ví dụ 5f8a50.";
        if (HangerEnabled && !KataJointNotation.TryParseBars(Hanger, out _, out _))
            return $"{Title}: cốt đai vai bò '{Hanger}' phải dạng số lượng f Ø, ví dụ 2f16.";
        if (double.IsNaN(HangerTopLengthMm) || double.IsInfinity(HangerTopLengthMm) || HangerTopLengthMm < 0.0)
            return $"{Title}: bẻ ngang vai bò phải là số không âm.";
        return HangerAngleDegrees is 45 or 60 ? null : $"{Title}: góc bẻ vai bò là 45 hoặc 60.";
    }

    public KataJointRebarSettings Export() => new()
    {
        Stirrups = Stirrups.Trim(),
        StirrupCountSpec = string.IsNullOrWhiteSpace(StirrupCountSpec) ? KataJointRebarSettings.Default.StirrupCountSpec : StirrupCountSpec.Trim(),
        StirrupsThroughJoint = StirrupsThroughJoint,
        HangerEnabled = HangerEnabled,
        Hanger = Hanger.Trim(),
        HangerTopLengthMm = HangerTopLengthMm,
        HangerAngleDegrees = HangerAngleDegrees
    };
}
