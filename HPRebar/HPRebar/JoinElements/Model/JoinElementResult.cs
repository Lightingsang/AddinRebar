using System;

namespace HPRebar.JoinElements.Model;

/// <summary>
/// Execution result of Auto Join operation.
/// </summary>
public sealed class JoinElementResult
{
    public bool Success { get; }
    public int TotalPairsChecked { get; }
    public int TotalJoined { get; }
    public int TotalUnjoined { get; }
    public int TotalSwitched { get; }
    public int TotalSkipped { get; }
    public TimeSpan Duration { get; }
    public string Message { get; }

    public JoinElementResult(
        bool success,
        int totalPairsChecked,
        int totalJoined,
        int totalUnjoined,
        int totalSwitched,
        int totalSkipped,
        TimeSpan duration,
        string message)
    {
        Success = success;
        TotalPairsChecked = totalPairsChecked;
        TotalJoined = totalJoined;
        TotalUnjoined = totalUnjoined;
        TotalSwitched = totalSwitched;
        TotalSkipped = totalSkipped;
        Duration = duration;
        Message = message;
    }

    public static JoinElementResult Succeeded(
        int totalPairs,
        int joined,
        int unjoined,
        int switched,
        int skipped,
        TimeSpan duration) =>
        new(true, totalPairs, joined, unjoined, switched, skipped, duration,
            $"Hoàn thành trong {duration.TotalSeconds:F2}s: Đã Join {joined} cặp, Đảo cắt {switched} cặp, Tách rác {unjoined} cặp, Bỏ qua {skipped} cặp đã chuẩn (Quét {totalPairs} cặp).");

    public static JoinElementResult Failed(string message) =>
        new(false, 0, 0, 0, 0, 0, TimeSpan.Zero, message);
}
