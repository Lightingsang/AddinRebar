using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using HPRebar.BeamRebar.Service;
using Serilog;
using RevitView = Autodesk.Revit.DB.View;

namespace HPRebar.KataRebar.Service;

/// <summary>
/// The run's sheet "K-&lt;B3&gt;" (named after the beam, the title block the model's sheets use most): the long section on
/// top and the cross sections in a row under it, laid out as T2-DY7.dwg lays out B01 — each section's centre line
/// 250 + 1675 i mm (model) right of the elevation's origin, the beam tops of the sections in line 900 mm under the
/// elevation's lowest drawing —, at 1:25 on paper, the whole 30 mm in from the sheet's origin. Kata's own sheet rule
/// is not derived; these are B01's numbers. A re-run moves the viewports back into place and drops those of views the
/// run no longer has.
/// </summary>
internal static class KataSheetComposer
{
    private const double SectionStartMm = 250.0;
    private const double SectionStepMm = 1675.0;
    private const double SectionTopUnderElevationMm = 900.0;
    private const double PaperMarginMm = 30.0;

    /// <summary>A view to place and its crop in its drawing's millimetres (right, up from the drawing's origin).</summary>
    public sealed record Item(RevitView View, double Left, double Right, double Bottom, double Top);

    public sealed record Outcome(string SheetNumber, int Placed, IReadOnlyList<string> OnOtherSheets);

    /// <param name="elevation">The long section, its crop in local millimetres (x along the run, z up from its top).</param>
    /// <param name="elevationBottom">Lowest point of Kata's elevation drawing (local z).</param>
    /// <param name="sections">The cross sections in station order, crops in their own drawing (x right of the centre line, z up from the top).</param>
    public static Outcome Compose(Document doc, string runKey, string beamName, Item elevation, double elevationBottom, IReadOnlyList<Item> sections)
    {
        var sheet = KataDraftingStorage.Find<ViewSheet>(doc, runKey, KataDraftingStorage.SheetKind).FirstOrDefault() ?? NewSheet(doc, runKey, beamName);

        // Every view at its place on the drawing, in model millimetres from the elevation's origin.
        var layout = new List<(RevitView View, double Cx, double Cz, double MinX, double MinZ)>
        {
            (elevation.View, (elevation.Left + elevation.Right) / 2.0, (elevation.Bottom + elevation.Top) / 2.0, elevation.Left, elevation.Bottom)
        };
        double topRow = elevationBottom - SectionTopUnderElevationMm;
        for (int i = 0; i < sections.Count; i++)
        {
            var s = sections[i];
            double x0 = SectionStartMm + SectionStepMm * i;
            layout.Add((s.View, x0 + (s.Left + s.Right) / 2.0, topRow + (s.Bottom + s.Top) / 2.0, x0 + s.Left, topRow + s.Bottom));
        }

        double minX = layout.Min(v => v.MinX), minZ = layout.Min(v => v.MinZ);
        double scale = KataSectionViews.Scale;
        var existing = sheet.GetAllViewports().Select(doc.GetElement).OfType<Viewport>().ToList();
        var wanted = new HashSet<ElementId>(layout.Select(v => v.View.Id));
        foreach (var stale in existing.Where(v => !wanted.Contains(v.ViewId))) doc.Delete(stale.Id);

        var onOther = new List<string>();
        int placed = 0;
        foreach (var (view, cx, cz, _, _) in layout)
        {
            var centre = new XYZ(RevitUnits.MmToFt((cx - minX) / scale + PaperMarginMm), RevitUnits.MmToFt((cz - minZ) / scale + PaperMarginMm), 0.0);
            var viewport = existing.FirstOrDefault(v => v.ViewId == view.Id);
            if (viewport is not null)
            {
                viewport.SetBoxCenter(centre);
                placed++;
            }
            else if (Viewport.CanAddViewToSheet(doc, sheet.Id, view.Id))
            {
                Viewport.Create(doc, sheet.Id, view.Id, centre);
                placed++;
            }
            else
            {
                onOther.Add(view.Name);
                Log.Information("Kata Rebar: view {View} is on another sheet; left there", view.Name);
            }
        }

        return new Outcome(sheet.SheetNumber, placed, onOther);
    }

    /// <summary>
    /// The title block the model's sheets use most — the office's drawing sheet; the first by name picked a cover sheet
    /// ("HỒ SƠ THIẾT KẾ CƠ SỞ") in the HP template (measured). The first by name when no sheet has one yet.
    /// </summary>
    private static FamilySymbol? TitleBlock(Document doc)
    {
        var used = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_TitleBlocks).WhereElementIsNotElementType()
            .OfType<FamilyInstance>().GroupBy(i => KataSectionViews.IdValue(i.Symbol.Id))
            .OrderByDescending(g => g.Count()).Select(g => g.First().Symbol).FirstOrDefault();
        return used ?? new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_TitleBlocks).WhereElementIsElementType()
            .OfType<FamilySymbol>().OrderBy(t => t.FamilyName, StringComparer.OrdinalIgnoreCase).ThenBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static ViewSheet NewSheet(Document doc, string runKey, string beamName)
    {
        var titleBlock = TitleBlock(doc);
        var sheet = ViewSheet.Create(doc, titleBlock?.Id ?? ElementId.InvalidElementId);
        Log.Information("Kata Rebar: sheet for {Beam} uses title block {TitleBlock}", beamName, titleBlock is null ? "<none>" : $"{titleBlock.FamilyName}: {titleBlock.Name}");

        var taken = new HashSet<string>(new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)).Cast<ViewSheet>()
            .Where(s => s.Id != sheet.Id).Select(s => s.SheetNumber), StringComparer.OrdinalIgnoreCase);
        string number = "K-" + beamName;
        for (int i = 1; taken.Contains(number); i++) number = i == 1 ? $"K-{beamName} (Kata)" : $"K-{beamName} (Kata {i})";
        sheet.SheetNumber = number;
        sheet.Name = beamName;
        KataDraftingStorage.Write(sheet, runKey, KataDraftingStorage.SheetKind);
        return sheet;
    }
}
