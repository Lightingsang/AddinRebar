using Autodesk.AutoCAD.DatabaseServices;
using HPAutoCad.Aec.Text;

namespace HPAutoCad.Aec.Cad;

/// <summary>One text style and what the font rules say about it.</summary>
public sealed record VnStyle(ObjectId Id, string Name, string Handle, string Face, string FileName, string BigFontFileName, bool IsDependent, VnFontInfo Font)
{
    /// <summary>The style's font as a person reads it: the TrueType face, else the SHX file.</summary>
    public string FontLabel => Face.Length > 0 ? Face : FileName;

    public bool IsShx => Face.Length == 0;
}

/// <summary>Every text style of the drawing, classified once per run; entities look their style up by id.</summary>
public sealed class VnStyleCatalog
{
    private readonly Dictionary<ObjectId, VnStyle> _byId = [];

    public VnStyleCatalog(Database db, Transaction tr, VnFontRules rules)
    {
        var table = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);
        foreach (var id in table)
        {
            var record = (TextStyleTableRecord)tr.GetObject(id, OpenMode.ForRead);
            if (record.IsShapeFile) continue; // shape (not text) styles hold no text
            var face = record.Font.TypeFace ?? "";
            var file = record.FileName ?? "";
            _byId[id] = new VnStyle(id, record.Name, record.Handle.ToString(), face, file, record.BigFontFileName ?? "", record.IsDependent, rules.Classify(face, file));
        }
    }

    public IReadOnlyCollection<VnStyle> All => _byId.Values;

    public VnStyle? Get(ObjectId id) => _byId.GetValueOrDefault(id);

    /// <summary>The font of a style id, or <see cref="VnFontInfo.Unknown"/> for a missing / null style.</summary>
    public VnFontInfo FontOf(ObjectId id) => Get(id)?.Font ?? VnFontInfo.Unknown;
}
