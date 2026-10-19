using System;
using HPKata.Core.Foundations.Models;

namespace HPKata.Core.Foundations.Calculators
{
    /// <summary>
    /// Computes concrete volumes, formwork contact areas, and lean concrete quantities
    /// for isolated spread footings and pile caps per TCVN standards.
    /// </summary>
    public static class FootingGeometryCalculator
    {
        public static double CalculateFootingVolume(IsolatedFootingModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            model.Validate();

            double v1Mm3 = model.LengthMm * model.WidthMm * model.BaseSlabHeightMm;
            double h2 = model.PedestalHeightMm;
            double v2Mm3 = 0.0;

            if (h2 > 0)
            {
                if (model.PedestalStyle == PedestalType.Sloped)
                {
                    double l = model.LengthMm;
                    double b = model.WidthMm;
                    double lc = model.PedestalLengthMm;
                    double bc = model.PedestalWidthMm;
                    v2Mm3 = (h2 / 6.0) * (l * b + lc * bc + (l + lc) * (b + bc));
                }
                else
                {
                    v2Mm3 = model.PedestalLengthMm * model.PedestalWidthMm * h2;
                }
            }

            return (v1Mm3 + v2Mm3) / 1e9 * model.MemberCount;
        }

        public static double CalculateFootingFormwork(IsolatedFootingModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            model.Validate();

            double a1Mm2 = 2.0 * (model.LengthMm + model.WidthMm) * model.BaseSlabHeightMm;
            double h2 = model.PedestalHeightMm;
            double a2Mm2 = 0.0;

            if (h2 > 0)
            {
                if (model.PedestalStyle == PedestalType.Sloped)
                {
                    double l = model.LengthMm;
                    double b = model.WidthMm;
                    double lc = model.PedestalLengthMm;
                    double bc = model.PedestalWidthMm;
                    double slantY = Math.Sqrt(h2 * h2 + Math.Pow((b - bc) / 2.0, 2));
                    double slantX = Math.Sqrt(h2 * h2 + Math.Pow((l - lc) / 2.0, 2));
                    a2Mm2 = (l + lc) * slantY + (b + bc) * slantX;
                }
                else
                {
                    a2Mm2 = 2.0 * (model.PedestalLengthMm + model.PedestalWidthMm) * h2;
                }
            }

            return (a1Mm2 + a2Mm2) / 1e6 * model.MemberCount;
        }

        public static double CalculateLeanConcreteVolume(IsolatedFootingModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            double lLean = model.LengthMm + 2.0 * model.LeanConcreteOverhangMm;
            double bLean = model.WidthMm + 2.0 * model.LeanConcreteOverhangMm;
            return (lLean * bLean * model.LeanConcreteThicknessMm) / 1e9 * model.MemberCount;
        }

        public static double CalculatePileCapVolume(PileCapModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            model.Validate();

            double baseHeight = model.BaseTierHeightMm;
            double grossVolMm3 = model.LengthMm * model.WidthMm * baseHeight;

            if (model.IsStepped && model.Tier2HeightMm > 0)
            {
                grossVolMm3 += model.Tier2LengthMm * model.Tier2WidthMm * model.Tier2HeightMm;
            }

            double pileDeductionMm3 = 0.0;
            foreach (var p in model.Piles)
            {
                double emb = p.EmbedmentDepthMm > 0 ? p.EmbedmentDepthMm : model.PileEmbedmentDepthMm;
                pileDeductionMm3 += p.AreaMm2 * emb;
            }

            double netMm3 = Math.Max(0.0, grossVolMm3 - pileDeductionMm3);
            return netMm3 / 1e9 * model.MemberCount;
        }

        public static double CalculatePileCapFormwork(PileCapModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            model.Validate();

            double baseHeight = model.BaseTierHeightMm;
            double areaMm2 = 2.0 * (model.LengthMm + model.WidthMm) * baseHeight;

            if (model.IsStepped && model.Tier2HeightMm > 0)
            {
                areaMm2 += 2.0 * (model.Tier2LengthMm + model.Tier2WidthMm) * model.Tier2HeightMm;
            }

            return areaMm2 / 1e6 * model.MemberCount;
        }
    }
}
