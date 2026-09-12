using CommunityToolkit.Mvvm.ComponentModel;
using HPRebar.BeamRebar.Model;

namespace HPRebar.BeamRebar.Service;

/// <summary>
/// Provides reactive runtime language switching (English / Vietnamese) for all UI labels.
/// </summary>
public sealed partial class LocalizationService : ObservableObject
{
    [ObservableProperty]
    private UiStrings _strings = UiStringsCatalog.English;

    public bool IsVietnamese => ReferenceEquals(Strings, UiStringsCatalog.Vietnamese);

    public void Toggle() =>
        Strings = IsVietnamese ? UiStringsCatalog.English : UiStringsCatalog.Vietnamese;
}
