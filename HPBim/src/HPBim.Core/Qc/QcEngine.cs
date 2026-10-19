namespace HPBim.Core.Qc;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using HPBim.Core.Models;
using HPBim.Core.Tcvn;

public sealed record QcOptions(
    bool CheckClashes = true,
    bool CheckClearances = true,
    bool CheckTcvnCompliance = true);

/// <summary>
/// Orchestrates all pre-flight verification checks on a BimModel prior to Revit dispatch.
/// </summary>
public sealed class QcEngine
{
    private readonly QcClashDetector _clashDetector = new();
    private readonly ClearanceChecker _clearanceChecker = new();

    public QcReport Validate(BimModel model, QcOptions? options = null)
    {
        if (model == null) throw new ArgumentNullException(nameof(model));
        var opts = options ?? new QcOptions(true, true, true);
        var sw = Stopwatch.StartNew();
        var errors = new List<QcViolation>();
        var warnings = new List<QcViolation>();
        var clashes = new List<QcClash>();
        int totalChecks = 0;

        if (opts.CheckTcvnCompliance)
        {
            totalChecks++;
            ValidateTcvnCovers(model.Rebars, errors);
        }

        if (opts.CheckClashes)
        {
            totalChecks += 2;
            clashes.AddRange(_clashDetector.DetectAll(model));
        }

        if (opts.CheckClearances)
        {
            totalChecks += 3;
            var clearanceViolations = _clearanceChecker.CheckAll(model);
            foreach (var v in clearanceViolations)
            {
                if (v.Severity == QcSeverity.Error || v.Severity == QcSeverity.Critical)
                {
                    errors.Add(v);
                }
                else
                {
                    warnings.Add(v);
                }
            }
        }

        sw.Stop();
        return new QcReport(errors, warnings, clashes, totalChecks, sw.Elapsed);
    }

    private static void ValidateTcvnCovers(IReadOnlyList<RebarDef> rebars, List<QcViolation> errors)
    {
        if (rebars == null)
        {
            return;
        }

        foreach (var bar in rebars)
        {
            if (bar.CoverMm > 0 && bar.CoverMm < ConcreteCoverRules.SlabCoverMm)
            {
                errors.Add(new QcViolation(
                    QcSeverity.Error, "TCVN-COVER-001",
                    $"Thanh thép {bar.BarMark} có lớp bảo vệ {bar.CoverMm:F1}mm nhỏ hơn mức tối thiểu 15mm của TCVN."));
            }
        }
    }
}
