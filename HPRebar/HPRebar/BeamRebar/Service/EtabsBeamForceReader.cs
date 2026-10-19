using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HPRebar.Core.Tcvn5574.Models;

namespace HPRebar.BeamRebar.Service;

/// <summary>
/// A single station force entry from ETABS analysis output (Frame Forces - Beams).
/// </summary>
public sealed record EtabsStationForce
{
    public string BeamMark { get; init; } = string.Empty;
    public int SpanIndex { get; init; }
    public double StationMm { get; init; }
    public double SpanLengthMm { get; init; }
    public string OutputCase { get; init; } = string.Empty;
    public double P { get; init; }
    public double V2 { get; init; }
    public double M3 { get; init; }
}

/// <summary>
/// Extracts and maps ETABS beam internal design forces into <see cref="BeamForceEnvelope"/> instances.
/// Groups stations by beam span, computes envelopes across combinations, and assigns critical station forces.
/// </summary>
public static class EtabsBeamForceReader
{
    public static IReadOnlyList<BeamForceEnvelope> ExtractEnvelopes(IEnumerable<EtabsStationForce> stationForces)
    {
        if (stationForces is null)
            throw new ArgumentNullException(nameof(stationForces));

        var groups = stationForces
            .GroupBy(f => (f.BeamMark.Trim(), f.SpanIndex))
            .OrderBy(g => g.Key.Item1)
            .ThenBy(g => g.Key.SpanIndex);

        var result = new List<BeamForceEnvelope>();
        foreach (var group in groups)
        {
            var list = group.ToList();
            if (list.Count == 0)
                continue;

            double spanLength = ResolveSpanLength(list);
            result.Add(BuildEnvelopeForSpan(group.Key.Item1, group.Key.SpanIndex, spanLength, list));
        }

        return result;
    }

    public static BeamForceEnvelope BuildEnvelopeForSpan(
        string beamMark,
        int spanIndex,
        double spanLengthMm,
        IReadOnlyList<EtabsStationForce> stations)
    {
        double leftBoundary = spanLengthMm * 0.25;
        double rightBoundary = spanLengthMm * 0.75;

        var leftStations = stations.Where(s => s.StationMm <= leftBoundary).ToList();
        var midStations = stations.Where(s => s.StationMm > leftBoundary && s.StationMm < rightBoundary).ToList();
        var rightStations = stations.Where(s => s.StationMm >= rightBoundary).ToList();

        return new BeamForceEnvelope
        {
            BeamMark = beamMark,
            SpanIndex = spanIndex,
            SpanLengthMm = spanLengthMm,
            MomentSupportLeftKnm = ExtractSupportMoment(leftStations),
            MomentMidSpanKnm = ExtractMidSpanMoment(midStations),
            MomentSupportRightKnm = ExtractSupportMoment(rightStations),
            ShearSupportLeftKn = ExtractMaxShear(leftStations),
            ShearMidSpanKn = ExtractMaxShear(midStations),
            ShearSupportRightKn = ExtractMaxShear(rightStations)
        };
    }

    public static IReadOnlyList<BeamForceEnvelope> ParseTableText(string tableText)
    {
        if (string.IsNullOrWhiteSpace(tableText))
            return Array.Empty<BeamForceEnvelope>();

        var lines = tableText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1)
            return Array.Empty<BeamForceEnvelope>();

        var records = new List<EtabsStationForce>();
        var separator = DetectSeparator(lines[0]);

        for (int i = 1; i < lines.Length; i++)
        {
            var parts = lines[i].Split(separator);
            if (parts.Length < 6)
                continue;

            var rec = ParseRecord(parts);
            if (rec is not null)
                records.Add(rec);
        }

        return ExtractEnvelopes(records);
    }

    private static double ResolveSpanLength(IReadOnlyList<EtabsStationForce> list)
    {
        double maxSpec = list.Max(s => s.SpanLengthMm);
        if (maxSpec > 0.0)
            return maxSpec;

        double maxStation = list.Max(s => s.StationMm);
        return maxStation > 0.0 ? maxStation : 6000.0;
    }

    private static double ExtractSupportMoment(IReadOnlyList<EtabsStationForce> list)
    {
        if (list.Count == 0) return 0.0;
        // Top tension at supports is conventionally negative M3 (hogging). Find largest negative moment.
        double minM = list.Min(s => s.M3);
        if (minM < 0.0) return minM;
        // If all M3 are positive, return largest absolute value with negative sign for top tension.
        return -list.Max(s => Math.Abs(s.M3));
    }

    private static double ExtractMidSpanMoment(IReadOnlyList<EtabsStationForce> list)
    {
        if (list.Count == 0) return 0.0;
        // Bottom tension at mid-span is positive M3 (sagging). Find largest positive moment.
        double maxM = list.Max(s => s.M3);
        return maxM > 0.0 ? maxM : Math.Abs(list.Min(s => s.M3));
    }

    private static double ExtractMaxShear(IReadOnlyList<EtabsStationForce> list)
    {
        if (list.Count == 0) return 0.0;
        return list.Max(s => Math.Abs(s.V2));
    }

    private static char[] DetectSeparator(string headerLine)
    {
        if (headerLine.Contains('\t')) return new[] { '\t' };
        if (headerLine.Contains(',')) return new[] { ',' };
        if (headerLine.Contains(';')) return new[] { ';' };
        return new[] { ' ', '\t' };
    }

    private static EtabsStationForce? ParseRecord(string[] parts)
    {
        string mark = parts[0].Trim();
        if (string.IsNullOrWhiteSpace(mark))
            return null;

        double station = ParseDouble(parts, 1, 0.0);
        string combo = parts.Length > 2 ? parts[2].Trim() : string.Empty;
        double p = ParseDouble(parts, 3, 0.0);
        double v2 = ParseDouble(parts, 4, 0.0);
        double m3 = ParseDouble(parts, 5, 0.0);

        return new EtabsStationForce
        {
            BeamMark = mark,
            SpanIndex = 0,
            StationMm = station,
            OutputCase = combo,
            P = p,
            V2 = v2,
            M3 = m3
        };
    }

    private static double ParseDouble(string[] parts, int index, double defaultValue)
    {
        if (index >= parts.Length) return defaultValue;
        string raw = parts[index].Trim();
        return double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out double val)
            ? val
            : defaultValue;
    }
}
