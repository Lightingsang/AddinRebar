using System.Collections.Generic;
using Autodesk.Revit.DB;
using HPRebar.BeamRebar.Model;
using HPRebar.Core.BeamRebar.Models;

namespace HPRebar.BeamRebar.Service;

/// <summary>
///     The settings the window opens with: sensible bar counts, diameters and spacings derived from the
///     bar types the document actually has, so the user edits a working spec rather than an empty one.
/// </summary>
public static class BeamDefaultSpecBuilder
{
    public static BeamRebarSpec Build(Document document, BeamStack stack)
    {
        var catalog = new RebarTypeCatalog(document);
        var stirrupType = catalog.FindBarType(string.Empty, 8.0);
        var mainType = catalog.FindBarType(string.Empty, 20.0);
        var addType = catalog.FindBarType(string.Empty, 18.0);
        var sideType = catalog.FindBarType(string.Empty, 12.0);
        var tieType = catalog.FindBarType(string.Empty, 8.0);

        var supportTopBars = new List<SupportAdditionalTopBarConfig>();
        for (int i = 0; i < stack.Supports.Count; i++)
        {
            supportTopBars.Add(new SupportAdditionalTopBarConfig
            {
                SupportIndex = i,
                Layer1Count = 2,
                Layer1Diameter = addType?.DiameterMm ?? 18.0,
                Layer1ExtensionRatio = 1.0 / 3.0,
                BarTypeName = addType?.Name ?? string.Empty
            });
        }

        var spanBottomBars = new List<SpanAdditionalBottomBarConfig>();
        for (int i = 0; i < stack.Spans.Count; i++)
        {
            spanBottomBars.Add(new SpanAdditionalBottomBarConfig
            {
                SpanIndex = i,
                Layer1Count = 2,
                Layer1Diameter = addType?.DiameterMm ?? 18.0,
                CutoffRatio = 1.0 / 7.0,
                BarTypeName = addType?.Name ?? string.Empty
            });
        }

        return new BeamRebarSpec
        {
            Stirrups = new BeamStirrupSpec
            {
                Layout = StirrupLayout.ThreeZoneL4,
                Diameter = stirrupType?.DiameterMm ?? 8.0,
                Cover = 25.0,
                SpacingDense = 100.0,
                SpacingSparse = 200.0,
                StartOffset = 50.0,
                BarTypeName = stirrupType?.Name ?? string.Empty
            },
            MainBars = new BeamMainBarSpec
            {
                TopCount = 2,
                TopDiameter = mainType?.DiameterMm ?? 20.0,
                BottomCount = 2,
                BottomDiameter = mainType?.DiameterMm ?? 20.0,
                TopBarTypeName = mainType?.Name ?? string.Empty,
                BottomBarTypeName = mainType?.Name ?? string.Empty
            },
            AdditionalBars = new BeamAdditionalBarSpec
            {
                SupportTopBars = supportTopBars,
                SpanBottomBars = spanBottomBars
            },
            SideBars = new BeamSideBarSpec
            {
                AutoSkinBars = true,
                DepthThreshold = 700.0,
                Diameter = sideType?.DiameterMm ?? 12.0,
                MaxVerticalSpacing = 300.0,
                IncludeCrossTies = true,
                CrossTieDiameter = tieType?.DiameterMm ?? 8.0,
                SideBarTypeName = sideType?.Name ?? string.Empty,
                CrossTieBarTypeName = tieType?.Name ?? string.Empty
            },
            SpecialBars = new BeamSpecialBarSpec
            {
                EnableHangingStirrups = true,
                HangingStirrupsPerSide = 3,
                HangingStirrupDiameter = stirrupType?.DiameterMm ?? 8.0,
                HangingStirrupSpacing = 50.0,
                EnableDiagonalTies = false,
                HangingStirrupTypeName = stirrupType?.Name ?? string.Empty
            },
            MainBarType = mainType,
            StirrupBarType = stirrupType,
            AddTopBarType = addType,
            AddBottomBarType = addType,
            SideBarType = sideType,
            TieBarType = tieType,
            PartitionName = "Beam"
        };
    }
}
