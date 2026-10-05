using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

namespace HPRebar.KataRebar.Service;

/// <summary>
/// Marks a created bar: its host, Kata number and beam in extensible storage (<see cref="KataRebarStorage"/>), its
/// partition the beam name (when the sheet gives one) so the beam is numbered on its own. Comments and Schedule Mark
/// are left to the user; the Kata number goes to Rebar Number once every bar exists (<see cref="KataRebarNumberAssigner"/>).
/// </summary>
public static class KataRebarStamp
{
    public static void Apply(Rebar rebar, Element host, string beamName, int kataNumber)
    {
        KataRebarStorage.Write(rebar, host.UniqueId, kataNumber, beamName?.Trim() ?? "");

        if (!string.IsNullOrWhiteSpace(beamName))
        {
            var partition = rebar.get_Parameter(BuiltInParameter.NUMBER_PARTITION_PARAM);
            if (partition is { IsReadOnly: false }) partition.Set(beamName.Trim());
        }
    }
}
