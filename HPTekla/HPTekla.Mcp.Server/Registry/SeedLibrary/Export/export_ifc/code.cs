string path = args.Str("outputFilePath");
if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("outputFilePath is required.");
string format = args.Str("format", "IFC4").ToUpperInvariant();
bool selectedOnly = args.Bool("exportSelectedOnly", false);

if (!path.EndsWith(".ifc", StringComparison.OrdinalIgnoreCase))
{
    path += ".ifc";
}

log($"Exporting Tekla model to {path} (Format: {format}, SelectedOnly: {selectedOnly})...");

var exportView = format == "IFC2X3"
    ? Tekla.Structures.Model.Operations.Operation.IFCExportViewTypeEnum.REFERENCE_VIEW
    : Tekla.Structures.Model.Operations.Operation.IFCExportViewTypeEnum.DESIGN_TRANSFER_VIEW;

bool ok = Tekla.Structures.Model.Operations.Operation.CreateIFC4ExportFromSelected(
    path,
    exportView,
    new List<string>(),
    Tekla.Structures.Model.Operations.Operation.ExportBasePoint.WORK_PLANE,
    "",
    "",
    new Tekla.Structures.Model.Operations.Operation.IFCExportFlags(),
    ""
);

if (!ok) throw new InvalidOperationException($"Tekla IFC export failed for output path: {path}");

log($"IFC Export successfully created: {path}");

return new
{
    success = true,
    filePath = path,
    format = format,
    selectedOnly = selectedOnly,
    summary = $"Exported IFC file to {path}"
};
