namespace HPBim.Core.Coordination;

using System;
using System.Collections.Generic;

/// <summary>
/// Outcome of a Fail-Fast evaluation containing pass/fail state and violation details.
/// </summary>
public sealed class GateEvaluationResult
{
    private static readonly GateEvaluationResult PassedResult = new(true, Array.Empty<string>());

    public bool IsPassed { get; }
    public IReadOnlyList<string> Violations { get; }

    private GateEvaluationResult(bool isPassed, IReadOnlyList<string> violations)
    {
        IsPassed = isPassed;
        Violations = violations;
    }

    public static GateEvaluationResult Pass() => PassedResult;

    public static GateEvaluationResult Fail(IEnumerable<string> violations)
    {
        if (violations == null)
        {
            throw new ArgumentNullException(nameof(violations));
        }

        var list = new List<string>(violations);
        return new GateEvaluationResult(false, list.AsReadOnly());
    }
}
