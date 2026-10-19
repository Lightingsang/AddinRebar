namespace HPRebar.JoinElements.Model;

/// <summary>
/// Report data payload for reporting progress during Auto Join execution.
/// </summary>
public sealed class JoinProgressReport
{
    public int Current { get; }
    public int Total { get; }
    public double Percentage => Total > 0 ? (double)Current / Total * 100.0 : 0.0;
    public string StepDescription { get; }

    public JoinProgressReport(int current, int total, string stepDescription)
    {
        Current = current;
        Total = total;
        StepDescription = stepDescription;
    }
}
