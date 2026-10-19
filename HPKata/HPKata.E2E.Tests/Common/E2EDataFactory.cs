using System.Collections.Generic;
using System.Linq;
using HPKata.AutoCad.Abstractions;
using HPKata.Core.Configuration;
using HPKata.Core.Models;
using HPKata.Core.Serialization;
using HPKata.Revit.Abstractions;

namespace HPKata.E2E.Tests.Common;

/// <summary>
/// Factory generating realistic engineering rebar data for end-to-end tests.
/// </summary>
public static class E2EDataFactory
{
    public static KataConfig CreateStandardConfig() => new()
    {
        ConcreteCoverMm = 30.0,
        BaseBarDiameter = 20,
        LapLengthMultiplier = 40.0,
        TensionAnchorMult = 35.0,
        DefaultHookAngle = "90",
        ApplyBendDeduction = 1,
        AutoRenumberMarks = 1
    };

    public static CadEntityDto CreateCadRebarEntity(
        string handle,
        string dang,
        int stt,
        int phi,
        string soLuong,
        int soLuongCk,
        double[] lengthsMm,
        string tenCk = "D1")
    {
        var record = new KataXDataRecord
        {
            Dang = dang,
            STT = (short)stt,
            Phi = (short)phi,
            KhoangCach = "a200",
            SoLuong = soLuong,
            SoLuongCauKien = (short)soLuongCk,
            ChieuDaiCacDoanMm = lengthsMm,
            TenCauKien = tenCk,
            GocUon = "90"
        };

        double totalLen = lengthsMm.Sum();

        return new CadEntityDto
        {
            Handle = handle,
            Layer = "THEP_CHU",
            EntityType = "POLYLINE",
            XDataRecord = record,
            TotalLengthMm = totalLen
        };
    }

    public static RevitRebarDto CreateRevitRebarDto(
        long elementId,
        string partition,
        string mark,
        string shapeName,
        int dia,
        int qty,
        double singleBarLenMm,
        double[]? paramsMm = null)
    {
        return new RevitRebarDto
        {
            ElementId = elementId,
            Partition = partition,
            Mark = mark,
            ShapeName = shapeName,
            DiameterMm = dia,
            Quantity = qty,
            SingleBarLengthMm = singleBarLenMm,
            ParametersMm = paramsMm ?? [singleBarLenMm],
            Comments = $"E2E_Rebar_{partition}_{mark}"
        };
    }

    public static List<RebarItem> CreateContinuousBeamItems(
        string beamMark = "B101",
        double spanMm = 6000.0)
    {
        return
        [
            new RebarItem
            {
                ComponentMark = beamMark,
                BarMark = "1",
                ShapeCode = "02",
                DiameterMm = 20,
                CountPerMember = 3,
                MemberCount = 1,
                ParametersMm = [400.0, spanMm * 2 + 500.0, 400.0],
                CuttingLengthMm = spanMm * 2 + 1300.0
            },
            new RebarItem
            {
                ComponentMark = beamMark,
                BarMark = "2",
                ShapeCode = "02",
                DiameterMm = 20,
                CountPerMember = 3,
                MemberCount = 1,
                ParametersMm = [400.0, spanMm * 2 + 500.0, 400.0],
                CuttingLengthMm = spanMm * 2 + 1300.0
            },
            new RebarItem
            {
                ComponentMark = beamMark,
                BarMark = "3",
                ShapeCode = "01",
                DiameterMm = 18,
                CountPerMember = 2,
                MemberCount = 1,
                ParametersMm = [spanMm * 0.3],
                CuttingLengthMm = spanMm * 0.3
            },
            new RebarItem
            {
                ComponentMark = beamMark,
                BarMark = "4",
                ShapeCode = "22",
                DiameterMm = 8,
                CountPerMember = 60,
                MemberCount = 1,
                ParametersMm = [550.0, 250.0, 550.0, 250.0, 70.0, 70.0],
                CuttingLengthMm = 1740.0
            }
        ];
    }
}
