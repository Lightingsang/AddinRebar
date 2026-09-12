using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPRebar.ColumnRebar.ViewModel;
using HPRebar.ColumnRebar.Service;

namespace HPRebar.ColumnRebar.ViewModel.Tabs;

/// <summary>
///     What every tab has in common: the shared session, the current language, and the column picker that
///     each tab shows at the top.
/// </summary>
public abstract partial class ColumnRebarTabViewModel : ObservableObject
{
    protected ColumnRebarTabViewModel(ColumnRebarSession session, LocalizationService localization)
    {
        Session = session;
        Localization = localization;
    }

    public ColumnRebarSession Session { get; }

    public LocalizationService Localization { get; }

    /// <summary>Label shown in the navigation list. Follows the current language.</summary>
    public abstract string Title { get; }

    /// <summary>Template resource key for the vector icon shown in the navigation list.</summary>
    public abstract string IconKey { get; }

    /// <summary>Refreshes the navigation label after the language has been swapped.</summary>
    public void NotifyTitleChanged() => OnPropertyChanged(nameof(Title));

    /// <summary>Copies this column's settings onto the rest of the stack.</summary>
    [RelayCommand]
    protected void ApplyToAllColumns() => Session.ApplySelectedToAll();
}
