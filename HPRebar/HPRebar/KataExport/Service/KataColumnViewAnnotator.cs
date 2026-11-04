using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPRebar.KataRebar.Service;
using Serilog;
using RevitView = Autodesk.Revit.DB.View;

namespace HPRebar.KataExport.Service;

/// <summary>
/// What the column views share: the stack's drafting tag (views, dimensions, notes found again on a re-run), the
/// column frame in model coordinates, rebar tags (a text note with the number when the model has no rebar tag
/// family), and the tags Revit refused.
/// </summary>
internal sealed class KataColumnViewAnnotator
{
    private readonly Document _doc;
    private readonly Transform _frame;
    private readonly Dictionary<string, int> _tagWays = new();

    public KataColumnViewAnnotator(Document doc, IReadOnlyList<FamilyInstance> columns, Transform frame)
    {
        _doc = doc;
        _frame = frame;
        RunKey = KataDraftingStorage.RunKey(columns);
        TagsMissing = !new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_RebarTags).WhereElementIsElementType().Any();
    }

    public string RunKey { get; }

    /// <summary>The model has no rebar tag family: numbers are written as text notes.</summary>
    public bool TagsMissing { get; }

    public int Tags { get; private set; }

    /// <summary>Tags Revit refused on every reference of the bar (the bar is not seen in the view).</summary>
    public int RefusedTags { get; private set; }

    /// <summary>How the tags were attached, for the log.</summary>
    public string TagWays => string.Join(", ", _tagWays.Select(kv => $"{kv.Key} {kv.Value}"));

    /// <summary>A point of the column frame (mm; X along b, Y along h, Z elevation) in the model.</summary>
    public XYZ ToModel(double x, double y, double z) => KataColumnFrame.ToModel(_frame, x, y, z);

    public XYZ AlongB => _frame.BasisX;

    public XYZ AlongH => _frame.BasisY;

    /// <summary>Dimensions, their ticks and text notes this stack drew on the view before.</summary>
    public void ClearDrafting(RevitView view)
    {
        var ids = KataDraftingStorage.Find<Dimension>(_doc, RunKey, KataDraftingStorage.DraftingKind).Cast<Element>()
            .Concat(KataDraftingStorage.Find<CurveElement>(_doc, RunKey, KataDraftingStorage.DraftingKind))
            .Concat(KataDraftingStorage.Find<TextNote>(_doc, RunKey, KataDraftingStorage.DraftingKind))
            .Where(e => e.OwnerViewId == view.Id)
            .Select(e => e.Id)
            .ToList();
        if (ids.Count > 0)
        {
            _doc.Delete(ids);
        }
    }

    public void Tag(RevitView view, Rebar rebar, XYZ point)
    {
        if (!TagsMissing)
        {
            string? last = null;
            foreach (var (how, reference) in References(view, rebar))
            {
                try
                {
                    IndependentTag.Create(_doc, view.Id, reference, false, TagMode.TM_ADDBY_CATEGORY, TagOrientation.Horizontal, point);
                    Tags++;
                    _tagWays[how] = _tagWays.TryGetValue(how, out int n) ? n + 1 : 1;
                    return;
                }
                catch (Autodesk.Revit.Exceptions.ApplicationException ex)
                {
                    last = $"{how}: {ex.Message}";
                }
            }

            Log.Warning("Kata Column Rebar: tag of rebar {Id} on {View} refused ({Reason})", rebar.Id, view.Name, last);
            RefusedTags++;
            return;
        }

        string number = rebar.get_Parameter(BuiltInParameter.REBAR_NUMBER)?.AsString() ?? "";
        var note = TextNote.Create(_doc, view.Id, point, number, _doc.GetDefaultElementTypeId(ElementTypeGroup.TextNoteType));
        KataDraftingStorage.Write(note, RunKey, KataDraftingStorage.DraftingKind);
        Tags++;
    }

    /// <summary>The rebar's first subelement, the element, then the first curve of its geometry in the view.</summary>
    private IEnumerable<(string How, Reference Reference)> References(RevitView view, Rebar rebar)
    {
        // Measured in Revit 2026: a plan refuses a tag on the element ("The reference can not be tagged") and takes it on
        // the rebar's first subelement.
        var sub = rebar.GetSubelements().FirstOrDefault();
        if (sub is not null && sub.GetReference() is { } subReference)
        {
            yield return ("subelement", subReference);
        }

        yield return ("element", new Reference(rebar));

        var geometry = rebar.get_Geometry(new Options { View = view, ComputeReferences = true });
        var curve = geometry?.OfType<Curve>().FirstOrDefault(c => c.Reference is not null)
                    ?? geometry?.OfType<GeometryInstance>().SelectMany(i => i.GetInstanceGeometry().OfType<Curve>()).FirstOrDefault(c => c.Reference is not null);
        if (curve?.Reference is { } curveReference)
        {
            yield return ("curve", curveReference);
        }
    }
}
