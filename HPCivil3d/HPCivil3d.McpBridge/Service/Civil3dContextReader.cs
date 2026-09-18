using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.Civil.ApplicationServices;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Scripting;
using Serilog;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPCivil3d.McpBridge.Service;

/// <summary>
///     Builds the session snapshot the AI asks for before scripting. Runs on the main thread. The
///     Revit-shaped fields are filled with their AutoCAD equivalents (a layout is the "view", a layer is
///     the "category"); what has no Revit counterpart goes into <see cref="ContextResult.Autocad"/>, and
///     what only Civil 3D has — the product, the Civil drawing unit and zone, coarse object counts — into
///     <see cref="ContextResult.Civil3d"/>. Counts come from the id collections, no object is opened.
/// </summary>
public static class Civil3dContextReader
{
    public static ContextResult Read(bool includeSelection, bool executionEnabled, string hostVersion)
    {
        var documents = AcadApp.DocumentManager;
        var doc = documents.MdiActiveDocument;

        var result = new ContextResult
        {
            RevitVersion = hostVersion,
            Host = PipeNaming.Civil3dHost,
            HostVersion = hostVersion,
            ExecutionEnabled = executionEnabled,
            OpenDocs = documents.Cast<Document>().Select(d => d.Name).ToArray(),
        };

        if (doc is null) return result;

        var db = doc.Database;
        var quiescent = AcadApp.IsQuiescent;
        var insunits = (int)db.Insunits;
        // civil-only: begin
        var civil = Civil3dDocumentAccess.TryGetActive();
        var units = Civil3dUnits.For(civil, db, out var drawingUnit, out var insunitsMismatch);
        // civil-only: end
        var currentLayout = Safe(() => LayoutManager.Current.CurrentLayout);

        result.DocTitle = doc.Name;
        result.DocPath = doc.IsNamedDrawing ? db.Filename : null;
        result.IsReadOnly = doc.IsReadOnly;
        result.IsModifiable = !doc.IsReadOnly && quiescent;
        result.Units = new UnitsInfo(units.Label);
        result.ActiveView = new ViewInfo(db.CurrentSpaceId.Handle.Value, currentLayout ?? (db.TileMode ? "Model" : "Layout"), db.TileMode ? "Model" : "Layout");
        result.Autocad = new AutocadInfo(
            Insunits: AutocadInsunits.LabelFor(insunits),
            Measurement: db.Measurement.ToString(),
            CurrentLayout: currentLayout,
            CurrentLayer: Safe(() => LayerName(db)),
            IsModelSpace: db.TileMode,
            IsQuiescent: quiescent,
            IsNamedDrawing: doc.IsNamedDrawing);
        // civil-only: begin
        result.Civil3d = ReadCivil(civil, drawingUnit, insunitsMismatch);
        // civil-only: end

        if (includeSelection) result.Selection = ReadSelection(doc);

        return result;
    }

    // civil-only: begin
    /// <summary>The Civil block of the context: product, document flag, unit, zone and one count per object family (id collections only).</summary>
    private static Civil3dInfo ReadCivil(CivilDocument? civil, string? drawingUnit, bool insunitsMismatch)
    {
        var product = Civil3dDocumentAccess.ProductName();
        if (civil is null) return new Civil3dInfo(product, false, null, null, false, 0, 0, 0, 0, 0, 0);

        return new Civil3dInfo(
            Product: product,
            IsCivilDocument: true,
            DrawingUnit: drawingUnit,
            CoordinateSystemCode: Civil3dUnits.ReadCoordinateSystemCode(civil),
            InsunitsMismatch: insunitsMismatch,
            AlignmentCount: Count(() => civil.GetAlignmentIds().Count),
            SurfaceCount: Count(() => civil.GetSurfaceIds().Count),
            CorridorCount: Count(() => civil.CorridorCollection.Count),
            PipeNetworkCount: Count(() => civil.GetPipeNetworkIds().Count),
            PressureNetworkCount: Count(() => CivilDocumentPressurePipesExtension.GetPressurePipeNetworkIds(civil).Count),
            CogoPointCount: Count(() => (int)civil.CogoPoints.Count));
    }

    private static int Count(Func<int> read)
    {
        try { return read(); }
        catch (Exception exception)
        {
            // a collection Civil refuses to enumerate in this state counts as empty, never fails the context
            Log.Debug(exception, "Civil count could not be read");
            return 0;
        }
    }
    // civil-only: end

    /// <summary>One line per selected entity: handle as the id, layer as the category, DXF name as the name.</summary>
    public static ElementInfo Describe(ObjectId id, Transaction transaction)
    {
        var dxfName = Safe(() => id.ObjectClass?.DxfName);
        string? layer = null;

        try
        {
            if (transaction.GetObject(id, OpenMode.ForRead, false) is Entity entity) layer = entity.Layer;
        }
        catch
        {
            // erased or otherwise unreadable: the handle and class are still worth reporting
        }

        return new ElementInfo(id.Handle.Value, layer, dxfName);
    }

    private static IReadOnlyList<ElementInfo> ReadSelection(Document doc)
    {
        PromptSelectionResult selection;
        try
        {
            selection = doc.Editor.SelectImplied();
        }
        catch
        {
            return Array.Empty<ElementInfo>();
        }

        if (selection.Status != PromptStatus.OK || selection.Value is null || selection.Value.Count == 0) return Array.Empty<ElementInfo>();

        using var transaction = doc.Database.TransactionManager.StartOpenCloseTransaction();
        var described = selection.Value.GetObjectIds().Select(id => Describe(id, transaction)).ToArray();
        transaction.Commit();
        return described;
    }

    private static string? LayerName(Database db)
    {
        using var transaction = db.TransactionManager.StartOpenCloseTransaction();
        var name = (transaction.GetObject(db.Clayer, OpenMode.ForRead) as LayerTableRecord)?.Name;
        transaction.Commit();
        return name;
    }

    private static string? Safe(Func<string?> read)
    {
        try { return read(); }
        catch { return null; }
    }
}
