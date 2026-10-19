using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HPRebar.JoinElements.Model;

/// <summary>
/// Represents a category pair option for Auto Join and Priority cutting order.
/// </summary>
public sealed partial class JoinCategoryPairOption : ObservableObject
{
    public BuiltInCategory PrimaryCategory { get; }
    public BuiltInCategory SecondaryCategory { get; }
    public string PrimaryName { get; }
    public string SecondaryName { get; }

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _primaryCutsSecondary;

    public string DisplayPair => $"{PrimaryName} & {SecondaryName}";

    public string CuttingOrderText => PrimaryCutsSecondary
        ? $"{PrimaryName} cắt {SecondaryName}"
        : $"{SecondaryName} cắt {PrimaryName}";

    public JoinCategoryPairOption(
        BuiltInCategory primaryCategory,
        BuiltInCategory secondaryCategory,
        string primaryName,
        string secondaryName,
        bool isSelected = true,
        bool primaryCutsSecondary = true)
    {
        PrimaryCategory = primaryCategory;
        SecondaryCategory = secondaryCategory;
        PrimaryName = primaryName;
        SecondaryName = secondaryName;
        _isSelected = isSelected;
        _primaryCutsSecondary = primaryCutsSecondary;
    }

    partial void OnPrimaryCutsSecondaryChanged(bool value)
    {
        OnPropertyChanged(nameof(CuttingOrderText));
    }
}
