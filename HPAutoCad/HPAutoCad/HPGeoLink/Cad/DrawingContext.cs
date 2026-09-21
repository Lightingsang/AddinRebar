using System.IO;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using HPAutoCad.Core.HPGeoLink.Geometry;
using HPAutoCad.Core.HPGeoLink.Units;

namespace HPAutoCad.HPGeoLink.Cad;

/// <summary>
/// What the tool needs to know about the active drawing before converting anything: its name (the default
/// KMZ name), where it lives (the default export folder) and its INSUNITS — the unit VN-2000 metres are
/// scaled from. INSUNITS 0 means "unknown" and the user must choose; the tool never assumes metres.
/// </summary>
internal sealed record DrawingContext(string DocumentName, string? DirectoryPath, int InsUnitsCode, DrawingUnit Unit)
{
    public double? MetersPerUnit => DrawingUnitFactor.MetersPerUnit(Unit);

    /// <summary>Arc chord tolerance in drawing units: 5 mm whatever the unit (5 mm on a metre drawing, 5 units on a millimetre one).</summary>
    public double ChordToleranceDrawingUnits => ChordToleranceFor(MetersPerUnit);

    public static double ChordToleranceFor(double? metersPerUnit) =>
        metersPerUnit is { } f && f > 0 ? BulgeTessellator.DefaultToleranceM / f : BulgeTessellator.DefaultToleranceM;

    public string UnitLabel => DrawingUnitFactor.Label(Unit);

    /// <summary>Drawing name without extension, for the KMZ file name.</summary>
    public string BaseName => Path.GetFileNameWithoutExtension(DocumentName);

    public static DrawingContext Read(Document doc)
    {
        var db = doc.Database;
        var insUnits = (int)db.Insunits;
        var fileName = db.Filename;
        string? dir = null;
        try
        {
            // An unsaved drawing's Database.Filename is the template it was made from (…\Template\acad.dwt), which
            // exists — IsNamedDrawing is what says whether the drawing has a folder of its own.
            if (doc.IsNamedDrawing && !string.IsNullOrWhiteSpace(fileName) && Path.IsPathRooted(fileName) && File.Exists(fileName))
                dir = Path.GetDirectoryName(fileName);
        }
        catch (System.Exception)
        {
            dir = null; // a name like "Drawing1.dwg" that is not a path
        }
        var name = string.IsNullOrWhiteSpace(doc.Name) ? "Drawing" : Path.GetFileName(doc.Name);
        return new DrawingContext(name, dir, insUnits, DrawingUnitFactor.FromInsUnits(insUnits));
    }
}
