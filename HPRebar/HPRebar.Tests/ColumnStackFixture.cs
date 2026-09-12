using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.Revit.DB;

namespace HPRebar.Tests;

/// <summary>
///     Locates the sample model these tests run against and pulls named columns out of it.
///     See Fixtures/README.md for exactly what the model has to contain.
/// </summary>
internal static class ColumnStackFixture
{
    public const string FileName = "column-stack-2-storey.rvt";

    /// <summary>Full path of the sample model next to the test assembly, whether or not it exists.</summary>
    public static string Path =>
        System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(typeof(ColumnStackFixture).Assembly.Location)!,
            "Fixtures",
            FileName);

    public static bool Exists => File.Exists(Path);

    public static string MissingReason =>
        $"The sample model is not in the repository yet. Build it in Revit per Fixtures/README.md and save it as {Path}.";

    /// <summary>Columns carrying the given marks, ordered bottom to top.</summary>
    public static IReadOnlyList<Element> ColumnsMarked(Document document, params string[] marks)
    {
        var wanted = new HashSet<string>(marks);

        return new FilteredElementCollector(document)
            .OfCategory(BuiltInCategory.OST_StructuralColumns)
            .WhereElementIsNotElementType()
            .Where(column => wanted.Contains(MarkOf(column)))
            .OrderBy(column => column.get_BoundingBox(null).Min.Z)
            .ToList();
    }

    private static string MarkOf(Element element) =>
        element.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? string.Empty;
}
