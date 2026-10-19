using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HPRebar.RebarPartition.Model;

/// <summary>
/// Selectable host category option for the UI checkboxes.
/// </summary>
public sealed partial class HostCategoryOption : ObservableObject
{
    public BuiltInCategory Category { get; }
    public string DisplayName { get; }

    [ObservableProperty]
    private bool _isSelected;

    public HostCategoryOption(BuiltInCategory category, string displayName, bool isSelected = true)
    {
        Category = category;
        DisplayName = displayName;
        _isSelected = isSelected;
    }
}
