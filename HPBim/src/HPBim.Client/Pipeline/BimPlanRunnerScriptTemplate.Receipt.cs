namespace HPBim.Client.Pipeline;

public static partial class BimPlanRunnerScriptTemplate
{
    private static string GetReceiptScript() =>
        GetReceiptHeaderAndSchemaScript() +
        GetReceiptStorageScript() +
        GetBatchRollbackCheckScript();

    private static string GetReceiptHeaderAndSchemaScript() => """
        // -------------------------------------------------------------------------
        // 3. Extensible Storage Receipt Writing (Atomic Commit)
        // -------------------------------------------------------------------------
        bool dryRun = args.Obj("options").Bool("dryRun", false);
        string planHash = args.Str("planHash", string.Empty);
        string opIdStr = args.Obj("options").Str("operationId", string.Empty);
        Guid operationId = Guid.TryParse(opIdStr, out var parsedOp)
            ? parsedOp
            : (Guid.TryParse(args.Str("planId", string.Empty), out var parsedPId) ? parsedPId : Guid.NewGuid());

        bool hasBatchErrors = records.Any(r => (string)r["status"] == "Failed");

        if (!dryRun && !string.IsNullOrEmpty(planHash))
        {
            if (!hasBatchErrors)
            {
                var schemaGuid = new Guid("D4E5F6A7-8B9C-0D1E-2F3A-4B5C6D7E8F90");
                var schema = Autodesk.Revit.DB.ExtensibleStorage.Schema.Lookup(schemaGuid);
                if (schema == null)
                {
                    var builder = new Autodesk.Revit.DB.ExtensibleStorage.SchemaBuilder(schemaGuid);
                    builder.SetSchemaName("HPBimCoordinationReceipt");
                    builder.SetDocumentation("HPBim: Execution receipt tracking atomic commit of BIM execution plan.");
                    builder.SetReadAccessLevel(Autodesk.Revit.DB.ExtensibleStorage.AccessLevel.Public);
                    builder.SetWriteAccessLevel(Autodesk.Revit.DB.ExtensibleStorage.AccessLevel.Public);
                    builder.AddSimpleField("OperationId", typeof(Guid));
                    builder.AddSimpleField("PlanHash", typeof(string));
                    builder.AddSimpleField("CommittedAtUtc", typeof(string));
                    builder.AddSimpleField("ElementMapJson", typeof(string));
                    schema = builder.Finish();
                }

        """;

    private static string GetReceiptStorageScript() => """
                var elemMap = new Dictionary<string, string>();
                foreach (var r in records)
                {
                    string sk = r.ContainsKey("sourceKey") ? (string)r["sourceKey"] : string.Empty;
                    string uid = r.ContainsKey("targetUniqueId") ? (string)r["targetUniqueId"] : string.Empty;
                    string st = r.ContainsKey("status") ? (string)r["status"] : string.Empty;
                    if (!string.IsNullOrEmpty(sk) && !string.IsNullOrEmpty(uid) &&
                        (st == "Created" || st == "Updated" || st == "NoOp"))
                    {
                        elemMap[sk] = uid;
                    }
                }

                string elemMapJson = System.Text.Json.JsonSerializer.Serialize(elemMap);

                Autodesk.Revit.DB.ExtensibleStorage.DataStorage receiptDs = null;
                var allDs = new FilteredElementCollector(doc)
                    .OfClass(typeof(Autodesk.Revit.DB.ExtensibleStorage.DataStorage))
                    .Cast<Autodesk.Revit.DB.ExtensibleStorage.DataStorage>();
                foreach (var ds in allDs)
                {
                    var ent = ds.GetEntity(schema);
                    if (ent != null && ent.IsValid() && ent.Get<Guid>("OperationId") == operationId)
                    {
                        receiptDs = ds;
                        break;
                    }
                }

                if (receiptDs == null)
                {
                    receiptDs = Autodesk.Revit.DB.ExtensibleStorage.DataStorage.Create(doc);
                }

                var entity = new Autodesk.Revit.DB.ExtensibleStorage.Entity(schema);
                entity.Set<Guid>("OperationId", operationId);
                entity.Set<string>("PlanHash", planHash);
                entity.Set<string>("CommittedAtUtc", DateTime.UtcNow.ToString("o"));
                entity.Set<string>("ElementMapJson", elemMapJson);
                receiptDs.SetEntity(entity);
            }
        }
        """;

    private static string GetBatchRollbackCheckScript() => """
        if (hasBatchErrors)
        {
            int failCount = records.Count(r => (string)r["status"] == "Failed");
            var firstFailed = records.First(r => (string)r["status"] == "Failed");
            string errCode = firstFailed.ContainsKey("errorCode") ? (string)firstFailed["errorCode"] : "ERR_BATCH_ITEM_FAILED";
            string errReason = firstFailed.ContainsKey("errorReason") ? (string)firstFailed["errorReason"] : "Item failed";
            string payloadJson = System.Text.Json.JsonSerializer.Serialize(records);
            throw new InvalidOperationException($"[BATCH_TRANSACTION_FAILED] Execution failed for {failCount} element(s). First failure: [{errCode}] {errReason} ||PAYLOAD:{payloadJson}");
        }
        """;
}
