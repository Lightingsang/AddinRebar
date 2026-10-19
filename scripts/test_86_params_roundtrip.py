import os
import sys

# Blueprint table specification of the 86 parameters
# Lines 152 to 237 of GoiLenh_HPKata.lsp
EXPECTED_86_LINES = [
    ";kata_dim",               # 152: 1  LayerDim
    ";kata_net thay",          # 153: 2  LayerNetThay
    ";kata_net khung",         # 154: 3  LayerNetKhung
    ";kata_net khuat",         # 155: 4  LayerNetKhuat
    ";kata_grid",              # 156: 5  LayerGrid
    ";kata_thep chu",          # 157: 6  LayerThepChu
    ";kata_thep dai",          # 158: 7  LayerThepDai
    ";kata_thep dai",          # 159: 8  LayerThepDaiPhu
    ";kata_net manh",          # 160: 9  LayerNetManh
    ";kata_text",              # 161: 10 LayerText
    ";kata_text",              # 162: 11 LayerTextTag
    ";a",                      # 163: 12 SpacingPrefix
    ";Ø",                      # 164: 13 SmallBarPrefix
    ";Ø",                      # 165: 14 DeformedBarPrefix
    ";kata_dim_iso",           # 166: 15 DimStyleName
    ";30",                     # 167: 16 ConcreteCoverMm
    ";135/180",                # 168: 17 DefaultHookAngle
    ";1/25",                   # 169: 18 DetailDrawingScale
    ";16",                     # 170: 19 BaseBarDiameter
    ";30",                     # 171: 20 LapLengthMultiplier
    ";1",                      # 172: 21 StaggeredLapMode
    ";1",                      # 173: 22 DrawFilletRadius
    ";1",                      # 174: 23 GroupSameRebar
    ";0",                      # 175: 24 ApplyBendDeduction
    ";1",                      # 176: 25 AutoRenumberMarks
    ";40",                     # 177: 26 TensionAnchorMult
    ";1/6",                    # 178: 27 ColSpliceRatio
    ";1",                      # 179: 28 DrawElevationHooks
    ";0",                      # 180: 29 DimStyleOverride
    ";0",                      # 181: 30 TagStyle
    ";Ø",                      # 182: 31 ScheduleDiaPrefix
    ";11700",                  # 183: 32 StandardBarLength
    ";100",                    # 184: 33 MinRemnantLength
    ";0",                      # 185: 34 CouplerMinDia
    ";0.2",                    # 186: 35 BeamTopCutoffL1
    "; mép",                   # 187: 36 BeamCutoffDatumL1
    ";0",                      # 188: 37 BeamBotCutoff
    ";0",                      # 189: 38 DenseZoneLayout
    ";0",                      # 190: 39 LengthRoundingMode
    ";1a, 1b, 1c, 1d ...",     # 191: 40 VarBarNaming
    ";1",                      # 192: 41 DrawShapeInSched
    ";5",                      # 193: 42 AnnotationTextH
    ";0",                      # 194: 43 DrawCenterlines
    ";-",                      # 195: 44 TagSeparator
    ";7.5",                    # 196: 45 ScheduleRowHeight
    ";25",                     # 197: 46 SchedShapeColWidth
    ";",                       # 198: 47 ReservedField47
    ";0.25",                   # 199: 48 BeamTopCutoffL2
    "; mép",                   # 200: 49 BeamCutoffDatumL2
    ";8800",                   # 201: 50 SecondaryBarLength
    ";Tiếng Việt",             # 202: 51 LanguageUiSched
    ";0",                      # 203: 52 SchedExportFormat
    ";0",                      # 204: 53 CouplerSpec
    ";",                       # 205: 54 ReservedField54
    ";Default",                # 206: 55 TableTemplate
    ";",                       # 207: 56 ReservedField56
    ";Closed",                 # 208: 57 StirrupClosureType
    ";0",                      # 209: 58 FootingHookMult
    ";0",                      # 210: 59 SlabHookMult
    ";0",                      # 211: 60 WallHookMult
    ";",                       # 212: 61 ReservedField61
    ";0",                      # 213: 62 TitleScaleMode
    ";",                       # 214: 63 ReservedField63
    ";0",                      # 215: 64 AutoDimRebar
    ";mm",                     # 216: 65 LengthUnitDisplay
    ";50",                     # 217: 66 CutLengthRoundStep
    ";0",                      # 218: 67 MassUnitDisplay
    ";0",                      # 219: 68 TotalLengthRound
    ";1",                      # 220: 69 DrawLeaderArrow
    ";0",                      # 221: 70 LeaderArrowType
    ";5f10a50",                # 222: 71 SecBeamStirrupAdd1
    ";1",                      # 223: 72 SecBeamGroup1
    ";2f14",                   # 224: 73 SecBeamAntiCrack1
    ";150",                    # 225: 74 SecBeamZoneLen1
    ";5f10a50",                # 226: 75 SecBeamStirrupAdd2
    ";1",                      # 227: 76 SecBeamGroup2
    ";2f14",                   # 228: 77 SecBeamAntiCrack2
    ";150",                    # 229: 78 SecBeamZoneLen2
    ";10",                     # 230: 79 TagCircleRadius
    ";50",                     # 231: 80 StaggeredOffsetMm
    ";0",                      # 232: 81 CouplerSepSchedule
    ";None",                   # 233: 82 CouplerEndTreat1
    ";None",                   # 234: 83 CouplerEndTreat2
    ";0",                      # 235: 84 PrintQrCodeOnTag
    ";0",                      # 236: 85 RealtimeSync
    ";0"                       # 237: 86 QsFormulaMode
]

def check_lsp_lines(lsp_path):
    with open(lsp_path, "r", encoding="utf-8") as f:
        content = f.read()

    lines = content.splitlines()
    actual_86 = lines[151:237]
    print(f"Comparing {len(actual_86)} lines from {lsp_path} against blueprint spec...")

    mismatches = []
    for i in range(86):
        line_num = 152 + i
        act = actual_86[i]
        exp = EXPECTED_86_LINES[i]
        if act != exp:
            mismatches.append((line_num, i+1, exp, act))

    if mismatches:
        print(f"FAIL: {len(mismatches)} mismatches found:")
        for line_num, param_idx, exp, act in mismatches:
            print(f"  Line {line_num} (Param #{param_idx}): expected '{exp}', got '{act}'")
        return False

    print("PASS: 100% of all 86 configuration lines match the Blueprint and KataConfigDefaults!")
    return True

if __name__ == "__main__":
    lsp_file = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.AutoCad\GoiLenh_HPKata.lsp"
    ok = check_lsp_lines(lsp_file)
    sys.exit(0 if ok else 1)
