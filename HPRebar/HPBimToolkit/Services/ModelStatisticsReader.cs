using System.Diagnostics;
using Autodesk.Revit.UI;
using HPBimToolkit.Core.Contracts;
using HPBimToolkit.Core.Statistics;
using RevitView = Autodesk.Revit.DB.View;

namespace HPBimToolkit.Services;

/// <summary>
///     Counts instance elements per category in the requested scope with ONE collector pass, plus model-wide counts
///     that Revit answers without walking elements (<c>GetElementCount</c>). Runs only when the page asks for it.
/// </summary>
public static class ModelStatisticsReader
{
    /// <summary>How often the walk checks whether the page still wants the answer (the walk blocks Revit's UI thread).</summary>
    private const int CancellationCheckInterval = 2000;

    private static readonly HashSet<BuiltInCategory> NotModelContent =
    [
        BuiltInCategory.OST_Materials,
        BuiltInCategory.OST_ProjectInformation,
        BuiltInCategory.OST_Sheets,
        BuiltInCategory.OST_ColorFillSchema,
        BuiltInCategory.OST_PipeSegments,
    ];

    public static ModelStatistics Read(UIApplication app, StatisticsScope scope, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var uiDocument = ActiveDocument.Require(app);
        var document = uiDocument.Document;

        var tally = new CategoryTally();
        var isCountedById = new Dictionary<long, bool>();
        var walked = 0;
        foreach (var element in ScopeCollector.Instances(uiDocument, scope))
        {
            if (++walked % CancellationCheckInterval == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (element.Category is not { } category)
            {
                continue;
            }

            var categoryId = category.Id.Value;
            if (!isCountedById.TryGetValue(categoryId, out var isCounted))
            {
                isCounted = IsCounted(category);
                isCountedById[categoryId] = isCounted;
            }

            if (isCounted)
            {
                tally.Add(categoryId, () => category.Name);
            }
        }

        return new ModelStatistics(
            DocumentSnapshot.KeyOf(document.PathName, document.Title),
            scope == StatisticsScope.ActiveView ? uiDocument.ActiveView.Id.Value : null,
            scope,
            tally.Total,
            tally.ToCounts(),
            ReadMetrics(document),
            stopwatch.ElapsedMilliseconds);
    }

    /// <summary>
    ///     Model and annotation categories a user sees in Visibility/Graphics. Hidden categories (sketches, constraints,
    ///     analytical nodes), subcategories and the non-graphical bookkeeping in <see cref="NotModelContent" /> are left out;
    ///     views and sheets are counted in the metrics instead. Decided once per category, not per element.
    /// </summary>
    private static bool IsCounted(Category category) =>
        category.IsVisibleInUI &&
        category.Parent is null &&
        category.CategoryType is CategoryType.Model or CategoryType.Annotation &&
        !NotModelContent.Contains(category.BuiltInCategory);

    private static ModelMetrics ReadMetrics(Document document) => new(
        ElementTypes: new FilteredElementCollector(document).WhereElementIsElementType().GetElementCount(),
        Views: new FilteredElementCollector(document).OfClass(typeof(RevitView)).Cast<RevitView>().Count(view => !view.IsTemplate),
        Sheets: new FilteredElementCollector(document).OfClass(typeof(ViewSheet)).GetElementCount(),
        Levels: new FilteredElementCollector(document).OfClass(typeof(Level)).GetElementCount(),
        Families: new FilteredElementCollector(document).OfClass(typeof(Family)).GetElementCount(),
        Warnings: document.GetWarnings().Count,
        Links: new FilteredElementCollector(document).OfClass(typeof(RevitLinkInstance)).GetElementCount(),
        Worksets: document.IsWorkshared
            ? new FilteredWorksetCollector(document).OfKind(WorksetKind.UserWorkset).ToWorksets().Count
            : null);
}
