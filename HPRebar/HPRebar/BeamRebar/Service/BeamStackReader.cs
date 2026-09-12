using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using HPRebar.BeamRebar.Model;
using HPRebar.Core.BeamRebar.Models;

namespace HPRebar.BeamRebar.Service;

/// <summary>
/// Reads a validated selection of structural framing elements and produces a fully assembled <see cref="BeamStack"/>.
/// Performs all conversions between Revit decimal feet and domain millimetres.
/// </summary>
public static class BeamStackReader
{
    public static BeamStack Read(Document doc, IReadOnlyList<Element> selectedBeams)
    {
        if (selectedBeams is null || selectedBeams.Count == 0)
            throw new ArgumentException("No beams provided to BeamStackReader.", nameof(selectedBeams));

        // 1. Establish dominant longitudinal axis X_beam and transverse axis Y_beam
        var primaryLine = (selectedBeams[0].Location as LocationCurve)!.Curve as Line;
        XYZ originRef = primaryLine!.GetEndPoint(0);
        XYZ beamAxis = (primaryLine.GetEndPoint(1) - originRef).Normalize();
        XYZ transAxis = XYZ.BasisZ.CrossProduct(beamAxis).Normalize();

        // 2. Project and sort beams in ascending order along X_beam
        var orderedBeams = selectedBeams
            .Select(b =>
            {
                var line = (b.Location as LocationCurve)!.Curve as Line;
                double s0 = (line!.GetEndPoint(0) - originRef).DotProduct(beamAxis);
                double s1 = (line.GetEndPoint(1) - originRef).DotProduct(beamAxis);
                return new { Beam = b, MinS = Math.Min(s0, s1), MaxS = Math.Max(s0, s1) };
            })
            .OrderBy(item => item.MinS)
            .Select(item => item.Beam)
            .ToList();

        // Origin datum point at start of first beam
        XYZ originPoint = originRef + (orderedBeams.Select(b =>
        {
            var l = (b.Location as LocationCurve)!.Curve as Line;
            return Math.Min((l!.GetEndPoint(0) - originRef).DotProduct(beamAxis),
                            (l.GetEndPoint(1) - originRef).DotProduct(beamAxis));
        }).Min()) * beamAxis;

        // 3. Find supports and secondary intersections
        var supports = BeamSupportFinder.FindSupports(doc, orderedBeams, beamAxis, transAxis, originPoint);
        var secondaryBeams = BeamSupportFinder.FindSecondaryBeams(doc, orderedBeams, beamAxis, transAxis, originPoint);

        // 4. Extract faces and construct domain spans
        var facesList = new List<BeamFaces>(orderedBeams.Count);
        var spansList = new List<BeamSpan>(orderedBeams.Count);

        for (int i = 0; i < orderedBeams.Count; i++)
        {
            var beam = orderedBeams[i];
            var faces = ReadBeamFaces(doc, beam, beamAxis, transAxis, i);
            facesList.Add(faces);

            double widthMm = BeamSolidFaceReader.GetWidthMm(beam, transAxis);
            double heightMm = BeamSolidFaceReader.GetHeightMm(beam);
            double topElevMm = RevitUnits.FtToMm(faces.Top.Origin.Z - originPoint.Z);

            // Locate adjacent supports for this span
            var leftSupport = i < supports.Count ? supports[i] : null;
            var rightSupport = (i + 1) < supports.Count ? supports[i + 1] : null;

            double leftCenterXMm = leftSupport?.CenterX ?? 0.0;
            double rightCenterXMm = rightSupport?.CenterX ?? leftCenterXMm + 4000.0;
            double leftWidthMm = leftSupport?.Width ?? 0.0;
            double rightWidthMm = rightSupport?.Width ?? 0.0;

            double lengthCenterMm = rightCenterXMm - leftCenterXMm;
            double lengthClearMm = lengthCenterMm - (leftWidthMm / 2.0) - (rightWidthMm / 2.0);
            if (lengthClearMm <= 0) lengthClearMm = lengthCenterMm;

            double startXMm = leftCenterXMm + (leftWidthMm / 2.0);

            var cantPos = CantileverPosition.None;
            if (leftSupport?.Type == SupportType.CantileverEnd && rightSupport?.Type == SupportType.CantileverEnd)
                cantPos = CantileverPosition.Both;
            else if (leftSupport?.Type == SupportType.CantileverEnd)
                cantPos = CantileverPosition.Left;
            else if (rightSupport?.Type == SupportType.CantileverEnd)
                cantPos = CantileverPosition.Right;

            var span = new BeamSpan(
                index: i,
                name: $"Span {i + 1}",
                lengthCenter: lengthCenterMm,
                width: widthMm,
                height: heightMm,
                topElevation: topElevMm,
                cover: 25.0, // Default cover
                clearLength: lengthClearMm,
                startX: startXMm,
                cantilever: cantPos,
                elementUniqueId: beam.UniqueId);

            spansList.Add(span);
        }

        // 5. Build pure BeamContinuousStack
        var continuousStack = new BeamContinuousStack(spansList, supports, secondaryBeams);

        // 6. Build coordinate mapper
        var mapper = new PointMapper(originPoint, beamAxis, transAxis, XYZ.BasisZ);

        // 7. Build dimension witness faces
        var dimFaces = BuildDimensionFaces(facesList);
        var supportFaces = BuildSupportFaces(facesList);

        return new BeamStack
        {
            Style = BeamSectionStyle.Rectangle,
            ContinuousStack = continuousStack,
            Faces = facesList,
            PointMapper = mapper,
            BeamDirection = beamAxis,
            TransverseDirection = transAxis,
            OriginPoint = originPoint,
            DatumFace = facesList[0].StartFace ?? facesList[0].Bottom,
            TopDatum = facesList[0].Top,
            BottomDatum = facesList[0].Bottom,
            DimensionFaces = dimFaces,
            SupportFaces = supportFaces
        };
    }

    private static BeamFaces ReadBeamFaces(Document doc, Element beam, XYZ beamAxis, XYZ transAxis, int spanIndex)
    {
        return new BeamFaces
        {
            Element = beam,
            Top = BeamSolidFaceReader.GetTop(beam),
            Bottom = BeamSolidFaceReader.GetBottom(beam),
            Left = BeamSolidFaceReader.GetLeftFace(beam, transAxis),
            Right = BeamSolidFaceReader.GetRightFace(beam, transAxis),
            StartFace = BeamSolidFaceReader.GetStartFace(beam, beamAxis),
            EndFace = BeamSolidFaceReader.GetEndFace(beam, beamAxis),
            SpanIndex = spanIndex,
            Level = LevelOf(doc, beam, BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM)
        };
    }

    private static IReadOnlyList<PlanarFace> BuildDimensionFaces(IReadOnlyList<BeamFaces> facesList)
    {
        var list = new List<PlanarFace>();
        foreach (var f in facesList)
        {
            if (f.StartFace != null) list.Add(f.StartFace);
            if (f.Top != null) list.Add(f.Top);
            if (f.Bottom != null) list.Add(f.Bottom);
            if (f.EndFace != null) list.Add(f.EndFace);
        }
        return list;
    }

    private static IReadOnlyList<PlanarFace> BuildSupportFaces(IReadOnlyList<BeamFaces> facesList)
    {
        var list = new List<PlanarFace>();
        for (int i = 0; i < facesList.Count; i++)
        {
            if (facesList[i].StartFace != null) list.Add(facesList[i].StartFace!);
            if (i == facesList.Count - 1 && facesList[i].EndFace != null) list.Add(facesList[i].EndFace!);
        }
        return list;
    }

    private static Level? LevelOf(Document doc, Element elem, BuiltInParameter paramId)
    {
        var id = elem.get_Parameter(paramId)?.AsElementId();
        return id is null ? null : doc.GetElement(id) as Level;
    }
}
