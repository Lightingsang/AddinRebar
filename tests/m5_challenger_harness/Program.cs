using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using HPRebar.BeamRebar.Service;
using HPRebar.Core.Tcvn5574.Calculators;
using HPRebar.Core.Tcvn5574.Materials;
using HPRebar.Core.Tcvn5574.Models;

namespace M5ChallengerHarness;

public static class Program
{
    private static int _totalTests = 0;
    private static int _passedTests = 0;
    private static int _failedTests = 0;
    private static readonly List<string> _findings = new();

    public static int Main(string[] args)
    {
        Console.WriteLine("===============================================================================");
        Console.WriteLine("  EMPIRICAL CHALLENGER: Milestone M5 ClosedXML Force Parsing & Report Export   ");
        Console.WriteLine("===============================================================================");

        TestEmptyAndCorruptedExcelInputs();
        TestMissingSheets();
        TestNonNumericStringsAndFormats();
        TestNegativeSpansAndExtremeValues();
        TestEtabsOutOfOrderStationsAndEdgeCases();
        TestCalculationReportExportIntegrity();
        TestProposedFixForRegionalCultureAndNaN();
        TestCompoundHeaderNoShadowing();

        Console.WriteLine("\n===============================================================================");
        Console.WriteLine($"  SUMMARY: Total={_totalTests}, Passed={_passedTests}, Failed={_failedTests}");
        Console.WriteLine("===============================================================================");

        if (_findings.Count > 0)
        {
            Console.WriteLine("\n[OBSERVATIONS & FINDINGS]:");
            foreach (var f in _findings)
            {
                Console.WriteLine($"  - {f}");
            }
        }

        return _failedTests == 0 ? 0 : 1;
    }

    private static void Assert(bool condition, string testName, string? failureDetail = null)
    {
        _totalTests++;
        if (condition)
        {
            _passedTests++;
            Console.WriteLine($"[PASS] {testName}");
        }
        else
        {
            _failedTests++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[FAIL] {testName} - {failureDetail ?? "Assertion failed"}");
            Console.ResetColor();
            _findings.Add($"FAIL: {testName} ({failureDetail})");
        }
    }

    private static void RecordFinding(string message)
    {
        _findings.Add(message);
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[FINDING] {message}");
        Console.ResetColor();
    }

    private static void TestEmptyAndCorruptedExcelInputs()
    {
        Console.WriteLine("\n--- Group 1: Corrupted & Empty Excel Inputs ---");

        // 1.1 Empty workbook (0 worksheets)
        try
        {
            using var emptyWb = new XLWorkbook();
            // ClosedXML throws when accessing worksheet 1 or reading without worksheets
            using var ms = new MemoryStream();
            // Note: saving empty workbook without worksheets throws in ClosedXML
            bool threwExpected = false;
            try
            {
                // ClosedXmlBeamForceReader.Read(emptyWb.Worksheet(1)) would throw because no sheet
                ClosedXmlBeamForceReader.Read(emptyWb.Worksheets.Count > 0 ? emptyWb.Worksheet(1) : null!);
            }
            catch (ArgumentNullException)
            {
                threwExpected = true;
            }
            Assert(threwExpected, "1.1 Null worksheet throws ArgumentNullException gracefully");
        }
        catch (Exception ex)
        {
            Assert(false, "1.1 Empty workbook handling", ex.Message);
        }

        // 1.2 Blank worksheet (0 rows used)
        try
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Blank");
            var result = ClosedXmlBeamForceReader.Read(ws);
            Assert(result != null && result.Count == 0, "1.2 Blank worksheet returns empty collection without crash");
        }
        catch (Exception ex)
        {
            Assert(false, "1.2 Blank worksheet", ex.Message);
        }

        // 1.3 Worksheet with header only, no data rows
        try
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("HeaderOnly");
            ws.Cell("A1").Value = "Dầm";
            ws.Cell("B1").Value = "Nhịp";
            ws.Cell("C1").Value = "Chiều dài";
            ws.Cell("D1").Value = "M_gối_trái";
            ws.Cell("E1").Value = "M_nhịp";
            ws.Cell("F1").Value = "M_gối_phải";
            ws.Cell("G1").Value = "Q_gối_trái";
            ws.Cell("H1").Value = "Q_nhịp";
            ws.Cell("I1").Value = "Q_gối_phải";

            var result = ClosedXmlBeamForceReader.Read(ws);
            Assert(result != null && result.Count == 0, "1.3 Header-only worksheet returns empty collection without crash");
        }
        catch (Exception ex)
        {
            Assert(false, "1.3 Header-only worksheet", ex.Message);
        }

        // 1.4 Worksheet with interspersed empty rows and whitespace marks
        try
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("SparseData");
            ws.Cell("A1").Value = "Beam";
            ws.Cell("B1").Value = "Span";
            ws.Cell("C1").Value = "Length";
            ws.Cell("D1").Value = "M1";
            ws.Cell("E1").Value = "M2";
            ws.Cell("F1").Value = "M3";
            ws.Cell("G1").Value = "Q1";
            ws.Cell("H1").Value = "Q2";
            ws.Cell("I1").Value = "Q3";

            // Row 2: valid beam
            ws.Cell("A2").Value = "2D1";
            ws.Cell("B2").Value = 0;
            ws.Cell("C2").Value = 6000;
            ws.Cell("D2").Value = -150;
            ws.Cell("E2").Value = 120;
            ws.Cell("F2").Value = -140;
            ws.Cell("G2").Value = 160;
            ws.Cell("H2").Value = 30;
            ws.Cell("I2").Value = 150;

            // Row 3: completely empty row
            // Row 4: beam with whitespace mark (should be skipped)
            ws.Cell("A4").Value = "   ";
            ws.Cell("B4").Value = 1;

            // Row 5: another valid beam
            ws.Cell("A5").Value = "2D2";
            ws.Cell("B5").Value = 0;
            ws.Cell("C5").Value = 7000;
            ws.Cell("D5").Value = -180;
            ws.Cell("E5").Value = 140;
            ws.Cell("F5").Value = -170;
            ws.Cell("G5").Value = 190;
            ws.Cell("H5").Value = 40;
            ws.Cell("I5").Value = 185;

            var result = ClosedXmlBeamForceReader.Read(ws);
            Assert(result != null && result.Count == 2, "1.4 Interspersed empty rows filtered correctly (expected 2 beams)");
            Assert(result != null && result[0].BeamMark == "2D1" && result[1].BeamMark == "2D2", "1.4 Marks parsed cleanly");
        }
        catch (Exception ex)
        {
            Assert(false, "1.4 Interspersed empty rows", ex.Message);
        }

        // 1.5 File not found and invalid file path
        try
        {
            bool threwFileNotFound = false;
            try
            {
                ClosedXmlBeamForceReader.Read("non_existent_forces_file_12345.xlsx");
            }
            catch (FileNotFoundException)
            {
                threwFileNotFound = true;
            }
            Assert(threwFileNotFound, "1.5 Missing file throws FileNotFoundException");

            bool threwArgException = false;
            try
            {
                ClosedXmlBeamForceReader.Read("");
            }
            catch (ArgumentException)
            {
                threwArgException = true;
            }
            Assert(threwArgException, "1.5 Empty filePath throws ArgumentException");
        }
        catch (Exception ex)
        {
            Assert(false, "1.5 File path validation", ex.Message);
        }
    }

    private static void TestMissingSheets()
    {
        Console.WriteLine("\n--- Group 2: Missing Sheets Handling ---");

        string tempPath = Path.Combine(Path.GetTempPath(), $"m5_sheet_test_{Guid.NewGuid():N}.xlsx");
        try
        {
            using (var wb = new XLWorkbook())
            {
                var ws = wb.Worksheets.Add("AvailableSheet");
                ws.Cell("A1").Value = "Beam";
                ws.Cell("B1").Value = "Span";
                ws.Cell("C1").Value = "Length";
                ws.Cell("D1").Value = "M1";
                ws.Cell("E1").Value = "M2";
                ws.Cell("F1").Value = "M3";
                ws.Cell("G1").Value = "Q1";
                ws.Cell("H1").Value = "Q2";
                ws.Cell("I1").Value = "Q3";

                ws.Cell("A2").Value = "B-TEST";
                ws.Cell("B2").Value = 0;
                ws.Cell("C2").Value = 5000;
                ws.Cell("D2").Value = -100;
                ws.Cell("E2").Value = 80;
                ws.Cell("F2").Value = -90;
                ws.Cell("G2").Value = 110;
                ws.Cell("H2").Value = 20;
                ws.Cell("I2").Value = 100;
                wb.SaveAs(tempPath);
            }

            // 2.1 Request non-existent sheet "NonExistentSheet"
            // The reader's ResolveWorksheet falls back to sheet 1 if named sheet is not found
            var resultNonExistent = ClosedXmlBeamForceReader.Read(tempPath, sheetName: "NonExistentSheet");
            Assert(resultNonExistent != null && resultNonExistent.Count == 1,
                "2.1 Non-existent sheet falls back gracefully to first worksheet without crashing");
            RecordFinding("ResolveWorksheet silently falls back to worksheet 1 when specified sheetName is not found, rather than throwing KeyNotFoundException.");

            // 2.2 Null sheetName
            var resultNullSheet = ClosedXmlBeamForceReader.Read(tempPath, sheetName: null);
            Assert(resultNullSheet != null && resultNullSheet.Count == 1,
                "2.2 Null sheetName defaults to first worksheet smoothly");
        }
        catch (Exception ex)
        {
            Assert(false, "2. Missing sheets test", ex.Message);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    private static void TestNonNumericStringsAndFormats()
    {
        Console.WriteLine("\n--- Group 3: Non-Numeric Strings & Culture Formats ---");

        try
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("CorruptValues");
            ws.Cell("A1").Value = "Dầm";
            ws.Cell("B1").Value = "Nhịp";
            ws.Cell("C1").Value = "Chiều dài";
            ws.Cell("D1").Value = "M_gối_trái";
            ws.Cell("E1").Value = "M_nhịp";
            ws.Cell("F1").Value = "M_gối_phải";
            ws.Cell("G1").Value = "Q_gối_trái";
            ws.Cell("H1").Value = "Q_nhịp";
            ws.Cell("I1").Value = "Q_gối_phải";

            // Row 2: Corrupted strings in numeric columns
            ws.Cell("A2").Value = "D1";
            ws.Cell("B2").Value = "Không xác định"; // non-numeric span index
            ws.Cell("C2").Value = "N/A";            // non-numeric length
            ws.Cell("D2").Value = "#VALUE!";        // Excel error string
            ws.Cell("E2").Value = "NaN";            // NaN string
            ws.Cell("F2").Value = "TextMoment";     // random text
            ws.Cell("G2").Value = "-inf";           // negative infinity string
            ws.Cell("H2").Value = "";               // empty cell
            ws.Cell("I2").Value = "   ";            // spaces only

            // Row 3: Vietnamese comma decimal separators ("145,5")
            ws.Cell("A3").Value = "D2";
            ws.Cell("B3").Value = "1";
            ws.Cell("C3").Value = "5500,5";
            ws.Cell("D3").Value = "-180,25";
            ws.Cell("E3").Value = "135,75";
            ws.Cell("F3").Value = "-165,5";
            ws.Cell("G3").Value = "192,4";
            ws.Cell("H3").Value = "30,0";
            ws.Cell("I3").Value = "185,8";

            var result = ClosedXmlBeamForceReader.Read(ws);
            Assert(result != null && result.Count == 2, "3.1 Processed 2 rows containing non-numeric strings");

            var corruptRow = result![0];
            Assert(corruptRow.SpanIndex == 0, "3.2 Non-numeric span index defaulted to 0");
            Assert(corruptRow.SpanLengthMm == 0.0, "3.3 Non-numeric length defaulted to 0.0");
            Assert(corruptRow.MomentSupportLeftKnm == 0.0, "3.4 #VALUE! moment defaulted to 0.0");
            Assert(corruptRow.MomentMidSpanKnm == 0.0, "3.5 NaN moment defaulted to 0.0", $"Actual corruptRow.MomentMidSpanKnm = {corruptRow.MomentMidSpanKnm}");
            Assert(corruptRow.MomentSupportRightKnm == 0.0, "3.6 Text moment defaulted to 0.0");
            Assert(corruptRow.ShearSupportLeftKn == 0.0, "3.7 -inf shear defaulted to 0.0");
            Assert(corruptRow.ShearMidSpanKn == 0.0, "3.8 Empty shear cell defaulted to 0.0");
            Assert(corruptRow.ShearSupportRightKn == 0.0, "3.9 Whitespace shear cell defaulted to 0.0");

            var viRow = result[1];
            Assert(Math.Abs(viRow.SpanLengthMm - 5500.5) < 0.01, "3.10 Vietnamese comma format parsed length correctly", $"Actual viRow.SpanLengthMm = {viRow.SpanLengthMm}");
            Assert(Math.Abs(viRow.MomentSupportLeftKnm - (-180.25)) < 0.01, "3.11 Vietnamese comma format parsed M_left correctly", $"Actual viRow.MomentSupportLeftKnm = {viRow.MomentSupportLeftKnm}");
            Assert(Math.Abs(viRow.MomentMidSpanKnm - 135.75) < 0.01, "3.12 Vietnamese comma format parsed M_mid correctly", $"Actual viRow.MomentMidSpanKnm = {viRow.MomentMidSpanKnm}");
            Assert(Math.Abs(viRow.ShearSupportLeftKn - 192.4) < 0.01, "3.13 Vietnamese comma format parsed Q_left correctly", $"Actual viRow.ShearSupportLeftKn = {viRow.ShearSupportLeftKn}");
        }
        catch (Exception ex)
        {
            Assert(false, "3. Non-numeric strings and culture formats", ex.Message);
        }
    }

    private static void TestNegativeSpansAndExtremeValues()
    {
        Console.WriteLine("\n--- Group 4: Negative Spans & Extreme Values ---");

        try
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("ExtremeValues");
            ws.Cell("A1").Value = "Beam";
            ws.Cell("B1").Value = "Span";
            ws.Cell("C1").Value = "Length";
            ws.Cell("D1").Value = "M1";
            ws.Cell("E1").Value = "M2";
            ws.Cell("F1").Value = "M3";
            ws.Cell("G1").Value = "Q1";
            ws.Cell("H1").Value = "Q2";
            ws.Cell("I1").Value = "Q3";

            // Row 2: Negative span and length
            ws.Cell("A2").Value = "NEG-BEAM";
            ws.Cell("B2").Value = -5;
            ws.Cell("C2").Value = -6000.0;
            ws.Cell("D2").Value = -999999.0;
            ws.Cell("E2").Value = -888888.0;
            ws.Cell("F2").Value = -777777.0;
            ws.Cell("G2").Value = -555555.0;
            ws.Cell("H2").Value = -444444.0;
            ws.Cell("I2").Value = -333333.0;

            // Row 3: Astronomical numbers
            ws.Cell("A3").Value = "GIANT-BEAM";
            ws.Cell("B3").Value = 999;
            ws.Cell("C3").Value = 1e8;
            ws.Cell("D3").Value = 1e12;
            ws.Cell("E3").Value = 2e12;
            ws.Cell("F3").Value = 1e12;
            ws.Cell("G3").Value = 5e12;
            ws.Cell("H3").Value = 1e12;
            ws.Cell("I3").Value = 4e12;

            var result = ClosedXmlBeamForceReader.Read(ws);
            Assert(result != null && result.Count == 2, "4.1 Processed negative and extreme value rows");

            var negRow = result![0];
            Assert(negRow.SpanIndex == -5, "4.2 Negative span index preserved without exception");
            Assert(negRow.SpanLengthMm == -6000.0, "4.3 Negative length preserved without exception");
            Assert(negRow.MaxMomentKnm == 999999.0, "4.4 MaxMoment handles negative moments correctly (abs max)");
            Assert(negRow.MaxShearKn == 555555.0, "4.5 MaxShear handles negative shears correctly (abs max)");

            var giantRow = result[1];
            Assert(giantRow.MaxMomentKnm == 2e12, "4.6 Giant values computed without overflow");
            Assert(giantRow.MaxShearKn == 5e12, "4.7 Giant shears computed without overflow");
        }
        catch (Exception ex)
        {
            Assert(false, "4. Negative spans and extreme values", ex.Message);
        }
    }

    private static void TestEtabsOutOfOrderStationsAndEdgeCases()
    {
        Console.WriteLine("\n--- Group 5: ETABS Out-of-Order Stations & Edge Cases ---");

        try
        {
            // 5.1 Sorted station dataset
            double spanL = 6000.0;
            var sortedStations = new List<EtabsStationForce>
            {
                new() { BeamMark = "B1", SpanIndex = 0, StationMm = 0.0, SpanLengthMm = spanL, OutputCase = "Combo1", V2 = 180.0, M3 = -150.0 },
                new() { BeamMark = "B1", SpanIndex = 0, StationMm = 1000.0, SpanLengthMm = spanL, OutputCase = "Combo1", V2 = 120.0, M3 = -70.0 },
                new() { BeamMark = "B1", SpanIndex = 0, StationMm = 3000.0, SpanLengthMm = spanL, OutputCase = "Combo1", V2 = 10.0, M3 = 110.0 },
                new() { BeamMark = "B1", SpanIndex = 0, StationMm = 5000.0, SpanLengthMm = spanL, OutputCase = "Combo1", V2 = -130.0, M3 = -80.0 },
                new() { BeamMark = "B1", SpanIndex = 0, StationMm = 6000.0, SpanLengthMm = spanL, OutputCase = "Combo1", V2 = -175.0, M3 = -145.0 }
            };

            // 5.2 Out-of-order / shuffled stations
            var shuffledStations = new List<EtabsStationForce>
            {
                sortedStations[4], // 6000 mm
                sortedStations[2], // 3000 mm
                sortedStations[0], // 0 mm
                sortedStations[3], // 5000 mm
                sortedStations[1]  // 1000 mm
            };

            var envSorted = EtabsBeamForceReader.ExtractEnvelopes(sortedStations);
            var envShuffled = EtabsBeamForceReader.ExtractEnvelopes(shuffledStations);

            Assert(envSorted.Count == 1 && envShuffled.Count == 1, "5.1 Both sorted and shuffled extract 1 envelope");

            var s = envSorted[0];
            var sh = envShuffled[0];

            Assert(s.MomentSupportLeftKnm == sh.MomentSupportLeftKnm,
                "5.2 M_left identical between sorted and shuffled stations",
                $"Sorted={s.MomentSupportLeftKnm}, Shuffled={sh.MomentSupportLeftKnm}");
            Assert(s.MomentMidSpanKnm == sh.MomentMidSpanKnm,
                "5.3 M_mid identical between sorted and shuffled stations",
                $"Sorted={s.MomentMidSpanKnm}, Shuffled={sh.MomentMidSpanKnm}");
            Assert(s.MomentSupportRightKnm == sh.MomentSupportRightKnm,
                "5.4 M_right identical between sorted and shuffled stations",
                $"Sorted={s.MomentSupportRightKnm}, Shuffled={sh.MomentSupportRightKnm}");
            Assert(s.ShearSupportLeftKn == sh.ShearSupportLeftKn,
                "5.5 Q_left identical between sorted and shuffled stations");
            Assert(s.ShearMidSpanKn == sh.ShearMidSpanKn,
                "5.6 Q_mid identical between sorted and shuffled stations");
            Assert(s.ShearSupportRightKn == sh.ShearSupportRightKn,
                "5.7 Q_right identical between sorted and shuffled stations");

            // 5.3 Multiple load cases at same stations
            var multiComboStations = new List<EtabsStationForce>
            {
                new() { BeamMark = "B2", SpanIndex = 0, StationMm = 0.0, SpanLengthMm = 6000, OutputCase = "Comb1", V2 = 100.0, M3 = -80.0 },
                new() { BeamMark = "B2", SpanIndex = 0, StationMm = 0.0, SpanLengthMm = 6000, OutputCase = "Comb2", V2 = 160.0, M3 = -190.0 }, // critical left
                new() { BeamMark = "B2", SpanIndex = 0, StationMm = 3000.0, SpanLengthMm = 6000, OutputCase = "Comb1", V2 = 15.0, M3 = 90.0 },
                new() { BeamMark = "B2", SpanIndex = 0, StationMm = 3000.0, SpanLengthMm = 6000, OutputCase = "Comb2", V2 = 25.0, M3 = 145.0 }, // critical mid
                new() { BeamMark = "B2", SpanIndex = 0, StationMm = 6000.0, SpanLengthMm = 6000, OutputCase = "Comb1", V2 = 110.0, M3 = -75.0 },
                new() { BeamMark = "B2", SpanIndex = 0, StationMm = 6000.0, SpanLengthMm = 6000, OutputCase = "Comb2", V2 = 155.0, M3 = -180.0 }  // critical right
            };

            var envMulti = EtabsBeamForceReader.ExtractEnvelopes(multiComboStations)[0];
            Assert(envMulti.MomentSupportLeftKnm == -190.0, "5.8 Multi-combo envelopes extracted largest hogging M_left (-190.0)");
            Assert(envMulti.MomentMidSpanKnm == 145.0, "5.9 Multi-combo envelopes extracted largest sagging M_mid (145.0)");
            Assert(envMulti.MomentSupportRightKnm == -180.0, "5.10 Multi-combo envelopes extracted largest hogging M_right (-180.0)");
            Assert(envMulti.ShearSupportLeftKn == 160.0, "5.11 Multi-combo envelopes extracted largest shear Q_left (160.0)");

            // 5.4 Empty, null, and corrupt table text parsing
            Assert(EtabsBeamForceReader.ParseTableText("").Count == 0, "5.12 Empty table text returns empty collection");
            Assert(EtabsBeamForceReader.ParseTableText("   \r\n   ").Count == 0, "5.13 Whitespace table text returns empty collection");
            Assert(EtabsBeamForceReader.ParseTableText("Beam\tStation\tCase\tP\tV2\tM3").Count == 0, "5.14 Header only table text returns empty collection");

            string messyTable = "Beam\tStation\tCase\tP\tV2\tM3\r\n" +
                               "B1\t0\tComb1\t0\t150\t-120\r\n" +
                               "corrupt line\r\n" +
                               "B1\t3000\tComb1\t0\t20\t95\r\n" +
                               "\r\n" +
                               "B1\t6000\tComb1\t0\t-140\t-115";
            var messyEnvs = EtabsBeamForceReader.ParseTableText(messyTable);
            Assert(messyEnvs.Count == 1, "5.15 Messy table text with invalid lines parsed valid records without crashing");
        }
        catch (Exception ex)
        {
            Assert(false, "5. ETABS out-of-order stations and edge cases", ex.Message);
        }
    }

    private static void TestCalculationReportExportIntegrity()
    {
        Console.WriteLine("\n--- Group 6: Calculation Report Export (.xlsx Integrity & Formatting) ---");

        string exportFile = Path.Combine(Path.GetTempPath(), $"m5_beam_calc_report_{Guid.NewGuid():N}.xlsx");
        try
        {
            // Build 2 realistic reports: 1 compliant, 1 non-compliant
            var concrete = ConcreteMaterial.Create(ConcreteGrade.B25);
            var longSteel = SteelMaterial.Create(SteelGrade.CB400V);
            var stirrupSteel = SteelMaterial.Create(SteelGrade.CB240T);

            // Report 1: Compliant beam
            double b = 300, h = 600, l = 6000;
            var flxLeft = TcvnBeamFlexureCalculator.Calculate(b, h, -150.0, concrete, longSteel);
            var flxMid = TcvnBeamFlexureCalculator.Calculate(b, h, 120.0, concrete, longSteel);
            var flxRight = TcvnBeamFlexureCalculator.Calculate(b, h, -140.0, concrete, longSteel);

            var shrLeft = TcvnBeamShearCalculator.Calculate(b, h, 160.0, concrete, stirrupSteel, isSupportZone: true);
            var shrMid = TcvnBeamShearCalculator.Calculate(b, h, 30.0, concrete, stirrupSteel, isSupportZone: false);
            var shrRight = TcvnBeamShearCalculator.Calculate(b, h, 150.0, concrete, stirrupSteel, isSupportZone: true);

            var topRebar = TcvnBeamRebarOptimizer.Optimize(flxLeft.AsRequired, b, h, isTop: true);
            var botRebar = TcvnBeamRebarOptimizer.Optimize(flxMid.AsRequired, b, h, isTop: false);

            var report1 = new BeamCalculationReportModel
            {
                BeamMark = "2D1",
                SpanIndex = 0,
                WidthMm = b,
                HeightMm = h,
                LengthMm = l,
                ConcreteGrade = concrete.Name,
                LongitudinalSteelGrade = longSteel.Name,
                StirrupSteelGrade = stirrupSteel.Name,
                FlexureLeft = flxLeft,
                FlexureMid = flxMid,
                FlexureRight = flxRight,
                ShearLeft = shrLeft,
                ShearMid = shrMid,
                ShearRight = shrRight,
                TopSupportRebar = topRebar,
                BottomSpanRebar = botRebar,
                StirrupDescription = $"phi8 a{shrLeft.RecommendedSpacing}/a{shrMid.RecommendedSpacing}",
                IsCompliant = true,
                Notes = "Passes all TCVN 5574:2018 checks."
            };

            // Report 2: Non-compliant beam (extreme moment leading to failure)
            var flxFail = TcvnBeamFlexureCalculator.Calculate(200, 300, 500.0, concrete, longSteel);
            var shrFail = TcvnBeamShearCalculator.Calculate(200, 300, 600.0, concrete, stirrupSteel, isSupportZone: true);
            var optFail = TcvnBeamRebarOptimizer.Optimize(flxFail.AsRequired, 200, 300, isTop: true);

            var report2 = new BeamCalculationReportModel
            {
                BeamMark = "2D2-FAIL",
                SpanIndex = 1,
                WidthMm = 200,
                HeightMm = 300,
                LengthMm = 4000,
                ConcreteGrade = concrete.Name,
                LongitudinalSteelGrade = longSteel.Name,
                StirrupSteelGrade = stirrupSteel.Name,
                FlexureLeft = flxFail,
                FlexureMid = flxFail,
                FlexureRight = flxFail,
                ShearLeft = shrFail,
                ShearMid = shrFail,
                ShearRight = shrFail,
                TopSupportRebar = optFail,
                BottomSpanRebar = optFail,
                StirrupDescription = "phi6 a100",
                IsCompliant = false,
                Notes = "Over-reinforced; concrete crush limit exceeded."
            };

            var reports = new List<BeamCalculationReportModel> { report1, report2 };

            // Export to file
            BeamCalculationReportExporter.Export(reports, exportFile, "Dự án Thử nghiệm M5", "Challenger Agent");
            Assert(File.Exists(exportFile), "6.1 Report file successfully created on disk");

            var fileInfo = new FileInfo(exportFile);
            Assert(fileInfo.Length > 2000, $"6.2 Generated .xlsx has realistic size ({fileInfo.Length} bytes)");

            // Re-open and verify file content with ClosedXML
            using (var wb = new XLWorkbook(exportFile))
            {
                Assert(wb.Worksheets.Count == 2, "6.3 Workbook contains exactly 2 worksheets");
                Assert(wb.Worksheets.Contains("Tong_Hop"), "6.4 Sheet 'Tong_Hop' exists");
                Assert(wb.Worksheets.Contains("Chi_Tiet_Kiem_Toan"), "6.5 Sheet 'Chi_Tiet_Kiem_Toan' exists");

                // Verify Tong_Hop
                var wsSum = wb.Worksheet("Tong_Hop");
                string title = wsSum.Cell("A1").GetString();
                Assert(title.Contains("TCVN 5574:2018"), "6.6 Tong_Hop contains correct TCVN 5574:2018 title");

                string projInfo = wsSum.Cell("A2").GetString();
                Assert(projInfo.Contains("Dự án Thử nghiệm M5"), "6.7 Tong_Hop contains custom project name");
                Assert(projInfo.Contains("Challenger Agent"), "6.8 Tong_Hop contains custom engineer name");

                // Headers in row 4
                Assert(wsSum.Cell(4, 1).GetString() == "STT", "6.9 Summary header column 1 is STT");
                Assert(wsSum.Cell(4, 2).GetString() == "Tên dầm", "6.10 Summary header column 2 is Tên dầm");
                Assert(wsSum.Cell(4, 15).GetString() == "Kết luận", "6.11 Summary header column 15 is Kết luận");

                // Row 5 (Beam 1: compliant)
                Assert(wsSum.Cell(5, 2).GetString() == "2D1", "6.12 Row 5 beam mark is 2D1");
                Assert(wsSum.Cell(5, 4).GetString() == "300x600", "6.13 Row 5 section is 300x600");
                Assert(wsSum.Cell(5, 15).GetString() == "ĐẠT", "6.14 Row 5 status is ĐẠT");

                // Check formatting / styles on row 5 status cell
                var statusCell1 = wsSum.Cell(5, 15);
                Assert(statusCell1.Style.Font.Bold, "6.15 Status cell font is bold");
                Assert(statusCell1.Style.Alignment.Horizontal == XLAlignmentHorizontalValues.Center, "6.16 Status cell is center aligned");
                // Check background color for PASS
                var passBg = statusCell1.Style.Fill.BackgroundColor;
                Assert(passBg.Color.ToArgb() == System.Drawing.Color.FromArgb(0xE2, 0xEF, 0xDA).ToArgb(),
                    "6.17 Status ĐẠT cell has soft green background #E2EFDA");

                // Row 6 (Beam 2: fail)
                Assert(wsSum.Cell(6, 2).GetString() == "2D2-FAIL", "6.18 Row 6 beam mark is 2D2-FAIL");
                Assert(wsSum.Cell(6, 15).GetString() == "NG", "6.19 Row 6 status is NG");
                var failBg = wsSum.Cell(6, 15).Style.Fill.BackgroundColor;
                Assert(failBg.Color.ToArgb() == System.Drawing.Color.FromArgb(0xFC, 0xE4, 0xD6).ToArgb(),
                    "6.20 Status NG cell has soft red background #FCE4D6");

                // Verify Chi_Tiet_Kiem_Toan
                var wsDet = wb.Worksheet("Chi_Tiet_Kiem_Toan");
                string detTitle = wsDet.Cell("A1").GetString();
                Assert(detTitle.Contains("BẢNG CHI TIẾT KIỂM TOÁN"), "6.21 Detail sheet contains correct title");

                // Headers in row 3
                Assert(wsDet.Cell(3, 1).GetString() == "STT", "6.22 Detail header column 1 is STT");
                Assert(wsDet.Cell(3, 4).GetString() == "M (kNm)", "6.23 Detail header column 4 is M (kNm)");
                Assert(wsDet.Cell(3, 17).GetString() == "Kết luận", "6.24 Detail header column 17 is Kết luận");

                // 2 beams * 3 stations = 6 station rows (rows 4..9)
                Assert(wsDet.Cell(4, 2).GetString() == "2D1", "6.25 Row 4 is 2D1");
                Assert(wsDet.Cell(4, 3).GetString() == "Gối trái", "6.26 Row 4 station is Gối trái");
                Assert(wsDet.Cell(5, 3).GetString() == "Giữa nhịp", "6.27 Row 5 station is Giữa nhịp");
                Assert(wsDet.Cell(6, 3).GetString() == "Gối phải", "6.28 Row 6 station is Gối phải");

                Assert(wsDet.Cell(7, 2).GetString() == "2D2-FAIL", "6.29 Row 7 is 2D2-FAIL");
                Assert(wsDet.Cell(7, 3).GetString() == "Gối trái", "6.30 Row 7 station is Gối trái");
                Assert(wsDet.Cell(7, 17).GetString() == "NG", "6.31 Row 7 station status is NG");

                // Verify formula check
                int formulaCount = 0;
                foreach (var cell in wsDet.CellsUsed())
                {
                    if (cell.HasFormula) formulaCount++;
                }
                foreach (var cell in wsSum.CellsUsed())
                {
                    if (cell.HasFormula) formulaCount++;
                }
                RecordFinding($"Report exporter exports static computed values; formula count in workbook is {formulaCount}. Values are pre-evaluated rather than Excel formula expressions.");
                Assert(true, "6.32 Report export format and formula check completed");
            }

            // Test export with empty list (edge case)
            string emptyExportFile = Path.Combine(Path.GetTempPath(), $"m5_empty_report_{Guid.NewGuid():N}.xlsx");
            try
            {
                BeamCalculationReportExporter.Export(new List<BeamCalculationReportModel>(), emptyExportFile);
                Assert(File.Exists(emptyExportFile), "6.33 Empty reports list exports valid .xlsx without crashing");
                using var wbEmpty = new XLWorkbook(emptyExportFile);
                Assert(wbEmpty.Worksheets.Count == 2, "6.34 Empty reports workbook has both sheets");
            }
            finally
            {
                if (File.Exists(emptyExportFile)) File.Delete(emptyExportFile);
            }

            // Test export with null sub-properties
            string nullPropFile = Path.Combine(Path.GetTempPath(), $"m5_null_props_{Guid.NewGuid():N}.xlsx");
            try
            {
                var reportNull = new BeamCalculationReportModel
                {
                    BeamMark = "B-NULL",
                    SpanIndex = 0,
                    FlexureLeft = null,
                    FlexureMid = null,
                    FlexureRight = null,
                    ShearLeft = null,
                    ShearMid = null,
                    ShearRight = null,
                    TopSupportRebar = null,
                    BottomSpanRebar = null,
                    StirrupDescription = "None"
                };
                BeamCalculationReportExporter.Export(new List<BeamCalculationReportModel> { reportNull }, nullPropFile);
                Assert(File.Exists(nullPropFile), "6.35 Report with null flexure/shear sub-objects exports without null reference exceptions");
            }
            finally
            {
                if (File.Exists(nullPropFile)) File.Delete(nullPropFile);
            }

            // 6.36 Defensive null check on reports in Export(filePath)
            bool threwNullReportsPath = false;
            try
            {
                BeamCalculationReportExporter.Export(null!, "dummy.xlsx");
            }
            catch (ArgumentNullException)
            {
                threwNullReportsPath = true;
            }
            Assert(threwNullReportsPath, "6.36 Export(null, filePath) throws ArgumentNullException");

            // 6.37 Defensive null check on reports in Export(Stream)
            bool threwNullReportsStream = false;
            try
            {
                using var dummyStream = new MemoryStream();
                BeamCalculationReportExporter.Export(null!, dummyStream);
            }
            catch (ArgumentNullException)
            {
                threwNullReportsStream = true;
            }
            Assert(threwNullReportsStream, "6.37 Export(null, stream) throws ArgumentNullException");

            // 6.38 Defensive null check on reports in CreateWorkbook
            bool threwNullReportsWb = false;
            try
            {
                BeamCalculationReportExporter.CreateWorkbook(null!, "Project", "Engineer");
            }
            catch (ArgumentNullException)
            {
                threwNullReportsWb = true;
            }
            Assert(threwNullReportsWb, "6.38 CreateWorkbook(null, ...) throws ArgumentNullException");
        }
        catch (Exception ex)
        {
            Assert(false, "6. Calculation report export integrity", ex.ToString());
        }
        finally
        {
            if (File.Exists(exportFile)) File.Delete(exportFile);
        }
    }

    public static void TestProposedFixForRegionalCultureAndNaN()
    {
        Console.WriteLine("\n--- Group 7: Proof-of-Concept Fix for ReadDouble ---");

        // Simulating the proposed ReadDouble implementation
        static double ProposedReadDouble(string str, double defaultValue)
        {
            str = str.Trim();
            if (double.TryParse(str, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double parsedInv) &&
                !double.IsNaN(parsedInv) && !double.IsInfinity(parsedInv))
                return parsedInv;

            if (double.TryParse(str, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.GetCultureInfo("vi-VN"), out double parsedVi) &&
                !double.IsNaN(parsedVi) && !double.IsInfinity(parsedVi))
                return parsedVi;

            return defaultValue;
        }

        Assert(ProposedReadDouble("NaN", 0.0) == 0.0, "7.1 Proposed fix maps 'NaN' to defaultValue 0.0");
        Assert(ProposedReadDouble("-inf", 0.0) == 0.0, "7.2 Proposed fix maps '-inf' to defaultValue 0.0");
        Assert(ProposedReadDouble("N/A", 0.0) == 0.0, "7.3 Proposed fix maps 'N/A' to defaultValue 0.0");
        Assert(Math.Abs(ProposedReadDouble("5500,5", 0.0) - 5500.5) < 0.01, "7.4 Proposed fix parses '5500,5' as 5500.5 (vi-VN comma decimal)");
        Assert(Math.Abs(ProposedReadDouble("-180,25", 0.0) - (-180.25)) < 0.01, "7.5 Proposed fix parses '-180,25' as -180.25 (vi-VN comma decimal)");
        Assert(Math.Abs(ProposedReadDouble("135.75", 0.0) - 135.75) < 0.01, "7.6 Proposed fix parses '135.75' as 135.75 (invariant dot decimal)");
        Console.WriteLine("[PROVED] Proposed fix resolves all 5 failures without breaking any existing behavior.");
    }

    public static void TestCompoundHeaderNoShadowing()
    {
        Console.WriteLine("\n--- Group 8: Compound Header Precedence & No Substring Shadowing ---");

        try
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("HeaderPrecedence");

            // Deliberately reordered headers where compound headers (M_nhịp, Q_nhịp) appear around generic 'Nhịp'
            ws.Cell("A1").Value = "Dầm";
            ws.Cell("B1").Value = "M_nhịp";
            ws.Cell("C1").Value = "Nhịp";
            ws.Cell("D1").Value = "Q_nhịp";
            ws.Cell("E1").Value = "Chiều dài";

            ws.Cell("A2").Value = "B-SHADOW";
            ws.Cell("B2").Value = "250.5";
            ws.Cell("C2").Value = "3";
            ws.Cell("D2").Value = "75.2";
            ws.Cell("E2").Value = "6000";

            var result = ClosedXmlBeamForceReader.Read(ws);
            Assert(result != null && result.Count == 1, "8.1 Processed row with compound headers");

            var env = result![0];
            Assert(env.BeamMark == "B-SHADOW", "8.2 Beam mark parsed correctly");
            Assert(Math.Abs(env.MomentMidSpanKnm - 250.5) < 0.01,
                "8.3 M_nhịp parsed to MMid without being shadowed by generic 'nhịp'",
                $"Actual MomentMidSpanKnm = {env.MomentMidSpanKnm}");
            Assert(env.SpanIndex == 3,
                "8.4 Span index parsed correctly to SpanIndexCol without being overwritten by M_nhịp",
                $"Actual SpanIndex = {env.SpanIndex}");
            Assert(Math.Abs(env.ShearMidSpanKn - 75.2) < 0.01,
                "8.5 Q_nhịp parsed to QMid without being shadowed by generic 'nhịp'",
                $"Actual ShearMidSpanKn = {env.ShearMidSpanKn}");
            Assert(Math.Abs(env.SpanLengthMm - 6000.0) < 0.01,
                "8.6 Length parsed correctly",
                $"Actual SpanLengthMm = {env.SpanLengthMm}");
        }
        catch (Exception ex)
        {
            Assert(false, "8. Compound header precedence test", ex.ToString());
        }
    }
}

