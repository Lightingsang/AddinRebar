using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using HPRebar.BeamRebar.Service;
using HPRebar.Core.KataExport.Models;
using HPRebar.Core.KataRebar.Models;
using HPRebar.KataExport.Service;
using HPRebar.KataRebar.Model;

namespace HPRebar.KataRebar.Service;

/// <summary>
/// Matches Revit structural framing selection with a KataBeamRebarSpec, verifies collinearity and span counts,
/// and builds an orthonormal PointMapper coordinate frame from Kata local millimetres to Revit world space.
/// </summary>
public static class KataBeamMatcher
{
    private const double MaxAngleDegrees = 1.0;
    private const double MaxLateralOffsetMm = 25.0;
    private const double MaxElevationOffsetMm = 25.0;
    private const double MaxSlopeZ = 1e-3;

    /// <summary>
    /// Matches selected structural framing elements with the specified Kata beam specification.
    /// </summary>
    public static KataBeamMatchResult Match(
        Document doc,
        IReadOnlyList<Element> selectedElements,
        KataBeamRebarSpec spec)
    {
        if (spec is null)
            throw new ArgumentNullException(nameof(spec));

        if (selectedElements is null || selectedElements.Count == 0)
        {
            return new KataBeamMatchResult
            {
                IsSuccess = false,
                Message = "Chưa chọn phần tử dầm kết cấu nào trong Revit."
            };
        }

        var framingInstances = new List<FamilyInstance>();
        var lines = new List<(FamilyInstance Element, Line Line)>();

        foreach (var elem in selectedElements)
        {
            if (elem is FamilyInstance instance && elem.Category?.BuiltInCategory == BuiltInCategory.OST_StructuralFraming)
            {
                if (instance.Location is LocationCurve locCurve && locCurve.Curve is Line line)
                {
                    framingInstances.Add(instance);
                    lines.Add((instance, line));
                }
                else
                {
                    return new KataBeamMatchResult
                    {
                        IsSuccess = false,
                        Message = $"Dầm {elem.Id} không có đường tim trục thẳng (chỉ hỗ trợ dầm thẳng)."
                    };
                }
            }
        }

        if (framingInstances.Count == 0)
        {
            return new KataBeamMatchResult
            {
                IsSuccess = false,
                Message = "Không tìm thấy phần tử dầm kết cấu (Structural Framing) hợp lệ trong danh sách chọn."
            };
        }

        var warnings = new List<string>();

        // 1. Establish axis frame from the first beam line
        var frame = KataAxisFrame.FromLine(lines[0].Line);

        // 2. Collinearity, slope & elevation verification
        double z0 = lines[0].Line.GetEndPoint(0).Z;
        foreach (var (element, line) in lines)
        {
            if (Math.Abs(line.Direction.Z) > MaxSlopeZ)
            {
                return new KataBeamMatchResult
                {
                    IsSuccess = false,
                    Message = $"Dầm {element.Id} là dầm dốc; Kata Rebar hiện chỉ hỗ trợ dầm nằm ngang."
                };
            }

            double zDiffMm = HPRebar.KataExport.Service.RevitUnits.FtToMm(Math.Abs(line.GetEndPoint(0).Z - z0));
            if (zDiffMm > MaxElevationOffsetMm)
            {
                return new KataBeamMatchResult
                {
                    IsSuccess = false,
                    Message = $"Dầm {element.Id} có cao độ khác biệt ({zDiffMm:0.#} mm) so với dải dầm."
                };
            }

            if (!frame.IsParallel(line.Direction, MaxAngleDegrees))
            {
                return new KataBeamMatchResult
                {
                    IsSuccess = false,
                    Message = $"Dầm {element.Id} không thẳng hàng với dải dầm; dải dầm Kata phải đồng trục."
                };
            }

            double p0Offset = Math.Abs(frame.Offset(line.GetEndPoint(0)));
            double p1Offset = Math.Abs(frame.Offset(line.GetEndPoint(1)));
            double maxOffset = Math.Max(p0Offset, p1Offset);

            if (maxOffset > MaxLateralOffsetMm)
            {
                return new KataBeamMatchResult
                {
                    IsSuccess = false,
                    Message = $"Dầm {element.Id} lệch trục {maxOffset:0.#} mm (vượt quá dung sai {MaxLateralOffsetMm} mm)."
                };
            }
        }

        // 3. Order beams along the axis
        var orderedLines = lines
            .Select(item =>
            {
                double s0 = frame.Station(item.Line.GetEndPoint(0));
                double s1 = frame.Station(item.Line.GetEndPoint(1));
                return (item.Element, item.Line, MinStation: Math.Min(s0, s1), MaxStation: Math.Max(s0, s1));
            })
            .OrderBy(item => item.MinStation)
            .ToList();

        var orderedBeams = orderedLines.Select(item => item.Element).ToList();

        // 4. Validate span count
        int revitCount = orderedBeams.Count;
        int kataSpanCount = spec.Spans.Count;

        if (revitCount != kataSpanCount && revitCount != 1)
        {
            warnings.Add($"Số lượng dầm trong Revit ({revitCount}) khác số nhịp trong bảng tính Kata ({kataSpanCount}).");
        }

        // 5. Check beam name match
        bool nameMatched = false;
        if (!string.IsNullOrWhiteSpace(spec.BeamName))
        {
            foreach (var beam in orderedBeams)
            {
                string mark = beam.get_Parameter(BuiltInParameter.DOOR_NUMBER)?.AsString() // Mark
                              ?? beam.LookupParameter("Mark")?.AsString() ?? "";
                string comment = beam.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString() ?? "";
                string typeName = beam.Symbol?.Name ?? "";
                string famName = beam.Symbol?.FamilyName ?? "";

                if (mark.Contains(spec.BeamName, StringComparison.OrdinalIgnoreCase) ||
                    comment.Contains(spec.BeamName, StringComparison.OrdinalIgnoreCase) ||
                    typeName.Contains(spec.BeamName, StringComparison.OrdinalIgnoreCase) ||
                    famName.Contains(spec.BeamName, StringComparison.OrdinalIgnoreCase))
                {
                    nameMatched = true;
                    break;
                }
            }

            if (!nameMatched)
            {
                warnings.Add($"Tên dầm trong Revit không chứa '{spec.BeamName}' từ bảng tính Kata (vẫn tiếp tục tạo theo yêu cầu).");
            }
        }

        // 6. Calculate total length in Kata
        double kataTotalLengthMm = spec.Spans.Sum(s => s.Length) + spec.Supports.Sum(s => s.ColumnWidth);
        if (kataTotalLengthMm <= 0.0)
        {
            kataTotalLengthMm = spec.Spans.Sum(s => s.Length);
        }

        // Measure Revit beam run stations
        double runMinStationMm = orderedLines.Min(i => i.MinStation);
        double runMaxStationMm = orderedLines.Max(i => i.MaxStation);
        double revitSpanLengthMm = runMaxStationMm - runMinStationMm;

        double c0 = spec.Supports.Count > 0 ? Math.Max(0.0, spec.Supports[0].ColumnWidth) : 0.0;
        double cLast = spec.Supports.Count > spec.Spans.Count ? Math.Max(0.0, spec.Supports[spec.Spans.Count].ColumnWidth) : 0.0;

        // Determine X = 0 origin station along the run
        double originStationMm;
        if (Math.Abs(revitSpanLengthMm - kataTotalLengthMm) <= 100.0)
        {
            // Case A: Model already spans outer-to-outer face
            originStationMm = runMinStationMm;
        }
        else if (Math.Abs(revitSpanLengthMm - (kataTotalLengthMm - c0 - cLast)) <= 200.0)
        {
            // Case B: Model represents clear spans between column inner faces
            originStationMm = runMinStationMm - c0;
        }
        else if (Math.Abs(revitSpanLengthMm - (kataTotalLengthMm - (c0 / 2.0) - (cLast / 2.0))) <= 200.0)
        {
            // Case C: Model represents column center-to-center framing
            originStationMm = runMinStationMm - (c0 / 2.0);
        }
        else
        {
            // Fallback: if clear span or partial, offset by c0/2 if present
            originStationMm = (c0 > 0.0 && revitSpanLengthMm < kataTotalLengthMm)
                ? runMinStationMm - (c0 / 2.0)
                : runMinStationMm;
        }

        // 7. Determine Top Elevation in Revit
        double topElevationFt = orderedBeams
            .Select(b => b.get_BoundingBox(null)?.Max.Z ?? (b.Location as LocationCurve)!.Curve.GetEndPoint(0).Z)
            .Max();

        // 8. Build origin point at (originStationMm, 0 transverse, topElevationFt)
        XYZ originXyz = frame.Point(originStationMm, 0.0, topElevationFt);
        var pointMapper = new PointMapper(originXyz, frame.Axis, frame.Transverse, XYZ.BasisZ);

        return new KataBeamMatchResult
        {
            IsSuccess = true,
            Message = $"Khớp thành công dải dầm ({orderedBeams.Count} đoạn) với cấu hình {spec.BeamName}.",
            OrderedBeams = orderedBeams,
            PointMapper = pointMapper,
            AxisFrame = frame,
            BeamTopElevationFt = topElevationFt,
            BeamWidthMm = spec.Width,
            BeamHeightMm = spec.Height,
            TotalRunLengthMm = kataTotalLengthMm,
            Warnings = warnings
        };
    }
}
