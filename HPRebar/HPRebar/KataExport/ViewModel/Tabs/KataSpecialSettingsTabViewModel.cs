using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;

namespace HPRebar.KataExport.ViewModel.Tabs;

/// <summary>
/// Tab "Thông số đặc thù": lap lengths and the two tables of Kata's dialog. All of it is for the shop drawings and
/// kept for the later tool; nothing here changes the bars drawn now.
/// </summary>
public sealed partial class KataSpecialSettingsTabViewModel : ObservableObject
{
    [ObservableProperty] private bool _useUnitMassTable;
    [ObservableProperty] private double _compressionLapFactor;
    [ObservableProperty] private bool _allowTensionZoneLap;
    [ObservableProperty] private double _tensionLapFactor;
    [ObservableProperty] private double _crankToLapEdgeMm;
    [ObservableProperty] private double _minLapGapMm;
    [ObservableProperty] private double _cuttingToleranceMm;
    [ObservableProperty] private bool _useLapTable;

    public ObservableCollection<KataMassRowViewModel> MassRows { get; } = new();

    public ObservableCollection<KataLapRowViewModel> LapRows { get; } = new();

    public void Import(KataShopSettings shop)
    {
        UseUnitMassTable = shop.UseUnitMassTable;
        CompressionLapFactor = shop.CompressionLapFactor;
        AllowTensionZoneLap = shop.AllowTensionZoneLap;
        TensionLapFactor = shop.TensionLapFactor;
        CrankToLapEdgeMm = shop.CrankToLapEdgeMm;
        MinLapGapMm = shop.MinLapGapMm;
        CuttingToleranceMm = shop.CuttingToleranceMm;
        UseLapTable = shop.UseLapTable;

        MassRows.Clear();
        foreach (var (d, kg) in KataShopTables.ReadMass(shop.UnitMassTable))
            MassRows.Add(new KataMassRowViewModel { Diameter = d, KgPerM = kg });
        LapRows.Clear();
        foreach (var row in KataShopTables.ReadLaps(shop.LapTable))
            LapRows.Add(new KataLapRowViewModel
            {
                Diameter = row.Diameter,
                LapCompression = row.LapCompression,
                LapTension = row.LapTension,
                AnchorCompression = row.AnchorCompression,
                AnchorTension = row.AnchorTension
            });
    }

    /// <summary>What is wrong with the tab, or null. Rows left empty are ignored.</summary>
    public string? Validate()
    {
        double[] all = { CompressionLapFactor, TensionLapFactor, CrankToLapEdgeMm, MinLapGapMm, CuttingToleranceMm };
        if (Array.Exists(all, v => double.IsNaN(v) || double.IsInfinity(v) || v < 0.0))
            return "Thông số đặc thù: chiều dài nối, khoảng cách và dung sai phải là số không âm.";

        var mass = MassRows.Where(r => !r.IsBlank).ToList();
        if (mass.Any(r => r.Diameter <= 0.0 || r.KgPerM < 0.0) || mass.Select(r => r.Diameter).Distinct().Count() != mass.Count)
            return "Bảng khối lượng: mỗi dòng một đường kính dương, khối lượng không âm, không trùng đường kính.";

        var laps = LapRows.Where(r => !r.IsBlank).ToList();
        if (laps.Any(r => r.Diameter <= 0.0 || r.LapCompression < 0.0 || r.LapTension < 0.0 || r.AnchorCompression < 0.0 || r.AnchorTension < 0.0)
            || laps.Select(r => r.Diameter).Distinct().Count() != laps.Count)
            return "Bảng neo nối: mỗi dòng một đường kính dương, chiều dài không âm, không trùng đường kính.";

        return null;
    }

    public KataShopSettings ExportShop(KataShopSettings start) => start with
    {
        UseUnitMassTable = UseUnitMassTable,
        UnitMassTable = KataShopTables.WriteMass(MassRows.Where(r => !r.IsBlank).Select(r => (r.Diameter, r.KgPerM))),
        CompressionLapFactor = CompressionLapFactor,
        AllowTensionZoneLap = AllowTensionZoneLap,
        TensionLapFactor = TensionLapFactor,
        CrankToLapEdgeMm = CrankToLapEdgeMm,
        MinLapGapMm = MinLapGapMm,
        CuttingToleranceMm = CuttingToleranceMm,
        UseLapTable = UseLapTable,
        LapTable = KataShopTables.WriteLaps(LapRows.Where(r => !r.IsBlank)
            .Select(r => new KataLapRow(r.Diameter, r.LapCompression, r.LapTension, r.AnchorCompression, r.AnchorTension)))
    };
}
