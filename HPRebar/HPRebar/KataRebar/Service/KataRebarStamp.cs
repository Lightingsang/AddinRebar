using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.KataRebar.Service;

/// <summary>
/// Marks a created bar: its comment carries the host tag the next run deletes by, its partition the
/// beam name (when the sheet gives one) and its schedule mark Kata's bar number (the plan's internal mark when the
/// bar has none).
/// </summary>
public static class KataRebarStamp
{
    /// <summary>The schedule mark of a bar: its Kata number, else its internal mark.</summary>
    public static string Mark(int barNumber, string barMark) => barNumber > 0 ? barNumber.ToString(System.Globalization.CultureInfo.InvariantCulture) : barMark;

    public static void Apply(Rebar rebar, Element host, string beamName, string barMark)
    {
        Set(rebar, BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS, KataRebarTag.ForHost(host.UniqueId));

        if (!string.IsNullOrWhiteSpace(beamName))
            Set(rebar, BuiltInParameter.NUMBER_PARTITION_PARAM, beamName.Trim());

        if (!string.IsNullOrWhiteSpace(barMark))
            Set(rebar, BuiltInParameter.REBAR_ELEM_SCHEDULE_MARK, barMark);
    }

    private static void Set(Element element, BuiltInParameter id, string value)
    {
        var parameter = element.get_Parameter(id);
        if (parameter is { IsReadOnly: false }) parameter.Set(value);
    }
}
