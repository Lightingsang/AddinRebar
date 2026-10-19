namespace HPBim.Core.Coordination;

/// <summary>
/// Aggregated statistics of coordination progress across all lifecycle steps and technical groups.
/// </summary>
public sealed record CoordinationProgressSummary
{
    public int TotalElements { get; init; }
    public int Step1_ReadCad { get; init; }
    public int Step2_CheckAndClassify { get; init; }
    public int Step3_ModelRevit { get; init; }
    public int Step4_QaqcAudit { get; init; }
    public int Step5_RouteFix { get; init; }
    public int Step6_ReAudit { get; init; }
    public int Step7_ConfirmedPass { get; init; }
    public int Group1_ReadyToModel { get; init; }
    public int Group2_ConflictingInfo { get; init; }
    public int Group3_MissingInfo { get; init; }
    public int UnresolvedHalt { get; init; }
}
