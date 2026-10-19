using System;
using System.Collections.Generic;
using HPDcePro.Core.Calculations.Beam;

namespace HPDcePro.Core.Calculations.Column
{
    public enum ColumnSectionShape
    {
        Rectangular,
        Circular
    }

    public sealed class ColumnBarPlacement
    {
        public double X { get; }
        public double Y { get; }
        public int Diameter { get; }
        public double AreaMm2 => Math.PI * Diameter * Diameter / 4.0;

        public ColumnBarPlacement(double x, double y, int diameter)
        {
            X = x;
            Y = y;
            Diameter = diameter > 0 ? diameter : throw new ArgumentException("Diameter must be positive.", nameof(diameter));
        }
    }

    public sealed class PmmPoint3D
    {
        public double AxialKn { get; }
        public double MomentXkNm { get; }
        public double MomentYkNm { get; }

        public PmmPoint3D(double axialKn, double momentXkNm, double momentYkNm)
        {
            AxialKn = axialKn;
            MomentXkNm = momentXkNm;
            MomentYkNm = momentYkNm;
        }
    }

    public sealed class ColumnPmmInput
    {
        public string ColumnMark { get; }
        public ColumnSectionShape SectionShape { get; }
        public double WidthBMm { get; }
        public double HeightHMm { get; }
        public double EffectiveLengthL0Mm { get; }
        public ConcreteProperties Concrete { get; }
        public RebarProperties Rebar { get; }
        public IReadOnlyList<ColumnBarPlacement> Bars { get; }
        public double DesignAxialNKn { get; }
        public double DesignMomentMxKnm { get; }
        public double DesignMomentMyKnm { get; }
        public double LongTermFactorPhiL { get; }

        public ColumnPmmInput(
            string columnMark,
            ColumnSectionShape sectionShape,
            double widthBMm,
            double heightHMm,
            double effectiveLengthL0Mm,
            ConcreteProperties concrete,
            RebarProperties rebar,
            IEnumerable<ColumnBarPlacement> bars,
            double designAxialNKn,
            double designMomentMxKnm,
            double designMomentMyKnm,
            double longTermFactorPhiL = 1.0)
        {
            ColumnMark = string.IsNullOrWhiteSpace(columnMark) ? "C1" : columnMark;
            SectionShape = sectionShape;
            WidthBMm = widthBMm > 0 ? widthBMm : throw new ArgumentException("Width must be positive.", nameof(widthBMm));
            HeightHMm = heightHMm > 0 ? heightHMm : throw new ArgumentException("Height must be positive.", nameof(heightHMm));
            EffectiveLengthL0Mm = effectiveLengthL0Mm > 0 ? effectiveLengthL0Mm : 3000.0;
            Concrete = concrete ?? throw new ArgumentNullException(nameof(concrete));
            Rebar = rebar ?? throw new ArgumentNullException(nameof(rebar));
            Bars = new List<ColumnBarPlacement>(bars ?? Array.Empty<ColumnBarPlacement>());
            DesignAxialNKn = designAxialNKn;
            DesignMomentMxKnm = designMomentMxKnm;
            DesignMomentMyKnm = designMomentMyKnm;
            LongTermFactorPhiL = longTermFactorPhiL > 0 ? longTermFactorPhiL : 1.0;
        }
    }

    public sealed class ColumnPmmResult
    {
        public string ColumnMark { get; }
        public double DesignAxialNKn { get; }
        public double DesignMomentMxKnm { get; }
        public double DesignMomentMyKnm { get; }
        public double SlendernessEtaX { get; }
        public double SlendernessEtaY { get; }
        public double MagnifiedMomentMxdKnm { get; }
        public double MagnifiedMomentMydKnm { get; }
        public double AxialCapacityN0Kn { get; }
        public double MomentCapacityMx0Knm { get; }
        public double MomentCapacityMy0Knm { get; }
        public double UtilizationRatio { get; }
        public bool IsSafe { get; }
        public IReadOnlyList<PmmPoint3D> InteractionCurve { get; }
        public string StatusSummary { get; }

        public ColumnPmmResult(
            string columnMark,
            double designAxialNKn,
            double designMomentMxKnm,
            double designMomentMyKnm,
            double slendernessEtaX,
            double slendernessEtaY,
            double magnifiedMomentMxdKnm,
            double magnifiedMomentMydKnm,
            double axialCapacityN0Kn,
            double momentCapacityMx0Knm,
            double momentCapacityMy0Knm,
            double utilizationRatio,
            bool isSafe,
            IEnumerable<PmmPoint3D> interactionCurve,
            string statusSummary)
        {
            ColumnMark = columnMark;
            DesignAxialNKn = designAxialNKn;
            DesignMomentMxKnm = designMomentMxKnm;
            DesignMomentMyKnm = designMomentMyKnm;
            SlendernessEtaX = slendernessEtaX;
            SlendernessEtaY = slendernessEtaY;
            MagnifiedMomentMxdKnm = magnifiedMomentMxdKnm;
            MagnifiedMomentMydKnm = magnifiedMomentMydKnm;
            AxialCapacityN0Kn = axialCapacityN0Kn;
            MomentCapacityMx0Knm = momentCapacityMx0Knm;
            MomentCapacityMy0Knm = momentCapacityMy0Knm;
            UtilizationRatio = utilizationRatio;
            IsSafe = isSafe;
            InteractionCurve = new List<PmmPoint3D>(interactionCurve ?? Array.Empty<PmmPoint3D>());
            StatusSummary = statusSummary;
        }
    }
}
