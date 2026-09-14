using HPRebar.Mcp.Server.Registry.Model;

namespace HPRebar.Mcp.Server.Registry;

/// <summary>
///     How much the registry trusts a tool, from its recent runs alone. A tool with two lucky runs is not
///     "100% stable": confidence ramps up over the first ten runs. Pure functions so the numbers are
///     testable and the thresholds live in one place.
/// </summary>
public static class StabilityScorer
{
    private const int FullConfidenceRuns = 10;

    /// <summary>0..1: success rate damped by how few runs there are.</summary>
    public static double Score(RunStats stats) =>
        stats.Runs == 0 ? 0 : Math.Round(stats.SuccessRate * Math.Min(1.0, (double)stats.Runs / FullConfidenceRuns), 3);

    public static bool ShouldQuarantine(RunStats stats, int minRuns, double maxFailureRate) =>
        stats.Runs >= minRuns && stats.Runs > 0 && (double)stats.Failures / stats.Runs > maxFailureRate;

    /// <summary>Weight of a status in search ranking: only published tools are first-class.</summary>
    public static double StatusWeight(ToolStatus status) => status switch
    {
        ToolStatus.Published => 1.0,
        ToolStatus.Tested => 0.6,
        ToolStatus.PendingApproval => 0.4,
        ToolStatus.Draft => 0.2,
        _ => 0.0,
    };

    /// <summary>textScore × (0.5 + 0.5·stability) × statusWeight — text relevance first, trust second.</summary>
    public static double Rank(double textScore, ToolStatus status, RunStats stats) =>
        Math.Round(Math.Max(textScore, 0.0001) * (0.5 + 0.5 * Score(stats)) * StatusWeight(status), 4);
}
