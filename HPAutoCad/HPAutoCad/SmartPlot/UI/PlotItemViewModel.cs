using CommunityToolkit.Mvvm.ComponentModel;
using HPAutoCad.Core.SmartPlot.Models;

namespace HPAutoCad.SmartPlot.UI;

/// <summary>
/// Observable ViewModel representing a single plot frame item in the DataGrid.
/// </summary>
public sealed partial class PlotItemViewModel : ObservableObject
{
    public PlotItem Model { get; }

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private int _order;

    [ObservableProperty]
    private string _status = "Ready";

    public string Id => Model.Id;
    public string LayoutName => Model.LayoutName;
    public string DisplayName => string.IsNullOrWhiteSpace(Model.DisplayName) ? Model.Id : Model.DisplayName;
    public string SheetNumber => Model.SheetNumber ?? string.Empty;
    public string SheetTitle => Model.SheetTitle ?? string.Empty;
    public double Rotation => Model.Rotation;
    public string? SourceHandle => Model.SourceHandle;
    public string Dimensions => $"{Model.Width:F0} × {Model.Height:F0} mm";
    public string OrientationText => Model.IsLandscape ? "Landscape" : "Portrait";

    public PlotItemViewModel(PlotItem model)
    {
        ArgumentNullException.ThrowIfNull(model);
        Model = model;
        _isSelected = model.IsSelected;
        _order = model.Order;
    }

    /// <summary>
    /// Produces a copy of the underlying PlotItem with the current Order and IsSelected states.
    /// </summary>
    public PlotItem ToModel()
    {
        return Model with
        {
            Order = Order,
            IsSelected = IsSelected
        };
    }
}
