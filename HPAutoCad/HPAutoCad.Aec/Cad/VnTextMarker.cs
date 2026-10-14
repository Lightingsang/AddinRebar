using Autodesk.AutoCAD.DatabaseServices;

namespace HPAutoCad.Aec.Cad;

/// <summary>
///     The XData flag a converted entity carries. Converted text is real Unicode, but Vietnamese words made only of Latin-1 letters
///     (<c>BÊ TÔNG</c>, <c>Cà phê</c>) look exactly like legacy bytes — run the conversion again while their style still names a legacy font
///     and they are destroyed. The flag is the only proof that survives a save; <c>U</c> and a change-set rollback remove it with the text.
/// </summary>
public static class VnTextMarker
{
    public const string AppName = "HP_VNCONVERT";

    public static bool IsMarked(Entity entity)
    {
        using var data = entity.GetXDataForApplication(AppName);
        return data is not null;
    }

    /// <summary>Registers the application name once per drawing; call before <see cref="Mark"/> in the write phase.</summary>
    public static void EnsureRegistered(Database db, Transaction tr)
    {
        var table = (RegAppTable)tr.GetObject(db.RegAppTableId, OpenMode.ForRead);
        if (table.Has(AppName)) return;
        table.UpgradeOpen();
        var record = new RegAppTableRecord { Name = AppName };
        table.Add(record);
        tr.AddNewlyCreatedDBObject(record, true);
    }

    /// <summary>Replaces only this application's XData on a write-open entity; other applications' data stays.</summary>
    public static void Mark(Entity entity)
    {
        using var data = new ResultBuffer(new TypedValue((int)DxfCode.ExtendedDataRegAppName, AppName), new TypedValue((int)DxfCode.ExtendedDataAsciiString, "unicode"));
        entity.XData = data;
    }
}
