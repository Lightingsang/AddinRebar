using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HPKata.Core.Calculators;
using HPKata.Core.Configuration;
using HPKata.Core.Export;
using HPKata.Core.Models;
using HPKata.Core.Optimization;
using HPKata.Core.Scheduling;
using HPKata.Core.Serialization;
using HPKata.Core.Shapes;
using HPKata.Core.Tagging;

namespace VerifyHpkataM2Auditor;

class Program
{
    static int passCount = 0;
    static int failCount = 0;

    static void Assert(string name, bool condition, string details = "")
    {
        if (condition)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  [PASS] {name}");
            passCount++;
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  [FAIL] {name} - {details}");
            failCount++;
        }
        Console.ResetColor();
    }

    static int Main()
    {
        Console.WriteLine("================================================================================");
        Console.WriteLine("FORENSIC EMPIRICAL VERIFICATION OF HPKATA.CORE (NET 8)");
        Console.WriteLine("================================================================================\n");

        // 1. RebarCalculator
        Console.WriteLine("--- 1. RebarCalculator Verification ---");
        Assert("GetUnitMass(6) == 0.222 kg/m", Math.Abs(RebarCalculator.GetUnitMass(6) - 0.222) < 1e-4);
        Assert("GetUnitMass(16) == 1.580 kg/m", Math.Abs(RebarCalculator.GetUnitMass(16) - 1.580) < 1e-4);
        Assert("GetUnitMass(25) == 3.850 kg/m", Math.Abs(RebarCalculator.GetUnitMass(25) - 3.850) < 1e-4);
        Assert("GetUnitMass(30) uses d^2/162.2 formula", Math.Abs(RebarCalculator.GetUnitMass(30) - (900.0 / 162.2)) < 1e-3);
        Assert("Hook90Length(10, 10d) == 100 mm", RebarCalculator.Hook90Length(10, 10.0) == 100.0);
        Assert("Hook90Length(6, 10d) clamped to 75 mm minimum", RebarCalculator.Hook90Length(6, 10.0) == 75.0);
        Assert("Hook135Length(8, 7.5d) clamped to 75 mm minimum", RebarCalculator.Hook135Length(8, 7.5) == 75.0);
        Assert("Hook180Length(10) == 90 mm", RebarCalculator.Hook180Length(10) == 90.0);
        Assert("Hook180Length(6) clamped to 60 mm minimum", RebarCalculator.Hook180Length(6) == 60.0);
        Assert("LapSpliceLength(20, 30d) == 600 mm", RebarCalculator.LapSpliceLength(20, 30.0) == 600.0);
        Assert("TensionAnchorageLength(20, 40d) == 800 mm", RebarCalculator.TensionAnchorageLength(20, 40.0) == 800.0);
        Assert("RoundLength(1234, 50) == 1250 mm", RebarCalculator.RoundLength(1234, 50) == 1250.0);

        // 2. RebarShapeCatalog
        Console.WriteLine("\n--- 2. RebarShapeCatalog Verification ---");
        Assert("Catalog registers 85 definitions", RebarShapeCatalog.GetDefinition(RebarShapeType.Dang00).ShapeCode == "00");
        Assert("NormalizeShapeCode('01') -> Dang01", RebarShapeCatalog.NormalizeShapeCode("01") == RebarShapeType.Dang01);
        Assert("NormalizeShapeCode('dang02a') -> Dang02a", RebarShapeCatalog.NormalizeShapeCode("dang02a") == RebarShapeType.Dang02a);
        Assert("NormalizeShapeCode('67') -> Dang67", RebarShapeCatalog.NormalizeShapeCode("67") == RebarShapeType.Dang67);
        Assert("NormalizeShapeCode('coupler') -> DangCoupler", RebarShapeCatalog.NormalizeShapeCode("coupler") == RebarShapeType.DangCoupler);

        double len00 = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang00, new[] { 100.0, 200.0, 300.0, 400.0, 500.0 });
        Assert("Shape 00 sum of 5 segments == 1500 mm", len00 == 1500.0, $"Actual: {len00}");

        double len01 = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang01, new[] { 3500.0 });
        Assert("Shape 01 straight bar == 3500 mm", len01 == 3500.0, $"Actual: {len01}");

        double len02 = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang02, new[] { 500.0, 3000.0 });
        Assert("Shape 02 U-bar without ded == 4000 mm", len02 == 4000.0, $"Actual: {len02}");

        double len02Ded = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang02, new[] { 500.0, 3000.0 }, 0.0, 16, true);
        Assert("Shape 02 U-bar with ded < 4000 mm", len02Ded < 4000.0 && len02Ded > 3900.0, $"Actual: {len02Ded}");

        double len14 = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang14, new[] { 200.0, 400.0 });
        Assert("Shape 14 closed stirrup 2*(200+400) == 1200 mm", len14 == 1200.0, $"Actual: {len14}");

        double len67 = RebarShapeCatalog.CalculateCuttingLength(RebarShapeType.Dang67, new[] { 500.0, 100.0, 1000.0 }, 0.0, 10);
        Assert("Shape 67 helical spiral calculates authentic length > 15,000 mm", len67 > 15000.0, $"Actual: {len67}");

        Assert("SafeSin(0 deg) == 0.0 (safely clamped)", RebarShapeCatalog.SafeSin(0.0) == 0.0);
        Assert("SafeSin(90 deg) == 1.0", Math.Abs(RebarShapeCatalog.SafeSin(90.0) - 1.0) < 1e-5);

        // 3. KataConfig & Parser
        Console.WriteLine("\n--- 3. KataConfig & Parser Verification ---");
        var cfg = KataConfigDefaults.CreateDefault();
        Assert("KataConfig has 86 defaults", cfg.StandardBarLength == 11700 && cfg.ConcreteCoverMm == 30.0);
        var lines = KataConfigParser.SerializeToLines(cfg);
        Assert("SerializeToLines produces exactly 86 lines", lines.Count == 86, $"Count: {lines.Count}");
        var parsedCfg = KataConfigParser.ParseLines(lines);
        Assert("ParseLines roundtrips ConcreteCoverMm (30.0)", parsedCfg.ConcreteCoverMm == 30.0);
        Assert("ParseLines roundtrips StandardBarLength (11700)", parsedCfg.StandardBarLength == 11700);
        Assert("ParseLines roundtrips LayerThepChu ('kata_thep chu')", parsedCfg.LayerThepChu == "kata_thep chu");

        // 4. CuttingStockEngine & 5 Strategies
        Console.WriteLine("\n--- 4. CuttingStockEngine (1D-CSP) Verification ---");
        var engine = new CuttingStockEngine();
        var demands = new List<CuttingDemand>
        {
            new() { DiameterMm = 20, LengthMm = 3500.0, Quantity = 3 },
            new() { DiameterMm = 20, LengthMm = 4000.0, Quantity = 2 },
            new() { DiameterMm = 20, LengthMm = 2000.0, Quantity = 2 }
        };
        // Total demanded pieces: 3 + 2 + 2 = 7 pieces. Total length = 22,500 mm.

        var strategies = new[] { SolverStrategy.LapDayCanhDai, SolverStrategy.DuBang0, SolverStrategy.BeamSearchTopK, SolverStrategy.PatternCat, SolverStrategy.Auto };
        foreach (var st in strategies)
        {
            var res = engine.Solve(20, demands, null, st);
            int cutCount = res.Patterns.Sum(p => p.CutsMm.Count);
            double totalCut = res.Patterns.Sum(p => p.CutsMm.Sum());
            bool piecesConserved = (cutCount == 7 && Math.Abs(totalCut - 22500.0) < 1e-4);
            Assert($"Strategy {st}: cutCount=7 and totalCut=22,500 mm conserved", piecesConserved, $"cutCount={cutCount}, totalCut={totalCut}");
            Assert($"Strategy {st}: Net waste is between 0% and 15%", res.NetWastePercent >= 0.0 && res.NetWastePercent <= 15.0, $"Waste: {res.NetWastePercent:F2}%");
        }

        // Remnant inventory test
        var warehouse = new List<WarehouseOffcut>
        {
            new() { DiameterMm = 20, LengthMm = 6000.0, Quantity = 1 }
        };
        var resWarehouse = engine.Solve(20, demands, warehouse, SolverStrategy.Auto);
        Assert("Remnant prioritized: ConsumedWarehouseBarsCount == 1", resWarehouse.ConsumedWarehouseBarsCount == 1, $"Actual: {resWarehouse.ConsumedWarehouseBarsCount}");

        // 5. Serialization (XData & Block Attributes)
        Console.WriteLine("\n--- 5. Serialization Verification ---");
        var xdRecord = new KataXDataRecord
        {
            Dang = "02",
            STT = 5,
            Phi = 18,
            KhoangCach = "a150",
            SoLuong = "12",
            SoLuongCauKien = 2,
            ChieuDaiCacDoanMm = new[] { 500.0, 3200.0 },
            TenCauKien = "D1",
            LinkedHandle = "3F1A",
            LinkType = "THEP_DON"
        };
        var dxfList = KataXDataSerializer.SerializeToDxf(xdRecord);
        Assert("SerializeToDxf produces >= 12 typed values", dxfList.Count >= 12, $"Count: {dxfList.Count}");
        var restoredXd = KataXDataSerializer.DeserializeFromDxf(dxfList);
        Assert("Roundtrip XData preserves Dang ('02')", restoredXd.Dang == "02");
        Assert("Roundtrip XData preserves STT (5)", restoredXd.STT == 5);
        Assert("Roundtrip XData preserves Phi (18)", restoredXd.Phi == 18);
        Assert("Roundtrip XData preserves ChieuDaiCacDoanMm (500, 3200)", restoredXd.ChieuDaiCacDoanMm.Length == 2 && restoredXd.ChieuDaiCacDoanMm[0] == 500.0 && restoredXd.ChieuDaiCacDoanMm[1] == 3200.0);
        Assert("Roundtrip XData preserves LinkedHandle ('3F1A')", restoredXd.LinkedHandle == "3F1A");

        string jsonXd = KataXDataSerializer.SerializeToJson(xdRecord);
        Assert("SerializeToJson contains '3F1A'", jsonXd.Contains("3F1A"));
        var restoredJson = KataXDataSerializer.DeserializeFromJson(jsonXd);
        Assert("DeserializeFromJson preserves Phi (18)", restoredJson.Phi == 18);

        // Block attributes test
        var tagData = new KataTagBlockData { SH1 = "05", DK1 = "18", KC1 = "a150", DKKC1 = "12Ø18a150" };
        var dict = KataBlockAttributeSerializer.WriteTagBlock(tagData);
        var readTag = KataBlockAttributeSerializer.ReadTagBlock(dict);
        Assert("KataBlockAttributeSerializer roundtrips SH1 ('05') and DK1 ('18')", readTag.SH1 == "05" && readTag.DK1 == "18");

        // 6. RebarScheduleAggregator
        Console.WriteLine("\n--- 6. RebarScheduleAggregator Verification ---");
        var scheduleItems = new List<RebarItem>
        {
            new() { ComponentMark = "D1", BarMark = "1", ShapeCode = "14", DiameterMm = 8, ParametersMm = new[] { 200.0, 400.0 }, MemberCount = 1, CountPerMember = 10 },
            new() { ComponentMark = "D1", BarMark = "2", ShapeCode = "01", DiameterMm = 16, ParametersMm = new[] { 4000.0 }, MemberCount = 1, CountPerMember = 4 },
            new() { ComponentMark = "D1", BarMark = "3", ShapeCode = "01", DiameterMm = 25, ParametersMm = new[] { 6000.0 }, MemberCount = 1, CountPerMember = 2 }
        };
        var report = RebarScheduleAggregator.GenerateReport(scheduleItems, false);
        Assert("Report has 3 items", report.Items.Count == 3);
        Assert("Report has 3 diameter groups", report.DiameterSummaries.Count == 3);
        Assert("TCVN WireLe10Kg > 0 (D=8)", report.Bins.WireLe10Kg > 0.0, $"Actual: {report.Bins.WireLe10Kg:F2} kg");
        Assert("TCVN Deformed10To18Kg > 0 (D=16)", report.Bins.Deformed10To18Kg > 0.0, $"Actual: {report.Bins.Deformed10To18Kg:F2} kg");
        Assert("TCVN DeformedGt18Kg > 0 (D=25)", report.Bins.DeformedGt18Kg > 0.0, $"Actual: {report.Bins.DeformedGt18Kg:F2} kg");

        // 7. QrTagGenerator
        Console.WriteLine("\n--- 7. QrTagGenerator Verification ---");
        var qrp = new QrTagPayload
        {
            Project = "VINHOMES_GRAND_PARK",
            Client = "VNGROUP",
            Order = "ORD-2026-09",
            Component = "B101",
            Mark = "01",
            ShapeCode = "02",
            DiameterMm = 20,
            CutLengthMm = 4000.0,
            Quantity = 50,
            WeightKg = 494.0,
            SteelGrade = "CB400-V",
            SegmentsMm = new[] { 500.0, 3000.0, 500.0, 0.0, 0.0, 0.0 }
        };
        string formattedPayload = QrTagGenerator.FormatPayload(qrp);
        Assert("FormatPayload starts with 'KATA|'", formattedPayload.StartsWith("KATA|"), $"Payload: {formattedPayload}");
        var parsedQrp = QrTagGenerator.ParsePayload(formattedPayload);
        Assert("ParsePayload restores Project ('VINHOMES_GRAND_PARK')", parsedQrp.Project == "VINHOMES_GRAND_PARK");
        Assert("ParsePayload restores CutLengthMm (4000)", parsedQrp.CutLengthMm == 4000.0);
        Assert("ParsePayload restores SegmentsMm[1] (3000)", parsedQrp.SegmentsMm[1] == 3000.0);

        byte[] pngBytes = QrTagGenerator.GeneratePngBytes(formattedPayload, 10);
        Assert("GeneratePngBytes produces byte array > 500 bytes", pngBytes != null && pngBytes.Length > 500, $"Length: {pngBytes?.Length}");
        byte[] expectedPngMagic = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        bool magicMatch = pngBytes != null && pngBytes.Take(8).SequenceEqual(expectedPngMagic);
        Assert("GeneratePngBytes has valid PNG magic header (89 50 4E 47 0D 0A 1A 0A)", magicMatch);

        // 8. ClosedXmlExporter
        Console.WriteLine("\n--- 8. ClosedXmlExporter Verification ---");
        using var ms = new MemoryStream();
        ClosedXmlExporter.ExportScheduleToStream(report, ms);
        byte[] xlsxBytes = ms.ToArray();
        Assert("ExportScheduleToStream produces valid stream > 3000 bytes", xlsxBytes.Length > 3000, $"Length: {xlsxBytes.Length}");
        byte[] expectedZipMagic = { 0x50, 0x4B, 0x03, 0x04 };
        bool zipMagicMatch = xlsxBytes.Take(4).SequenceEqual(expectedZipMagic);
        Assert("ExportScheduleToStream produces valid OpenXML Zip magic header (PK..)", zipMagicMatch);

        // Summary
        Console.WriteLine("\n================================================================================");
        Console.WriteLine("VERIFICATION RESULTS SUMMARY:");
        Console.WriteLine($"Total Tests: {passCount + failCount}");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Passed:      {passCount}");
        if (failCount > 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Failed:      {failCount}");
        }
        else
        {
            Console.WriteLine($"Failed:      0");
        }
        Console.ResetColor();
        Console.WriteLine("================================================================================");

        return failCount == 0 ? 0 : 1;
    }
}
