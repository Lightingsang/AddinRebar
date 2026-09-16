using System.Globalization;
using Autodesk.AutoCAD.DatabaseServices;
using HPAutoCad.Aec.Model;

namespace HPAutoCad.Aec.Cad;

/// <summary>
///     Handle text → <see cref="ObjectId"/>, with the failure classified so a tool can report it. Handles
///     are the only identity the API exposes: they survive sessions and file saves, ObjectIds do not.
/// </summary>
public static class HandleResolver
{
    /// <summary>Normalised upper-case hex without a leading 0x; null when the text is not a handle at all.</summary>
    public static string? Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var t = text.Trim();
        if (t.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) t = t[2..];
        return long.TryParse(t, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value) && value > 0 ? value.ToString("X") : null;
    }

    /// <summary>The entity's id, or an error naming why the handle cannot be used. Never throws.</summary>
    public static ObjectId Resolve(Database db, string? handleText, out ToolError? error)
    {
        var handle = Normalize(handleText);
        if (handle is null)
        {
            error = ToolError.ForHandle(ToolErrorCode.InvalidHandle, handleText ?? "", $"'{handleText}' is not a valid handle (hex digits expected).");
            return ObjectId.Null;
        }

        var value = long.Parse(handle, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        ObjectId id;
        try
        {
            if (!db.TryGetObjectId(new Handle(value), out id) || id.IsNull)
            {
                error = ToolError.ForHandle(ToolErrorCode.InvalidHandle, handle, $"Entity handle {handle} was not found in the drawing.");
                return ObjectId.Null;
            }
        }
        catch (Autodesk.AutoCAD.Runtime.Exception exception)
        {
            error = ToolError.ForHandle(ToolErrorCode.InvalidHandle, handle, $"Entity handle {handle} could not be resolved: {exception.ErrorStatus}.");
            return ObjectId.Null;
        }

        if (id.IsErased)
        {
            error = ToolError.ForHandle(ToolErrorCode.Erased, handle, $"Entity handle {handle} is erased.");
            return ObjectId.Null;
        }

        error = null;
        return id;
    }

    /// <summary>Resolves and opens the entity for read through the caller's transaction; non-entities (layers, blocks, dictionaries) are refused.</summary>
    public static Entity? OpenEntity(Database db, Transaction tr, string? handleText, out ToolError? error)
    {
        var id = Resolve(db, handleText, out error);
        if (id.IsNull) return null;
        try
        {
            if (tr.GetObject(id, OpenMode.ForRead, false) is Entity entity) return entity;
        }
        catch (Autodesk.AutoCAD.Runtime.Exception exception)
        {
            error = ToolError.ForHandle(ToolErrorCode.InvalidHandle, id.Handle.ToString(), $"Entity {id.Handle} could not be opened: {exception.ErrorStatus}.");
            return null;
        }

        error = ToolError.ForHandle(ToolErrorCode.NotAnEntity, id.Handle.ToString(), $"Handle {id.Handle} is a {id.ObjectClass.DxfName ?? id.ObjectClass.Name}, not an entity.");
        return null;
    }
}
