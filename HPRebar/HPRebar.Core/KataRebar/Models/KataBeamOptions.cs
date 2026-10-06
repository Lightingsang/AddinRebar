namespace HPRebar.Core.KataRebar.Models;

/// <summary>
/// Options of Kata's "Detail thép" tab that change how a beam is detailed and that the add-in has no rule for yet.
/// They are kept in the settings file so the dialog matches Kata's; nothing reads them when bars are laid out.
/// </summary>
public sealed record KataBeamOptions
{
    public static readonly KataBeamOptions Default = new();

    /// <summary>"Thép lớp dưới không bẻ ke".</summary>
    public bool BottomLayerNoBend { get; init; }

    /// <summary>"Thép trên ko neo xuống cột dưới".</summary>
    public bool TopNotAnchoredIntoColumnBelow { get; init; } = true;

    /// <summary>"Luôn luôn bẻ ke" and its leg in bar diameters.</summary>
    public bool AlwaysBend { get; init; }

    public double AlwaysBendFactor { get; init; }

    /// <summary>"Cắt thép chạy suốt dầm ở đầu nhịp".</summary>
    public bool CutContinuousBarsAtSpanEnds { get; init; }

    /// <summary>"Cho phép các thanh thép giống nhau đánh số hiệu khác nhau".</summary>
    public bool DifferentNumbersForIdenticalBars { get; init; }
}
