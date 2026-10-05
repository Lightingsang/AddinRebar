using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;

namespace HPRebar.KataRebar.Service;

/// <summary>
/// What Kata Rebar keeps on each bar it draws, out of the user's sight (extensible storage, not a parameter): the
/// host beam's unique id — the next run deletes the bars of its beams by it, never a bar drawn by hand —, Kata's bar
/// number and the beam's name.
/// </summary>
internal static class KataRebarStorage
{
    private static readonly Guid SchemaId = new("6F2C8B4E-1A7D-4C3B-9E5F-2D8A0B6C4E71");
    private const string HostField = "HostUniqueId";
    private const string NumberField = "KataNumber";
    private const string BeamField = "Beam";

    public static void Write(Element element, string hostUniqueId, int kataNumber, string beamName)
    {
        var entity = new Entity(SchemaOrBuild());
        entity.Set(HostField, hostUniqueId);
        entity.Set(NumberField, kataNumber);
        entity.Set(BeamField, beamName ?? "");
        element.SetEntity(entity);
    }

    /// <summary>The host and Kata number a bar was drawn with; null for a bar Kata Rebar did not draw.</summary>
    public static (string HostUniqueId, int KataNumber)? Read(Element element)
    {
        var schema = Schema.Lookup(SchemaId);
        if (schema is null) return null;
        var entity = element.GetEntity(schema);
        if (entity is null || !entity.IsValid()) return null;
        return (entity.Get<string>(HostField), entity.Get<int>(NumberField));
    }

    private static Schema SchemaOrBuild()
    {
        var existing = Schema.Lookup(SchemaId);
        if (existing is not null) return existing;

        var builder = new SchemaBuilder(SchemaId);
        builder.SetSchemaName("HPRebarKataRebar");
        builder.SetDocumentation("Kata Rebar: host beam, Kata bar number and beam name of a bar it drew.");
        builder.SetReadAccessLevel(AccessLevel.Public);
        builder.SetWriteAccessLevel(AccessLevel.Public);
        builder.AddSimpleField(HostField, typeof(string));
        builder.AddSimpleField(NumberField, typeof(int));
        builder.AddSimpleField(BeamField, typeof(string));
        return builder.Finish();
    }
}
