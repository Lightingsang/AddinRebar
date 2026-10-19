using System;
using System.Collections.Generic;
using HPDcePro.Core.Geometry;

namespace HPDcePro.Core.Etabs
{
    public sealed class EtabsModelSynchronizer
    {
        private static readonly double[] StandardBarDiameters = { 16.0, 18.0, 20.0, 22.0, 25.0 };

        public EtabsSyncReport Synchronize(
            EtabsExchangeModel etabsModel,
            IReadOnlyList<(string CadMark, Point3D P1, Point3D P2)> cadBeams,
            double toleranceMm = 200.0)
        {
            if (etabsModel == null) throw new ArgumentNullException(nameof(etabsModel));
            if (cadBeams == null) throw new ArgumentNullException(nameof(cadBeams));

            var report = new EtabsSyncReport();
            var matchedEtabs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            MatchCadBeams(report, matchedEtabs, cadBeams, etabsModel.Frames, toleranceMm);
            FindOrphanedEtabsFrames(report, matchedEtabs, etabsModel.Frames);
            return report;
        }

        private static void MatchCadBeams(
            EtabsSyncReport report,
            HashSet<string> matchedEtabs,
            IReadOnlyList<(string CadMark, Point3D P1, Point3D P2)> cadBeams,
            List<EtabsFrameMember> frames,
            double toleranceMm)
        {
            for (int i = 0; i < cadBeams.Count; i++)
            {
                var beam = cadBeams[i];
                var match = FindClosestFrame(beam.P1, beam.P2, frames, toleranceMm);
                if (match.Frame != null)
                {
                    matchedEtabs.Add(match.Frame.UniqueName);
                    report.MatchedCount++;
                    report.Items.Add(new EtabsSyncItem
                    {
                        CadMark = beam.CadMark,
                        EtabsUniqueName = match.Frame.UniqueName,
                        Status = EtabsSyncItemStatus.Matched,
                        SpatialDeviationMm = match.DeviationMm,
                        Message = $"Matched with ETABS frame {match.Frame.UniqueName} (dev: {match.DeviationMm:F1}mm)."
                    });
                }
                else
                {
                    report.OrphanCount++;
                    report.Items.Add(new EtabsSyncItem
                    {
                        CadMark = beam.CadMark,
                        Status = EtabsSyncItemStatus.OrphanedInCad,
                        SpatialDeviationMm = double.PositiveInfinity,
                        Message = $"CAD beam {beam.CadMark} has no matching ETABS frame within {toleranceMm}mm."
                    });
                    report.Warnings.Add($"CAD beam '{beam.CadMark}' not found in ETABS model.");
                }
            }
        }

        private static void FindOrphanedEtabsFrames(
            EtabsSyncReport report,
            HashSet<string> matchedEtabs,
            List<EtabsFrameMember> frames)
        {
            for (int j = 0; j < frames.Count; j++)
            {
                var f = frames[j];
                if (!matchedEtabs.Contains(f.UniqueName))
                {
                    report.OrphanCount++;
                    report.Items.Add(new EtabsSyncItem
                    {
                        EtabsUniqueName = f.UniqueName,
                        Status = EtabsSyncItemStatus.OrphanedInEtabs,
                        SpatialDeviationMm = double.PositiveInfinity,
                        Message = $"ETABS frame {f.UniqueName} has no corresponding beam on CAD drawing."
                    });
                }
            }
        }

        private static (EtabsFrameMember? Frame, double DeviationMm) FindClosestFrame(
            Point3D p1,
            Point3D p2,
            List<EtabsFrameMember> frames,
            double toleranceMm)
        {
            EtabsFrameMember? bestFrame = null;
            double bestDev = double.MaxValue;

            for (int i = 0; i < frames.Count; i++)
            {
                var f = frames[i];
                double dev1 = Math.Sqrt(Math.Pow(p1.X - f.Point1.X, 2) + Math.Pow(p1.Y - f.Point1.Y, 2) + Math.Pow(p1.Z - f.Point1.Z, 2)) +
                              Math.Sqrt(Math.Pow(p2.X - f.Point2.X, 2) + Math.Pow(p2.Y - f.Point2.Y, 2) + Math.Pow(p2.Z - f.Point2.Z, 2));

                double dev2 = Math.Sqrt(Math.Pow(p1.X - f.Point2.X, 2) + Math.Pow(p1.Y - f.Point2.Y, 2) + Math.Pow(p1.Z - f.Point2.Z, 2)) +
                              Math.Sqrt(Math.Pow(p2.X - f.Point1.X, 2) + Math.Pow(p2.Y - f.Point1.Y, 2) + Math.Pow(p2.Z - f.Point1.Z, 2));

                double dev = Math.Min(dev1, dev2) * 0.5;
                if (dev < bestDev && dev <= toleranceMm)
                {
                    bestDev = dev;
                    bestFrame = f;
                }
            }
            return (bestFrame, bestDev);
        }

        public (double MaxPositiveM, double MaxNegativeM, double MaxV) ExtractEnvelopeMoments(EtabsExchangeModel model, string uniqueName)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            double maxPosM = 0.0, maxNegM = 0.0, maxV = 0.0;

            for (int i = 0; i < model.Forces.Count; i++)
            {
                var force = model.Forces[i];
                if (string.Equals(force.UniqueName, uniqueName, StringComparison.OrdinalIgnoreCase))
                {
                    if (force.M3_Knm > maxPosM) maxPosM = force.M3_Knm;
                    if (force.M3_Knm < maxNegM) maxNegM = force.M3_Knm;
                    double vAbs = Math.Max(Math.Abs(force.V2_Kn), Math.Abs(force.V3_Kn));
                    if (vAbs > maxV) maxV = vAbs;
                }
            }
            return (maxPosM, Math.Abs(maxNegM), maxV);
        }

        public RebarArrangementResult ConvertRequiredAsToBarArrangement(double requiredAsMm2, int minBars = 2, int maxBars = 6)
        {
            if (requiredAsMm2 <= 0.0)
            {
                return new RebarArrangementResult { BarCount = 2, DiameterMm = 16.0, TotalAsMm2 = 402.12, ExcessAsMm2 = 402.12, Designation = "2D16" };
            }

            RebarArrangementResult? best = null;
            double minWaste = double.MaxValue;

            for (int d = 0; d < StandardBarDiameters.Length; d++)
            {
                double dia = StandardBarDiameters[d];
                double barAs = 0.25 * Math.PI * dia * dia;

                for (int count = minBars; count <= maxBars; count++)
                {
                    double totalAs = count * barAs;
                    if (totalAs >= requiredAsMm2)
                    {
                        double waste = totalAs - requiredAsMm2;
                        if (waste < minWaste)
                        {
                            minWaste = waste;
                            best = new RebarArrangementResult
                            {
                                BarCount = count,
                                DiameterMm = dia,
                                TotalAsMm2 = totalAs,
                                ExcessAsMm2 = waste,
                                Designation = $"{count}D{dia:F0}"
                            };
                        }
                    }
                }
            }

            return best ?? new RebarArrangementResult
            {
                BarCount = maxBars,
                DiameterMm = 25.0,
                TotalAsMm2 = maxBars * 0.25 * Math.PI * 625.0,
                ExcessAsMm2 = (maxBars * 0.25 * Math.PI * 625.0) - requiredAsMm2,
                Designation = $"{maxBars}D25"
            };
        }
    }
}
