using System;
using System.Collections.Generic;
using System.Globalization;

namespace HPKata.Core.Configuration
{
    public static partial class KataConfigParser
    {
        public static List<string> SerializeToLines(KataConfig c)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            var l = new List<string>(86);
            SerializeLines1To30(c, l);
            SerializeLines31To60(c, l);
            SerializeLines61To86(c, l);
            return l;
        }

        public static string SerializeToText(KataConfig c)
        {
            var lines = SerializeToLines(c);
            return string.Join(Environment.NewLine, lines);
        }

        public static void SaveToFile(string filePath, KataConfig config)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path cannot be null or whitespace.", nameof(filePath));
            }
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var configLines = SerializeToLines(config);
            if (System.IO.File.Exists(filePath))
            {
                string[] existing = System.IO.File.ReadAllLines(filePath);
                if (existing.Length >= 237)
                {
                    for (int i = 0; i < 86 && (151 + i) < existing.Length; i++)
                    {
                        existing[151 + i] = configLines[i];
                    }
                    System.IO.File.WriteAllLines(filePath, existing);
                    return;
                }
            }

            string? dir = System.IO.Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }
            System.IO.File.WriteAllLines(filePath, configLines);
        }

        private static void SerializeLines1To30(KataConfig c, List<string> l)
        {
            l.Add($";{c.LayerDim}");
            l.Add($";{c.LayerNetThay}");
            l.Add($";{c.LayerNetKhung}");
            l.Add($";{c.LayerNetKhuat}");
            l.Add($";{c.LayerGrid}");
            l.Add($";{c.LayerThepChu}");
            l.Add($";{c.LayerThepDai}");
            l.Add($";{c.LayerThepDaiPhu}");
            l.Add($";{c.LayerNetManh}");
            l.Add($";{c.LayerText}");
            l.Add($";{c.LayerTextTag}");
            l.Add($";{c.SpacingPrefix}");
            l.Add($";{c.SmallBarPrefix}");
            l.Add($";{c.DeformedBarPrefix}");
            l.Add($";{c.DimStyleName}");
            l.Add($";{c.ConcreteCoverMm.ToString(CultureInfo.InvariantCulture)}");
            l.Add($";{c.DefaultHookAngle}");
            l.Add($";{FormatFraction(c.DetailDrawingScale)}");
            l.Add($";{c.BaseBarDiameter}");
            l.Add($";{c.LapLengthMultiplier.ToString(CultureInfo.InvariantCulture)}");
            l.Add($";{c.StaggeredLapMode}");
            l.Add($";{c.DrawFilletRadius}");
            l.Add($";{c.GroupSameRebar}");
            l.Add($";{c.ApplyBendDeduction}");
            l.Add($";{c.AutoRenumberMarks}");
            l.Add($";{c.TensionAnchorMult.ToString(CultureInfo.InvariantCulture)}");
            l.Add($";{FormatFraction(c.ColSpliceRatio)}");
            l.Add($";{c.DrawElevationHooks}");
            l.Add($";{c.DimStyleOverride}");
            l.Add($";{c.TagStyle}");
        }

        private static void SerializeLines31To60(KataConfig c, List<string> l)
        {
            l.Add($";{c.ScheduleDiaPrefix}");
            l.Add($";{c.StandardBarLength}");
            l.Add($";{c.MinRemnantLength}");
            l.Add($";{c.CouplerMinDia}");
            l.Add($";{c.BeamTopCutoffL1.ToString(CultureInfo.InvariantCulture)}");
            l.Add($";{c.BeamCutoffDatumL1}");
            l.Add($";{c.BeamBotCutoff.ToString(CultureInfo.InvariantCulture)}");
            l.Add($";{c.DenseZoneLayout}");
            l.Add($";{c.LengthRoundingMode}");
            l.Add($";{c.VarBarNaming}");
            l.Add($";{c.DrawShapeInSched}");
            l.Add($";{c.AnnotationTextH.ToString(CultureInfo.InvariantCulture)}");
            l.Add($";{c.DrawCenterlines}");
            l.Add($";{c.TagSeparator}");
            l.Add($";{c.ScheduleRowHeight.ToString(CultureInfo.InvariantCulture)}");
            l.Add($";{c.SchedShapeColWidth.ToString(CultureInfo.InvariantCulture)}");
            l.Add($";{c.ReservedField47}");
            l.Add($";{c.BeamTopCutoffL2.ToString(CultureInfo.InvariantCulture)}");
            l.Add($";{c.BeamCutoffDatumL2}");
            l.Add($";{c.SecondaryBarLength}");
            l.Add($";{c.LanguageUiSched}");
            l.Add($";{c.SchedExportFormat}");
            l.Add($";{c.CouplerSpec}");
            l.Add($";{c.ReservedField54}");
            l.Add($";{c.TableTemplate}");
            l.Add($";{c.ReservedField56}");
            l.Add($";{c.StirrupClosureType}");
            l.Add($";{c.FootingHookMult.ToString(CultureInfo.InvariantCulture)}");
            l.Add($";{c.SlabHookMult.ToString(CultureInfo.InvariantCulture)}");
            l.Add($";{c.WallHookMult.ToString(CultureInfo.InvariantCulture)}");
        }

        private static void SerializeLines61To86(KataConfig c, List<string> l)
        {
            l.Add($";{c.ReservedField61}");
            l.Add($";{c.TitleScaleMode}");
            l.Add($";{c.ReservedField63}");
            l.Add($";{c.AutoDimRebar}");
            l.Add($";{c.LengthUnitDisplay}");
            l.Add($";{c.CutLengthRoundStep.ToString(CultureInfo.InvariantCulture)}");
            l.Add($";{c.MassUnitDisplay}");
            l.Add($";{c.TotalLengthRound}");
            l.Add($";{c.DrawLeaderArrow}");
            l.Add($";{c.LeaderArrowType}");
            l.Add($";{c.SecBeamStirrupAdd1}");
            l.Add($";{c.SecBeamGroup1}");
            l.Add($";{c.SecBeamAntiCrack1}");
            l.Add($";{c.SecBeamZoneLen1.ToString(CultureInfo.InvariantCulture)}");
            l.Add($";{c.SecBeamStirrupAdd2}");
            l.Add($";{c.SecBeamGroup2}");
            l.Add($";{c.SecBeamAntiCrack2}");
            l.Add($";{c.SecBeamZoneLen2.ToString(CultureInfo.InvariantCulture)}");
            l.Add($";{c.TagCircleRadius.ToString(CultureInfo.InvariantCulture)}");
            l.Add($";{c.StaggeredOffsetMm.ToString(CultureInfo.InvariantCulture)}");
            l.Add($";{c.CouplerSepSchedule}");
            l.Add($";{c.CouplerEndTreat1}");
            l.Add($";{c.CouplerEndTreat2}");
            l.Add($";{c.PrintQrCodeOnTag}");
            l.Add($";{c.RealtimeSync}");
            l.Add($";{c.QsFormulaMode}");
        }
    }
}
