using CommunityToolkit.Mvvm.ComponentModel;
using HPRebar.ColumnRebar.ViewModel;
using HPRebar.ColumnRebar.Service;

namespace HPRebar.ColumnRebar.ViewModel.Tabs;

/// <summary>Names and prefixes written onto the elements the tool creates.</summary>
public sealed partial class SettingTabViewModel : ColumnRebarTabViewModel
{
    public SettingTabViewModel(ColumnRebarSession session, LocalizationService localization)
        : base(session, localization)
    {
    }

    public override string Title => Localization.Strings.TabSetting;

    public override string IconKey => "TabIcon.Setting";

    /// <summary>
    ///     The tool only models real rebar today. The bound checkbox stays visible but disabled so the
    ///     setting is discoverable when the detail-item mode is added.
    /// </summary>
    public bool CanChooseDrawingMode => false;
}
