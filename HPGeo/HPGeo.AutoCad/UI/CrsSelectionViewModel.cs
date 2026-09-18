using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using HPGeo.Core.Catalog;
using HPGeo.Core.Projection;
using HPGeo.Core.Units;

namespace HPGeo.AutoCad.UI;

/// <summary>
/// The coordinate-system block both dialogs share: catalogue (34 current / 63 legacy provinces), province,
/// central meridian with the former-province note, a manual meridian box, k0/FE/FN and the drawing unit.
/// First-run defaults are the reference tool's (TP. Hồ Chí Minh, 105°45′). Raises <see cref="Changed"/> after
/// every edit so the owning view model can recompute.
/// </summary>
public sealed partial class CrsSelectionViewModel : ObservableObject
{
    public const string DefaultProvince = "TP. Hồ Chí Minh";
    public const double DefaultMeridian = 105.75;

    private readonly ProvinceCatalog _catalog;
    private bool _suspend;

    public CrsSelectionViewModel(DrawingUnit drawingUnit, ProvinceCatalog? catalog = null)
    {
        _catalog = catalog ?? ProvinceCatalog.Default;
        foreach (var unit in new[] { DrawingUnit.Meters, DrawingUnit.Millimeters, DrawingUnit.Centimeters, DrawingUnit.Decimeters, DrawingUnit.Kilometers, DrawingUnit.Feet, DrawingUnit.Inches })
            Units.Add(new UnitItem(unit, unit == drawingUnit));
        selectedUnit = Units.FirstOrDefault(u => u.FromDrawing);
        UnitFromDrawing = selectedUnit is not null;

        _suspend = true;
        RebuildProvinces(selectDefault: true);
        _suspend = false;
    }

    public event Action? Changed;

    public ObservableCollection<ProvinceItem> Provinces { get; } = new();
    public ObservableCollection<MeridianItem> Meridians { get; } = new();
    public ObservableCollection<UnitItem> Units { get; } = new();

    /// <summary>False when INSUNITS was unknown: the unit box then opens with nothing selected and the user must choose.</summary>
    public bool UnitFromDrawing { get; }

    [ObservableProperty] private bool useCurrentCatalog = true;
    [ObservableProperty] private ProvinceItem? selectedProvince;
    [ObservableProperty] private MeridianItem? selectedMeridian;
    [ObservableProperty] private string centralMeridianText = "";
    [ObservableProperty] private string k0Text = TmParameters.Tm3ScaleFactor.ToString(CultureInfo.InvariantCulture);
    [ObservableProperty] private string falseEastingText = TmParameters.DefaultFalseEasting.ToString(CultureInfo.InvariantCulture);
    [ObservableProperty] private string falseNorthingText = TmParameters.DefaultFalseNorthing.ToString(CultureInfo.InvariantCulture);
    [ObservableProperty] private UnitItem? selectedUnit;
    [ObservableProperty] private string meridianNote = "";

    public bool NeedsMeridianChoice => SelectedProvince is { Province.HasSingleMeridian: false } && SelectedMeridian is null;

    /// <summary>The TM parameters the boxes describe; NaN central meridian when the text is empty/invalid so the engine refuses.</summary>
    public TmParameters CurrentTm => new(
        CentralMeridian.Parse(CentralMeridianText) ?? double.NaN,
        ParseNumber(K0Text) ?? double.NaN,
        ParseNumber(FalseEastingText) ?? double.NaN,
        ParseNumber(FalseNorthingText) ?? double.NaN);

    public double MetersPerUnit => SelectedUnit is null ? 0 : DrawingUnitFactor.MetersPerUnit(SelectedUnit.Unit) ?? 0;

    /// <summary>Applies stored settings (a drawing's or the user's last ones); unknown names are ignored.</summary>
    public void Apply(bool currentCatalog, string? provinceName, double? centralMeridian, double? k0, double? falseEasting, double? falseNorthing, DrawingUnit? unit)
    {
        _suspend = true;
        try
        {
            UseCurrentCatalog = currentCatalog;
            if (provinceName is not null) SelectedProvince = Provinces.FirstOrDefault(p => p.Name == provinceName) ?? SelectedProvince;
            if (centralMeridian is { } cm)
            {
                SelectedMeridian = Meridians.FirstOrDefault(m => Math.Abs(m.Degrees - cm) < 1e-9);
                CentralMeridianText = cm.ToString(CultureInfo.InvariantCulture);
            }
            if (k0 is { } k) K0Text = k.ToString(CultureInfo.InvariantCulture);
            if (falseEasting is { } fe) FalseEastingText = fe.ToString(CultureInfo.InvariantCulture);
            if (falseNorthing is { } fn) FalseNorthingText = fn.ToString(CultureInfo.InvariantCulture);
            if (unit is { } u && !UnitFromDrawing) SelectedUnit = Units.FirstOrDefault(x => x.Unit == u) ?? SelectedUnit;
        }
        finally
        {
            _suspend = false;
        }
        Raise();
    }

    partial void OnUseCurrentCatalogChanged(bool value) => RebuildProvinces(selectDefault: false);

    partial void OnSelectedProvinceChanged(ProvinceItem? value)
    {
        Meridians.Clear();
        if (value is null)
        {
            SelectedMeridian = null;
            return;
        }
        foreach (var cm in value.Province.CentralMeridians)
            Meridians.Add(new MeridianItem(cm, _catalog.FormerProvinceLabel(value.Name, cm)));
        SelectedMeridian = value.Province.HasSingleMeridian ? Meridians[0] : null;
        if (!value.Province.HasSingleMeridian) CentralMeridianText = "";
        OnPropertyChanged(nameof(NeedsMeridianChoice));
        Raise();
    }

    partial void OnSelectedMeridianChanged(MeridianItem? value)
    {
        if (value is not null) CentralMeridianText = value.Degrees.ToString(CultureInfo.InvariantCulture);
        MeridianNote = value is { FormerProvince.Length: > 0 } ? $"KTT của {value.FormerProvince}" : "";
        OnPropertyChanged(nameof(NeedsMeridianChoice));
        Raise();
    }

    partial void OnCentralMeridianTextChanged(string value) => Raise();
    partial void OnK0TextChanged(string value) => Raise();
    partial void OnFalseEastingTextChanged(string value) => Raise();
    partial void OnFalseNorthingTextChanged(string value) => Raise();
    partial void OnSelectedUnitChanged(UnitItem? value) => Raise();

    private void RebuildProvinces(bool selectDefault)
    {
        var kind = UseCurrentCatalog ? ProvinceCatalogKind.Current : ProvinceCatalogKind.Legacy;
        Provinces.Clear();
        foreach (var p in _catalog.Get(kind)) Provinces.Add(new ProvinceItem(p));
        var wanted = selectDefault ? DefaultProvince : SelectedProvince?.Name;
        SelectedProvince = Provinces.FirstOrDefault(p => p.Name == wanted);
        if (selectDefault && SelectedProvince is not null)
            SelectedMeridian = Meridians.FirstOrDefault(m => Math.Abs(m.Degrees - DefaultMeridian) < 1e-9) ?? SelectedMeridian;
    }

    private void Raise()
    {
        if (!_suspend) Changed?.Invoke();
    }

    private static double? ParseNumber(string? text) =>
        double.TryParse((text ?? "").Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : null;
}
