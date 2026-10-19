namespace HPBim.Core.Tcvn;

using System;
using System.Collections.Generic;
using HPBim.Core.Models;

public enum LapZone
{
    Tension,
    Compression
}

public readonly record struct CommercialBarSpliceResult(
    int SpliceCount,
    double SingleLapLengthMm,
    double DevelopedLengthMm);

public readonly record struct BeamStirrupZones(
    double ClearSpanMm,
    double DenseZoneLengthMm,
    double DenseSpacingMm,
    double SparseSpacingMm,
    int DenseZoneCountStart,
    int SparseZoneCount,
    int DenseZoneCountEnd);

public readonly record struct ColumnStirrupZones(
    double StoreyHeightMm,
    double DenseZoneLengthMm,
    double DenseSpacingMm,
    double SparseSpacingMm,
    int DenseCountBottom,
    int SparseCountMiddle,
    int DenseCountTop);

/// <summary>
/// Core mathematical calculations for rebar detailing per TCVN 5574:2018.
/// </summary>
public static class TcvnRebarCalculator
{
    private const double WeightDivisor = 162.2;

    public static double CalculateUnitWeight(double diameterMm)
    {
        return (diameterMm * diameterMm) / WeightDivisor;
    }

    public static double CalculateWeight(double diameterMm, double lengthM)
    {
        return CalculateUnitWeight(diameterMm) * lengthM;
    }

    public static double CalculateWeightFromMm(double diameterMm, double lengthMm)
    {
        return CalculateWeight(diameterMm, lengthMm / 1000.0);
    }

    public static double CalculateTotalWeight(double diameterMm, double lengthMm, int quantity)
    {
        return CalculateWeightFromMm(diameterMm, lengthMm) * quantity;
    }

    public static double GetNominalUnitWeight(int diameterMm) => diameterMm switch
    {
        6 => 0.222,
        8 => 0.395,
        10 => 0.617,
        12 => 0.888,
        14 => 1.208,
        16 => 1.578,
        18 => 1.998,
        20 => 2.466,
        22 => 2.984,
        25 => 3.853,
        _ => CalculateUnitWeight(diameterMm)
    };

    public static (double SmallDiaWeightKg, double LargeDiaWeightKg) PartitionWeightByDiameter(
        IEnumerable<RebarDef> rebars, double thresholdMm = 10.0)
    {
        double small = 0.0;
        double large = 0.0;
        if (rebars != null)
        {
            foreach (var bar in rebars)
            {
                double weight = CalculateTotalWeight(bar.DiameterMm, bar.LengthMm, bar.Quantity);
                if (bar.DiameterMm <= thresholdMm)
                {
                    small += weight;
                }
                else
                {
                    large += weight;
                }
            }
        }
        return (small, large);
    }

    public static double CalculateAnchorageLengthMm(double diameterMm)
    {
        return TcvnStandards.AnchorageMultiplier * diameterMm;
    }

    public static double CalculateHook90LengthMm(double diameterMm)
    {
        return TcvnStandards.Hook90Multiplier * diameterMm;
    }

    public static double CalculateTensionLapLengthMm(double diameterMm)
    {
        return TcvnStandards.LapTensionMultiplier * diameterMm;
    }

    public static double CalculateCompressionLapLengthMm(double diameterMm)
    {
        return TcvnStandards.LapCompressionMultiplier * diameterMm;
    }

    public static double CalculateLapLengthMm(double diameterMm, LapZone zone)
    {
        return zone == LapZone.Tension
            ? CalculateTensionLapLengthMm(diameterMm)
            : CalculateCompressionLapLengthMm(diameterMm);
    }

    public static CommercialBarSpliceResult CalculateCommercialBarSplices(
        double totalSpanLengthMm, double diameterMm, LapZone zone)
    {
        double maxLen = TcvnStandards.CommercialBarMaxLengthMm;
        if (totalSpanLengthMm <= maxLen)
        {
            return new CommercialBarSpliceResult(0, 0.0, totalSpanLengthMm);
        }

        int spliceCount = (int)Math.Ceiling(totalSpanLengthMm / maxLen) - 1;
        double lapLen = CalculateLapLengthMm(diameterMm, zone);
        double developedLength = totalSpanLengthMm + (spliceCount * lapLen);

        return new CommercialBarSpliceResult(spliceCount, lapLen, developedLength);
    }

    public static double CalculateStirrupHookLengthMm(double diameterMm)
    {
        double byMultiplier = TcvnStandards.StirrupHookMultiplier * diameterMm;
        return Math.Max(byMultiplier, TcvnStandards.StirrupHookMinExtensionMm);
    }

    public static BeamStirrupZones CalculateBeamStirrupZones(
        double clearSpanMm, double beamHeightMm, double denseSpacingMm = 100.0, double sparseSpacingMm = 200.0)
    {
        double denseZoneLength = Math.Min(2.0 * beamHeightMm, clearSpanMm / 3.0);
        int denseCountStart = (int)Math.Floor(denseZoneLength / denseSpacingMm);
        double midSpanLength = clearSpanMm - (2.0 * denseZoneLength);
        int sparseCount = midSpanLength > 0 ? (int)Math.Floor(midSpanLength / sparseSpacingMm) : 0;
        int denseCountEnd = denseCountStart;

        return new BeamStirrupZones(
            clearSpanMm, denseZoneLength, denseSpacingMm, sparseSpacingMm,
            denseCountStart, sparseCount, denseCountEnd);
    }

    public static ColumnStirrupZones CalculateColumnStirrupZones(
        double storeyHeightMm, double denseSpacingMm = 100.0, double sparseSpacingMm = 200.0)
    {
        double denseZoneLength = storeyHeightMm / 4.0;
        int denseCountBottom = (int)Math.Floor(denseZoneLength / denseSpacingMm);
        double midHeight = storeyHeightMm - (2.0 * denseZoneLength);
        int sparseCountMiddle = midHeight > 0 ? (int)Math.Floor(midHeight / sparseSpacingMm) : 0;
        int denseCountTop = denseCountBottom;

        return new ColumnStirrupZones(
            storeyHeightMm, denseZoneLength, denseSpacingMm, sparseSpacingMm,
            denseCountBottom, sparseCountMiddle, denseCountTop);
    }
}
