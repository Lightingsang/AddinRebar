namespace HPBim.Client.Recovery;

/// <summary>
/// Roslyn script template executed in Revit under TransactionModes.None to query coordination receipts.
/// </summary>
public static class ExtensibleStorageQueryScriptTemplate
{
    public static string GetQueryScript() =>
        GetQueryHeaderScript() +
        GetQueryFilterAndReturnScript();

    private static string GetQueryHeaderScript() => """
        var schemaGuid = new Guid("D4E5F6A7-8B9C-0D1E-2F3A-4B5C6D7E8F90");
        var schema = Autodesk.Revit.DB.ExtensibleStorage.Schema.Lookup(schemaGuid);
        if (schema == null)
        {
            return new { count = 0, receipts = new List<object>() };
        }

        string targetOpId = args.Str("targetOperationId", string.Empty);
        string targetPlanHash = args.Str("targetPlanHash", string.Empty);

        var collector = new FilteredElementCollector(doc)
            .OfClass(typeof(Autodesk.Revit.DB.ExtensibleStorage.DataStorage));

        var receipts = new List<object>();

        """;

    private static string GetQueryFilterAndReturnScript() => """
        var docVer = Document.GetDocumentVersion(doc);
        string currentTitle = doc.Title ?? string.Empty;
        string currentPath = string.IsNullOrEmpty(doc.PathName) ? string.Empty : doc.PathName;
        string currentVerGuid = docVer != null ? docVer.VersionGUID.ToString("D") : Guid.Empty.ToString("D");
        string currentCentGuid = (doc.IsWorkshared ? doc.WorksharingCentralGUID : Guid.Empty).ToString("D");

        foreach (Autodesk.Revit.DB.ExtensibleStorage.DataStorage ds in collector)
        {
            var entity = ds.GetEntity(schema);
            if (entity == null || !entity.IsValid()) continue;

            Guid opId = entity.Get<Guid>("OperationId");
            string planHash = entity.Get<string>("PlanHash") ?? string.Empty;
            string committedUtc = entity.Get<string>("CommittedAtUtc") ?? string.Empty;
            string elemMapJson = entity.Get<string>("ElementMapJson") ?? "{}";

            if (!string.IsNullOrEmpty(targetOpId) && Guid.TryParse(targetOpId, out var parsedOp) && opId != parsedOp)
                continue;

            if (!string.IsNullOrEmpty(targetPlanHash) && !string.Equals(planHash, targetPlanHash, StringComparison.OrdinalIgnoreCase))
                continue;

            receipts.Add(new
            {
                operationId = opId.ToString("D"),
                planHash = planHash,
                targetDocId = currentTitle,
                targetDocTitle = currentTitle,
                targetDocPath = currentPath,
                targetVersionGuid = currentVerGuid,
                targetCentralGuid = currentCentGuid,
                committedAtUtc = committedUtc,
                elementMapJson = elemMapJson
            });
        }

        return new { count = receipts.Count, receipts = receipts };
        """;
}
