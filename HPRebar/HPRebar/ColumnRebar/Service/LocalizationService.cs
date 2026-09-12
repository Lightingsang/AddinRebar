using CommunityToolkit.Mvvm.ComponentModel;
using HPRebar.ColumnRebar.Model;

namespace HPRebar.ColumnRebar.Service;

/// <summary>
///     Holds the language the window is currently showing. Views bind through
///     <c>{Binding Loc.Strings.Xxx}</c>, so swapping the whole record refreshes every label at once
///     without rebuilding the window.
/// </summary>
public sealed partial class LocalizationService : ObservableObject
{
    [ObservableProperty]
    private UiStrings _strings = UiStringsCatalog.English;

    public bool IsVietnamese => ReferenceEquals(Strings, UiStringsCatalog.Vietnamese);

    /// <summary>Switches between the two languages.</summary>
    public void Toggle() =>
        Strings = IsVietnamese ? UiStringsCatalog.English : UiStringsCatalog.Vietnamese;
}
