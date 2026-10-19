namespace HPBim.Core.Coordination;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Interface for Fail-Fast engineering gate evaluating coordination element readiness.
/// </summary>
public interface IFailFastGate
{
    GateEvaluationResult Evaluate(CoordinationElementRecord element);
    void AssertReadyForRevit(CoordinationElementRecord element);
    GateEvaluationResult EvaluateCadSource(CadSourceSpec spec, CoordinationCategory category, string elementId = "");
    void AssertValidCadSource(CadSourceSpec spec, CoordinationCategory category, string elementId = "");
}

/// <summary>
/// Concrete implementation of Fail-Fast Gate.
/// Enforces zero-speculation rules: requires explicit elevations, cross sections, and supports.
/// </summary>
public sealed class FailFastGate : IFailFastGate
{
    public GateEvaluationResult Evaluate(CoordinationElementRecord element)
    {
        if (element == null)
        {
            throw new ArgumentNullException(nameof(element));
        }

        return EvaluateCadSource(element.CadSourceSpec, element.Category, element.Id);
    }

    public void AssertReadyForRevit(CoordinationElementRecord element)
    {
        if (element == null)
        {
            throw new ArgumentNullException(nameof(element));
        }

        var result = Evaluate(element);
        if (!result.IsPassed)
        {
            throw new FailFastCoordinationException(element.Id, result.Violations);
        }
    }

    public GateEvaluationResult EvaluateCadSource(
        CadSourceSpec spec, CoordinationCategory category, string elementId = "")
    {
        if (spec == null)
        {
            return GateEvaluationResult.Fail(new[] { "FF-0: Thiếu đặc tả nguồn CAD (CadSourceSpec is null)." });
        }

        var violations = new List<string>();

        CheckCadHandle(spec, violations);
        CheckElevations(spec, category, violations);
        CheckDimensions(spec, category, violations);
        CheckSupports(spec, category, violations);
        CheckCrossCheckStatus(spec, violations);

        if (violations.Count > 0)
        {
            return GateEvaluationResult.Fail(violations);
        }

        return GateEvaluationResult.Pass();
    }

    public void AssertValidCadSource(CadSourceSpec spec, CoordinationCategory category, string elementId = "")
    {
        var result = EvaluateCadSource(spec, category, elementId);
        if (!result.IsPassed)
        {
            throw new FailFastCoordinationException(elementId, result.Violations);
        }
    }

    private static void CheckCadHandle(CadSourceSpec spec, List<string> violations)
    {
        if (string.IsNullOrWhiteSpace(spec.Handle))
        {
            violations.Add("FF-1: Cấu kiện không có CAD Handle thực tế, nghi vấn tự sinh từ thuật toán ghép nét.");
        }
    }

    private static void CheckElevations(CadSourceSpec spec, CoordinationCategory category, List<string> violations)
    {
        if (!spec.TopElevation.HasValue || !IsFinite(spec.TopElevation.Value))
        {
            violations.Add("FF-3: Thiếu thông số cao độ đỉnh (TopElevation không hợp lệ hoặc không xác định).");
        }

        if (category == CoordinationCategory.Footing && (!spec.BottomElevation.HasValue || !IsFinite(spec.BottomElevation.Value)))
        {
            violations.Add("FF-3: Đài móng thiếu cao độ đáy đài (BottomElevation) để xác định độ ngàm cọc.");
        }
        else if (spec.BottomElevation.HasValue && !IsFinite(spec.BottomElevation.Value))
        {
            violations.Add("FF-3: Sai lệch thông số cao độ đáy (BottomElevation không hợp lệ).");
        }

        if (spec.TopElevation.HasValue && spec.BottomElevation.HasValue &&
            IsFinite(spec.TopElevation.Value) && IsFinite(spec.BottomElevation.Value))
        {
            if (spec.BottomElevation.Value >= spec.TopElevation.Value)
            {
                violations.Add($"FF-3: Cao độ đáy ({spec.BottomElevation:F3}m) >= cao độ đỉnh ({spec.TopElevation:F3}m).");
            }
        }
    }

    private static void CheckDimensions(CadSourceSpec spec, CoordinationCategory category, List<string> violations)
    {
        if (category == CoordinationCategory.Pile)
        {
            bool hasValidDiameter = spec.Diameter.HasValue && IsFinite(spec.Diameter.Value) && spec.Diameter.Value > 0;
            bool hasValidRect = spec.Width.HasValue && spec.Height.HasValue &&
                                IsFinite(spec.Width.Value) && IsFinite(spec.Height.Value) &&
                                spec.Width.Value > 0 && spec.Height.Value > 0;

            if (!hasValidDiameter && !hasValidRect)
            {
                violations.Add("FF-4: Cọc thiếu kích thước tiết diện (Diameter hoặc Width/Height <= 0).");
            }
            return;
        }

        if (!spec.Width.HasValue || !IsFinite(spec.Width.Value) || spec.Width.Value <= 0)
        {
            violations.Add("FF-4: Thiếu hoặc sai lệch chiều rộng tiết diện (Width <= 0).");
        }

        if (!spec.Height.HasValue || !IsFinite(spec.Height.Value) || spec.Height.Value <= 0)
        {
            violations.Add("FF-4: Thiếu hoặc sai lệch chiều cao tiết diện (Height <= 0).");
        }
    }

    private static void CheckSupports(CadSourceSpec spec, CoordinationCategory category, List<string> violations)
    {
        var validSupports = spec.Supports != null
            ? spec.Supports.Where(s => !string.IsNullOrWhiteSpace(s)).ToList()
            : new List<string>();

        if (category == CoordinationCategory.GradeBeam)
        {
            if (validSupports.Count < 2)
            {
                violations.Add("FF-5: Giằng móng thiếu điểm tựa liên kết 2 đầu (yêu cầu ít nhất 2 điểm tựa).");
            }
            return;
        }

        if (category == CoordinationCategory.Pile)
        {
            if (validSupports.Count < 1)
            {
                violations.Add("FF-5: Cọc thiếu định vị đài móng liên kết bao quanh.");
            }
            return;
        }

        if (category == CoordinationCategory.Footing)
        {
            if (validSupports.Count < 1)
            {
                violations.Add("FF-5: Đài móng thiếu định vị điểm tựa liên kết (cọc hoặc lưới trục).");
            }
            return;
        }

        if (category == CoordinationCategory.Column)
        {
            if (validSupports.Count < 1)
            {
                violations.Add("FF-5: Cột thiếu định vị điểm tựa móng hoặc dầm sàn liên kết.");
            }
        }
    }

    private static void CheckCrossCheckStatus(CadSourceSpec spec, List<string> violations)
    {
        if (!spec.CrossCheckPassed)
        {
            violations.Add("FF-6: Cấu kiện chưa vượt qua đối soát chéo 6 nguồn hoặc có mâu thuẫn.");
        }
    }

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}
