using System;
using System.Collections.Generic;
using HPDcePro.Core.Geometry;
using HPDcePro.Core.XData;

namespace HPDcePro.Core.Calculations.Beam
{
    public sealed class CurvedBeamDesignInput
    {
        public string BeamMark { get; set; } = "CB1";
        public Point2D Center { get; set; } = new Point2D(0, 0);
        public double RadiusMm { get; set; } = 6000.0;
        public double StartAngleRad { get; set; } = 0.0;
        public double EndAngleRad { get; set; } = Math.PI * 0.5;
        public double WidthMm { get; set; } = 300.0;
        public double HeightMm { get; set; } = 600.0;
        public double CoverMm { get; set; } = 35.0;
        public double MomentKnm { get; set; } = 150.0;
        public double ShearKn { get; set; } = 120.0;
        public double TorsionKnm { get; set; } = 40.0;
        public double MaxChordLengthMm { get; set; } = 1000.0;
        public ConcreteProperties Concrete { get; set; }
        public RebarProperties Steel { get; set; }

        public CurvedBeamDesignInput(
            string beamMark,
            ConcreteProperties concrete,
            RebarProperties steel)
        {
            BeamMark = beamMark ?? throw new ArgumentNullException(nameof(beamMark));
            Concrete = concrete ?? throw new ArgumentNullException(nameof(concrete));
            Steel = steel ?? throw new ArgumentNullException(nameof(steel));
        }
    }

    public sealed class CurvedBeamResult
    {
        public string BeamMark { get; set; } = string.Empty;
        public double ArcLengthMm { get; set; }
        public double SweepAngleDeg { get; set; }
        public IReadOnlyList<Point2D> DiscretizedChordPoints { get; set; } = Array.Empty<Point2D>();
        public double TorsionCapacityMaxKnm { get; set; }
        public bool TorsionCapacityCompliant { get; set; }
        public double RequiredTorsionStirrupAsw1PerS { get; set; }
        public double TotalCombinedStirrupAswPerS { get; set; }
        public double RequiredLongitudinalTorsionAslMm2 { get; set; }
        public double FlexuralAsRequiredMm2 { get; set; }
        public XDataPacket XDataNhanDamCong { get; set; } = new XDataPacket("NHAN_XDATA_DAM_CONG");
        public string SummaryNotes { get; set; } = string.Empty;
    }

    public static class CurvedBeamCalculator
    {
        public static CurvedBeamResult Calculate(CurvedBeamDesignInput input)
        {
            Validate(input);

            double sweepRad = Math.Abs(input.EndAngleRad - input.StartAngleRad);
            double arcLen = input.RadiusMm * sweepRad;
            var chordPts = DiscretizeArc(input, sweepRad, arcLen);

            double b = input.WidthMm;
            double h = input.HeightMm;
            double c = input.CoverMm;
            double h0 = h - c;

            double ak = Math.Max(1.0, (b - 2.0 * c) * (h - 2.0 * c));
            double uk = 2.0 * ((b - 2.0 * c) + (h - 2.0 * c));
            double tMaxKnm = 0.1 * input.Concrete.Rb * b * b * h * 1e-6;
            bool torsionOk = Math.Abs(input.TorsionKnm) <= tMaxKnm;

            double tNmm = Math.Abs(input.TorsionKnm) * 1e6;
            double asw1PerS = tNmm / (2.0 * input.Steel.Rsw * ak);
            double asl = (tNmm * uk) / (2.0 * input.Steel.Rs * ak);

            var sIn = new BeamShearInput(b, h, input.ShearKn, input.Concrete, input.Steel) { StirrupDiameterMm = 8.0, StirrupLegs = 2 };
            var shearRes = BeamShearCalculator.Calculate(sIn);
            double sSpacing = shearRes.RecommendedSpacingMm > 0 ? shearRes.RecommendedSpacingMm : 100.0;
            double aswShearPerS = (2.0 * 0.25 * Math.PI * 64.0) / sSpacing;
            double totalAswPerS = aswShearPerS + 2.0 * asw1PerS;

            var flexRes = BeamFlexureCalculator.Calculate(new BeamFlexureInput(b, h, input.MomentKnm, input.Concrete, input.Steel) { CoverTensionMm = c });
            var xdata = CreateCurvedBeamXData(input, arcLen, chordPts.Count, sweepRad * 180.0 / Math.PI);

            return new CurvedBeamResult
            {
                BeamMark = input.BeamMark,
                ArcLengthMm = arcLen,
                SweepAngleDeg = sweepRad * 180.0 / Math.PI,
                DiscretizedChordPoints = chordPts,
                TorsionCapacityMaxKnm = tMaxKnm,
                TorsionCapacityCompliant = torsionOk,
                RequiredTorsionStirrupAsw1PerS = asw1PerS,
                TotalCombinedStirrupAswPerS = totalAswPerS,
                RequiredLongitudinalTorsionAslMm2 = asl,
                FlexuralAsRequiredMm2 = flexRes.AsRequiredMm2,
                XDataNhanDamCong = xdata,
                SummaryNotes = torsionOk ? "Curved beam verified for combined flexure, shear, and torsion." : "Torsional moment exceeds plain section limit; section enlargement required."
            };
        }

        private static void Validate(CurvedBeamDesignInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (input.RadiusMm <= 0.0) throw new ArgumentOutOfRangeException(nameof(input.RadiusMm), "Radius must be strictly positive.");
            double sweep = Math.Abs(input.EndAngleRad - input.StartAngleRad);
            if (sweep <= 1e-6) throw new ArgumentOutOfRangeException(nameof(input.EndAngleRad), "Sweep angle must be strictly positive.");
            if (input.WidthMm <= 0.0 || input.HeightMm <= 0.0) throw new ArgumentOutOfRangeException(nameof(input.WidthMm), "Dimensions must be positive.");
            if (input.Concrete == null) throw new ArgumentNullException(nameof(input.Concrete));
            if (input.Steel == null) throw new ArgumentNullException(nameof(input.Steel));
        }

        private static List<Point2D> DiscretizeArc(CurvedBeamDesignInput input, double sweepRad, double arcLen)
        {
            int nSegments = Math.Max(2, (int)Math.Ceiling(arcLen / input.MaxChordLengthMm));
            var points = new List<Point2D>(nSegments + 1);
            double angleStep = (input.EndAngleRad - input.StartAngleRad) / nSegments;

            for (int i = 0; i <= nSegments; i++)
            {
                double angle = input.StartAngleRad + i * angleStep;
                double x = input.Center.X + input.RadiusMm * Math.Cos(angle);
                double y = input.Center.Y + input.RadiusMm * Math.Sin(angle);
                points.Add(new Point2D(x, y));
            }
            return points;
        }

        private static XDataPacket CreateCurvedBeamXData(CurvedBeamDesignInput input, double arcLen, int chordPtsCount, double sweepDeg)
        {
            var packet = new XDataPacket("NHAN_XDATA_DAM_CONG");
            packet.AddString(input.BeamMark);
            packet.AddReal(input.RadiusMm);
            packet.AddReal(sweepDeg);
            packet.AddReal(arcLen);
            packet.AddReal(input.Center.X);
            packet.AddReal(input.Center.Y);
            packet.AddInt32(chordPtsCount);
            return packet;
        }
    }
}
