using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPRebar.Core.KataRebar.Calculators;
using Serilog;

namespace HPRebar.KataRebar.Service;

/// <summary>
/// Gives the bars Kata Rebar drew on <paramref name="hosts"/> their Kata number as Rebar Number. Revit numbers bars per
/// partition (the beam name) and gives identical bars one number; each such number is changed to the Kata number of
/// its bars (<see cref="KataRebarNumberSwap"/>). A number Revit refuses, or one it shares with bars Kata Rebar did not
/// draw, keeps a number of Revit's and is listed; a beam without a name is not numbered, its bars would share Revit's
/// default partition with the whole model.
/// </summary>
public static class KataRebarNumberAssigner
{
    /// <summary>Call inside an open transaction, once every bar exists. Returns one warning per number not written.</summary>
    public static IReadOnlyList<string> Apply(Document doc, IReadOnlyList<Element> hosts)
    {
        doc.Regenerate();
        var hostIds = new HashSet<string>(hosts.Select(h => h.UniqueId));
        var ours = new FilteredElementCollector(doc).OfClass(typeof(Rebar)).WhereElementIsNotElementType()
            .Select(r => (Element: r, Stored: KataRebarStorage.Read(r)))
            .Where(x => x.Stored is { } s && hostIds.Contains(s.HostUniqueId) && s.KataNumber > 0)
            .ToDictionary(x => x.Element.Id, x => x.Stored!.Value.KataNumber);
        if (ours.Count == 0) return new List<string>();

        var schema = NumberingSchema.GetNumberingSchema(doc, NumberingSchemaTypes.StructuralNumberingSchemas.Rebar);
        var warnings = new List<string>();
        var numbered = NumberedBars(doc).ToList();
        var partitions = numbered.Where(b => ours.ContainsKey(b.Element.Id)).Select(b => b.Partition).Distinct().ToList();
        if (partitions.Count == 0)
            Log.Warning("Kata Rebar: {Count} Kata bars but none has a Rebar Number Revit reports — nothing renumbered", ours.Count);
        foreach (var partition in partitions)
        {
            if (partition.Length == 0)
            {
                warnings.Add("Dầm không tên (ô B3 trống): không ghi Rebar Number — các thanh ở partition mặc định của cả model.");
                continue;
            }

            var groups = numbered.Where(b => b.Partition == partition).GroupBy(b => b.Number)
                .Select(g => new KataNumberGroup(
                    g.Key,
                    g.Where(b => ours.ContainsKey(b.Element.Id)).Select(b => ours[b.Element.Id]).Distinct().ToList(),
                    g.Count(b => !ours.ContainsKey(b.Element.Id))))
                .Where(g => g.Kata.Count > 0)
                .ToList();
            var result = KataRebarNumberSwap.Apply(partition, groups, new RevitNumbering(schema, partition));
            warnings.AddRange(result.Warnings);
            Log.Information("Kata Rebar: Rebar Number in partition {Partition}: {Count} numbers, Kata {Numbers}, {Changed} changed, {Warnings} warnings",
                partition, groups.Count, string.Join(",", groups.Select(g => g.Kata.Min()).Distinct().OrderBy(n => n)), result.Changed, result.Warnings.Count);
        }

        return warnings;
    }

    /// <summary>Every bar the rebar numbering schema numbers (free bars and bars of area or path systems).</summary>
    private static IEnumerable<(Element Element, string Partition, int Number)> NumberedBars(Document doc) =>
        new FilteredElementCollector(doc)
            .WherePasses(new ElementMulticlassFilter(new[] { typeof(Rebar), typeof(RebarInSystem) }))
            .WhereElementIsNotElementType()
            .Select(e => (Element: e, Partition: Text(e, BuiltInParameter.NUMBER_PARTITION_PARAM), Number: Number(e)))
            .Where(b => b.Number > 0);

    private static int Number(Element element) =>
        int.TryParse(Text(element, BuiltInParameter.REBAR_NUMBER), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) ? n : 0;

    private static string Text(Element element, BuiltInParameter id) => element.get_Parameter(id)?.AsString() ?? "";

    /// <summary>The rebar numbering of one partition; a renumber Revit refuses is logged and reported as false.</summary>
    private sealed class RevitNumbering : IKataNumberingTarget
    {
        private readonly NumberingSchema schema;
        private readonly string partition;

        public RevitNumbering(NumberingSchema schema, string partition)
        {
            this.schema = schema;
            this.partition = partition;
        }

        public bool IsUsed(int number) => schema.GetNumbers(partition).Any(r => number >= r.Low && number <= r.High);

        public int Highest() => schema.GetNumbers(partition).Select(r => r.High).DefaultIfEmpty(0).Max();

        public bool TryChange(int from, int to)
        {
            try
            {
                schema.ChangeNumber(partition, from, to);
                return true;
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException ex)
            {
                // Number held by bars Revit calls different, or an element this user cannot edit (workshared model).
                Log.Information("Kata Rebar: partition {Partition}: {From} → {To} refused: {Message}", partition, from, to, ex.Message);
                return false;
            }
        }
    }
}
