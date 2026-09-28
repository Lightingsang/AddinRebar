using System;
using System.Collections.Generic;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>Centreline of one stirrup at a station, in the beam's local frame (mm).</summary>
public static class KataStirrupCurveFactory
{
    /// <param name="yMin">Centreline of the stirrup leg on the −Y face.</param>
    /// <param name="yMax">Centreline of the stirrup leg on the +Y face.</param>
    /// <param name="zTop">Centreline of the top branch.</param>
    /// <param name="zBot">Centreline of the bottom branch.</param>
    public static KataRebarCurve Create(
        KataStirrupShapeType shape,
        double x,
        double yMin,
        double yMax,
        double zTop,
        double zBot,
        double dia,
        int id,
        int spanIndex)
    {
        double outWidth = Math.Abs(yMax - yMin) + dia;
        double outHeight = Math.Abs(zTop - zBot) + dia;

        switch (shape)
        {
            case KataStirrupShapeType.CapStirrup:
                return new KataRebarCurve
                {
                    BarId = id,
                    Role = KataBarRole.StirrupCap,
                    Diameter = dia,
                    Polyline = new Polyline3(new List<Point3>
                    {
                        new(x, yMin, zTop),
                        new(x, yMin, zBot),
                        new(x, yMax, zBot),
                        new(x, yMax, zTop)
                    }).Simplify(1.0),
                    HostSpanIndex = spanIndex,
                    ShapeCode = "45",
                    BarMark = "d2",
                    BarDescription = "Đai nắp chữ U",
                    DimA = outWidth,
                    DimB = outHeight,
                    DimR = 2.0 * dia,
                    SttCad = 7
                };

            case KataStirrupShapeType.CrossTie:
            {
                double zMid = (zTop + zBot) / 2.0;
                return new KataRebarCurve
                {
                    BarId = id,
                    Role = KataBarRole.CrossTie,
                    Diameter = dia,
                    Polyline = new Polyline3(new List<Point3> { new(x, yMin, zMid), new(x, yMax, zMid) }).Simplify(1.0),
                    HostSpanIndex = spanIndex,
                    ShapeCode = "24a",
                    BarMark = "d3",
                    BarDescription = "Đai C / móc đan",
                    DimA = outWidth,
                    DimB = 10.0 * dia,
                    DimC = 10.0 * dia,
                    DimR = 2.0 * dia,
                    SttCad = 8
                };
            }

            default:
                return new KataRebarCurve
                {
                    BarId = id,
                    Role = KataBarRole.StirrupClosed,
                    Diameter = dia,
                    Polyline = new Polyline3(new List<Point3>
                    {
                        new(x, yMin, zTop),
                        new(x, yMax, zTop),
                        new(x, yMax, zBot),
                        new(x, yMin, zBot),
                        new(x, yMin, zTop)
                    }, isClosed: true).Simplify(1.0),
                    HostSpanIndex = spanIndex,
                    ShapeCode = "41",
                    BarMark = "d1",
                    BarDescription = "Đai chữ nhật kín",
                    DimA = outWidth,
                    DimB = outHeight,
                    DimR = 2.0 * dia,
                    SttCad = 6
                };
        }
    }

    public static string MarkOf(KataStirrupShapeType shape) => shape switch
    {
        KataStirrupShapeType.CapStirrup => "d2",
        KataStirrupShapeType.CrossTie => "d3",
        _ => "d1"
    };
}
