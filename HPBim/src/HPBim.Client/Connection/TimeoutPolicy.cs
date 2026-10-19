namespace HPBim.Client.Connection;

using System;

/// <summary>
/// 3-tier timeout configuration separating Transport connection, Queue wait, and Execution budget.
/// Prevents premature client aborts while enforcing deterministic timeout boundaries.
/// </summary>
public sealed record TimeoutPolicy
{
    public TimeSpan TransportTimeout { get; init; } = TimeSpan.FromSeconds(5);
    public TimeSpan QueueDeadline { get; init; } = TimeSpan.FromSeconds(30);
    public TimeSpan BaseExecutionBudget { get; init; } = TimeSpan.FromSeconds(15);
    public TimeSpan PerItemBudget { get; init; } = TimeSpan.FromMilliseconds(50);
    public TimeSpan DefaultExecutionBudget { get; init; } = TimeSpan.FromSeconds(60);
    public TimeSpan MaxExecutionBudget { get; init; } = TimeSpan.FromMinutes(5);

    public TimeSpan ComputeExecutionBudget(int itemCount)
    {
        if (itemCount <= 0) return DefaultExecutionBudget;

        long totalMs = (long)BaseExecutionBudget.TotalMilliseconds + (itemCount * (long)PerItemBudget.TotalMilliseconds);
        var budget = TimeSpan.FromMilliseconds(totalMs);

        if (budget > MaxExecutionBudget) return MaxExecutionBudget;
        return budget < BaseExecutionBudget ? BaseExecutionBudget : budget;
    }

    public TimeSpan ComputeTotalTimeout(int itemCount) =>
        QueueDeadline + ComputeExecutionBudget(itemCount);

    public TimeSpan CalculateExecutionBudget(int itemCount) => ComputeExecutionBudget(itemCount);

    public TimeSpan CalculateTotalTimeout(int itemCount) => ComputeTotalTimeout(itemCount);

    public void Validate()
    {
        if (TransportTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(TransportTimeout), "TransportTimeout must be positive.");
        if (QueueDeadline <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(QueueDeadline), "QueueDeadline must be positive.");
        if (BaseExecutionBudget <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(BaseExecutionBudget), "BaseExecutionBudget must be positive.");
        if (MaxExecutionBudget < BaseExecutionBudget)
            throw new ArgumentOutOfRangeException(nameof(MaxExecutionBudget), "MaxExecutionBudget cannot be less than BaseExecutionBudget.");
    }
}
