using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using HPRebar.BeamRebar.Model;
using HPRebar.Core.BeamRebar.Models;
using Serilog;

namespace HPRebar.BeamRebar.Service;

/// <summary>
/// Creates transverse cross-section detail views cut at support zones and midspans.
/// </summary>
public static class SectionViewCreator
{
    private const string ViewTypeName = "@BeamSection";

    public static IReadOnlyList<ViewSection> Create(
        Document document,
        BeamStack stack,
        BeamAnnotationSettings settings)
    {
        var views = new List<ViewSection>();
        var viewType = DetailViewCreator.ResolveViewType(document, ViewTypeName);

        if (viewType is null)
        {
            Log.Warning("The document has no section view family type; cross-section views were skipped.");
            return views;
        }

        for (int spanIndex = 0; spanIndex < stack.Spans.Count; spanIndex++)
        {
            var span = stack.Spans[spanIndex];
            var cutStations = ComputeCutStations(span, settings.SectionsPerSpan);

            for (int cutIndex = 0; cutIndex < cutStations.Count; cutIndex++)
            {
                double stationX = cutStations[cutIndex];
                var sectionView = CreateSectionAtStation(
                    document, viewType, settings, stack, spanIndex, cutIndex, stationX);

                if (sectionView is not null)
                {
                    views.Add(sectionView);
                }
            }
        }

        return views;
    }

    public static IReadOnlyList<double> ComputeCutStations(BeamSpan span, int sectionsPerSpan)
    {
        var stations = new List<double>();
        if (span.IsCantilever || sectionsPerSpan <= 1)
        {
            stations.Add(span.StartX + span.LengthClear * 0.5);
            return stations;
        }

        if (sectionsPerSpan == 2)
        {
            stations.Add(span.StartX + span.LengthClear / 6.0); // Support zone
            stations.Add(span.StartX + span.LengthClear * 0.5); // Midspan
        }
        else // 3 sections per span (Standard detailing)
        {
            stations.Add(span.StartX + span.LengthClear / 6.0);       // Left Support
            stations.Add(span.StartX + span.LengthClear * 0.5);       // Midspan
            stations.Add(span.StartX + span.LengthClear * 5.0 / 6.0); // Right Support
        }

        return stations;
    }

    private static ViewSection CreateSectionAtStation(
        Document document,
        ViewFamilyType viewType,
        BeamAnnotationSettings settings,
        BeamStack stack,
        int spanIndex,
        int cutIndex,
        double stationXMm)
    {
        var span = stack.Spans[spanIndex];
        var marginFt = RevitUnits.MmToFt(settings.ViewMargin);
        var widthFt = RevitUnits.MmToFt(span.Width);
        var heightFt = RevitUnits.MmToFt(span.Height);
        var stationFt = RevitUnits.MmToFt(stationXMm);

        XYZ axisDir = stack.BeamDirection;
        XYZ sideDir = stack.TransverseDirection;

        XYZ cutCenter = stack.OriginPoint 
            + stationFt * axisDir 
            + (RevitUnits.MmToFt(span.TopElevation) - heightFt * 0.5) * XYZ.BasisZ;

        var transform = Transform.Identity;
        transform.Origin = cutCenter;
        transform.BasisX = sideDir;
        transform.BasisY = XYZ.BasisZ;
        transform.BasisZ = axisDir; // Camera looks along beam axis

        var box = new BoundingBoxXYZ
        {
            Transform = transform,
            // Right-side margin scaled +2.5x to accommodate the bar schedule table
            Min = new XYZ(-widthFt * 0.5 - marginFt, -heightFt * 0.5 - marginFt, -marginFt * 0.5),
            Max = new XYZ(widthFt * 0.5 + 2.5 * marginFt, heightFt * 0.5 + marginFt, marginFt * 0.5)
        };

        ViewSection view;
        if (viewType.ViewFamily == ViewFamily.Detail)
        {
            view = ViewSection.CreateDetail(document, viewType.Id, box);
        }
        else
        {
            view = ViewSection.CreateSection(document, viewType.Id, box);
        }

        view.get_Parameter(BuiltInParameter.VIEWER_CROP_REGION_VISIBLE)?.Set(0);

        if (settings.SectionTemplate is not null)
        {
            view.ViewTemplateId = settings.SectionTemplate.Id;
        }

        string sectionName = settings.SectionViewName(spanIndex + 1, cutIndex + 1);
        DetailViewCreator.Rename(view, sectionName);

        return view;
    }
}
