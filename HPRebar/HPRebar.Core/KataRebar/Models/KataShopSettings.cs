namespace HPRebar.Core.KataRebar.Models;

/// <summary>
/// Kata's shop-drawing settings (stock lengths, laps, couplers, mass and lap tables). The add-in draws a design
/// model with whole bars, so nothing reads these when bars are laid out: they are kept for the shop-drawing tool.
/// </summary>
public sealed record KataShopSettings
{
    public static readonly KataShopSettings Default = new();

    /// <summary>"Coupler cho thép có phi từ" (mm).</summary>
    public double CouplerMinDiameter { get; init; } = 30.0;

    /// <summary>"Chiều dài thép tối đa" (mm).</summary>
    public double MaxBarLengthMm { get; init; } = 11700.0;

    /// <summary>"Chiều dài Min xét cắt thép" (mm).</summary>
    public double MinLengthForCuttingMm { get; init; } = 8800.0;

    /// <summary>"Chiều dài thép tối thiểu", in bar diameters.</summary>
    public double MinBarLengthFactor { get; init; } = 100.0;

    /// <summary>"Làm tròn đoạn neo nối" (mm).</summary>
    public double RoundLapMm { get; init; } = 5.0;

    /// <summary>"Thép trên được nối ở giữa nhịp" / "ở vùng gối".</summary>
    public bool TopLapAtMidspan { get; init; } = true;

    public bool TopLapAtSupport { get; init; }

    /// <summary>"Thép dưới được nối ở vùng gối" / "ở giữa nhịp".</summary>
    public bool BottomLapAtSupport { get; init; } = true;

    public bool BottomLapAtMidspan { get; init; }

    /// <summary>"Vùng nối thép trên": fraction of L, measured from the support face or from its centre.</summary>
    public double TopLapZoneFraction { get; init; } = 0.25;

    public bool TopLapZoneFromCentre { get; init; }

    /// <summary>"Vùng nối thép dưới".</summary>
    public double BottomLapZoneFraction { get; init; } = 0.2;

    public bool BottomLapZoneFromCentre { get; init; }

    /// <summary>"Ưu tiên phương án ít mối nối hơn là khả năng phối hợp thép khi cắt".</summary>
    public bool PreferFewerLaps { get; init; }

    /// <summary>"Nối thép vùng nén", in bar diameters.</summary>
    public double CompressionLapFactor { get; init; } = 30.0;

    /// <summary>"Cho phép nối ở vùng kéo" and "Nối thép vùng kéo" in bar diameters.</summary>
    public bool AllowTensionZoneLap { get; init; }

    public double TensionLapFactor { get; init; } = 40.0;

    /// <summary>"Mép nhấn cách mép nối" (mm).</summary>
    public double CrankToLapEdgeMm { get; init; }

    /// <summary>"Khoảng cách Min 2 mối nối" (mm).</summary>
    public double MinLapGapMm { get; init; } = 40.0;

    /// <summary>"Dung sai khi phối thép" (mm).</summary>
    public double CuttingToleranceMm { get; init; } = 50.0;

    /// <summary>"Lấy khối lượng thép theo table dưới" and its rows, "diameter:kg per m" joined by ';'.</summary>
    public bool UseUnitMassTable { get; init; }

    public string UnitMassTable { get; init; } = "";

    /// <summary>
    /// "Ưu tiên lấy chiều dài neo nối thép theo table bên dưới" and its rows, "diameter:lap compression,lap tension,
    /// anchor compression,anchor tension" (mm) joined by ';'.
    /// </summary>
    public bool UseLapTable { get; init; }

    public string LapTable { get; init; } = "";
}
