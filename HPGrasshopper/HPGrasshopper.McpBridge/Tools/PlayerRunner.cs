namespace HPGrasshopper.McpBridge.Tools;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Serilog;

public sealed class PlayerRunner
{
    public object Run(
        string filePath, JsonElement? inputs = null, IReadOnlyList<string>? requestedOutputs = null,
        bool headless = true, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!File.Exists(filePath))
            return new { success = false, error = $"Definition file not found: {filePath}" };

        var sw = Stopwatch.StartNew();
        if (!TryReadArchive(filePath, out var archive, out var archiveError))
            return new { success = false, error = archiveError };

        GH_Document? doc = null;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            doc = new GH_Document();
            if (!archive.ExtractObject(doc, "Definition"))
                return new { success = false, error = "Failed to extract definition graph from archive." };

            var (appliedCount, rejectedInputs) = ApplyInputs(doc, inputs);
            if (rejectedInputs.Count > 0)
                return BuildRejectedInputsResult(sw, appliedCount, rejectedInputs);

            cancellationToken.ThrowIfCancellationRequested();
            doc.NewSolution(true);
            cancellationToken.ThrowIfCancellationRequested();

            var validationResult = ValidateSolutionState(doc, sw, requestedOutputs, appliedCount);
            if (validationResult != null) return validationResult;

            var results = ExtractOutputs(doc.Objects, requestedOutputs);
            return BuildSuccessResult(sw, appliedCount, results);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Error(ex, "PlayerRunner: Execution failed for {Path}", filePath);
            return new { success = false, error = $"{ex.GetType().Name}: {ex.Message}" };
        }
        finally { DisposeDocument(doc, headless); }
    }

    private static object BuildRejectedInputsResult(Stopwatch sw, int appliedCount, List<PlayerInputFailure> rejected)
    {
        sw.Stop();
        return new
        {
            success = false,
            durationMs = sw.ElapsedMilliseconds,
            inputsApplied = appliedCount,
            rejectedInputs = rejected,
            error = $"Failed to apply {rejected.Count} input(s): {string.Join("; ", rejected.Select(r => $"{r.Name}: {r.Reason}"))}"
        };
    }

    private static object BuildSuccessResult(Stopwatch sw, int appliedCount, Dictionary<string, object?> results)
    {
        sw.Stop();
        return new { success = true, durationMs = sw.ElapsedMilliseconds, inputsApplied = appliedCount, rejectedInputs = new List<PlayerInputFailure>(), outputs = results };
    }

    private static object? ValidateSolutionState(
        GH_Document doc,
        Stopwatch sw,
        IReadOnlyList<string>? requestedOutputs,
        int appliedCount)
    {
        var (errors, warnings) = CollectRuntimeDiagnostics(doc.Objects);
        if (errors.Count > 0)
        {
            sw.Stop();
            return new
            {
                success = false,
                durationMs = sw.ElapsedMilliseconds,
                inputsApplied = appliedCount,
                error = $"Solve failed with {errors.Count} runtime error(s).",
                errors,
                warnings
            };
        }

        if (requestedOutputs != null && requestedOutputs.Count > 0)
        {
            var outputs = ExtractOutputs(doc.Objects, requestedOutputs);
            foreach (var requested in requestedOutputs)
            {
                bool outputProduced = outputs.Any(pair =>
                    string.Equals(pair.Key, requested, StringComparison.OrdinalIgnoreCase) ||
                    pair.Key.StartsWith(requested + "#", StringComparison.OrdinalIgnoreCase) ||
                    (pair.Key.Contains('#') && pair.Key.EndsWith(requested[..Math.Min(8, requested.Length)], StringComparison.OrdinalIgnoreCase)));

                if (!outputProduced)
                {
                    sw.Stop();
                    return new
                    {
                        success = false,
                        durationMs = sw.ElapsedMilliseconds,
                        inputsApplied = appliedCount,
                        error = $"Requested output '{requested}' was not found or produced no data."
                    };
                }
            }
        }
        return null;
    }

    internal static (List<string> errors, List<string> warnings) CollectRuntimeDiagnostics(IEnumerable<IGH_DocumentObject> objects)
    {
        var errorList = new List<string>();
        var warningList = new List<string>();

        foreach (var obj in objects)
        {
            var errorsBefore = errorList.Count;
            if (obj is IGH_ActiveObject active)
            {
                var errs = active.RuntimeMessages(GH_RuntimeMessageLevel.Error);
                if (errs != null)
                    foreach (var err in errs) errorList.Add($"[{obj.NickName ?? obj.Name}] {err}");
                var warns = active.RuntimeMessages(GH_RuntimeMessageLevel.Warning);
                if (warns != null)
                    foreach (var warn in warns) warningList.Add($"[{obj.NickName ?? obj.Name}] {warn}");
            }

            if (obj is IGH_Component comp)
            {
                if (comp.Phase == GH_SolutionPhase.Failed && errorList.Count == errorsBefore)
                    errorList.Add($"[{comp.NickName ?? comp.Name}] Component execution failed (SolutionPhase.Failed).");

                if (comp.Params != null)
                {
                    CheckParamMessages(comp.Params.Input, comp, errorList, warningList);
                    CheckParamMessages(comp.Params.Output, comp, errorList, warningList);
                }
            }
        }
        return (errorList, warningList);
    }

    internal static List<string> CollectRuntimeMessages(IEnumerable<IGH_DocumentObject> objects) =>
        CollectRuntimeDiagnostics(objects).errors;

    private static void CheckParamMessages(
        IEnumerable<IGH_Param>? parameters,
        IGH_Component parentComp,
        List<string> errorList,
        List<string> warningList)
    {
        if (parameters == null) return;
        foreach (var param in parameters)
        {
            var prefix = $"[{parentComp.NickName ?? parentComp.Name} : {param.NickName ?? param.Name}]";
            var errs = param.RuntimeMessages(GH_RuntimeMessageLevel.Error);
            if (errs != null) foreach (var err in errs) errorList.Add($"{prefix} {err}");
            var warns = param.RuntimeMessages(GH_RuntimeMessageLevel.Warning);
            if (warns != null) foreach (var warn in warns) warningList.Add($"{prefix} {warn}");
        }
    }

    private static (int appliedCount, List<PlayerInputFailure> rejectedInputs) ApplyInputs(GH_Document doc, JsonElement? inputs)
    {
        if (!inputs.HasValue || inputs.Value.ValueKind != JsonValueKind.Object)
            return (0, new List<PlayerInputFailure>());

        var (staged, rejected) = StageAllInputs(doc, inputs.Value);
        if (rejected.Count > 0)
            return (0, rejected);

        return CommitStagedInputs(staged);
    }

    private static (List<(IGH_Param Param, string Name, IReadOnlyList<StagedItem> Items)> Staged, List<PlayerInputFailure> Rejected)
        StageAllInputs(GH_Document doc, JsonElement inputs)
    {
        var rejected = new List<PlayerInputFailure>();
        var staged = new List<(IGH_Param Param, string Name, IReadOnlyList<StagedItem> Items)>();

        foreach (var prop in inputs.EnumerateObject())
        {
            var targetParam = FindInputParam(doc.Objects, prop.Name, g => doc.FindObject(g, true));
            if (targetParam == null)
            {
                rejected.Add(new PlayerInputFailure(prop.Name, $"Parameter '{prop.Name}' was not found in the definition."));
                continue;
            }

            if (!PlayerDataTreeHelper.TryStageParamValue(targetParam, prop.Value, out var stagedItems, out var failureReason))
            {
                rejected.Add(new PlayerInputFailure(prop.Name, failureReason ?? "Failed staging parameter value: incompatible type or structure."));
                continue;
            }

            staged.Add((targetParam, prop.Name, stagedItems!));
        }

        return (staged, rejected);
    }

    private static (int appliedCount, List<PlayerInputFailure> rejectedInputs)
        CommitStagedInputs(List<(IGH_Param Param, string Name, IReadOnlyList<StagedItem> Items)> staged)
    {
        var applied = 0;
        var rejected = new List<PlayerInputFailure>();
        foreach (var (param, name, items) in staged)
        {
            try
            {
                if (PlayerDataTreeHelper.ApplyStagedItems(param, items, out var applyError))
                {
                    param.ExpireSolution(false);
                    applied++;
                }
                else
                {
                    rejected.Add(new PlayerInputFailure(name, applyError ?? "Parameter rejected input data."));
                }
            }
            catch (Exception ex)
            {
                rejected.Add(new PlayerInputFailure(name, $"Exception during assignment: {ex.Message}"));
            }
        }
        return (applied, rejected);
    }

    private static bool TryReadArchive(string filePath, out GH_Archive archive, out string? error)
    {
        archive = new GH_Archive();
        try
        {
            if (!archive.ReadFromFile(filePath))
            {
                error = "Failed to deserialize Grasshopper definition from file.";
                return false;
            }
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            error = $"Failed to deserialize Grasshopper definition from file: {ex.Message}";
            return false;
        }
    }

    internal static bool LoadDefinition(string filePath, out GH_Archive archive, out string? error) =>
        TryReadArchive(filePath, out archive, out error);

    internal static object? ExtractParamValue(IGH_Param param) =>
        PlayerOutputExtractor.ExtractParamData(param);

    internal static void DisposeDocument(IDisposable? doc, bool headless)
    {
        if (headless) { doc?.Dispose(); return; }
        if (doc is GH_Document ghDoc && Grasshopper.Instances.DocumentServer != null)
        {
            if (!Grasshopper.Instances.DocumentServer.Contains(ghDoc)) Grasshopper.Instances.DocumentServer.AddDocument(ghDoc);
            if (Grasshopper.Instances.ActiveCanvas != null) Grasshopper.Instances.ActiveCanvas.Document = ghDoc;
        }
    }

    internal static IGH_Param? FindInputParam(
        IEnumerable<IGH_DocumentObject> objects,
        string nameOrGuid,
        Func<Guid, IGH_DocumentObject?>? findById = null) =>
        PlayerDataTreeHelper.FindParam(objects, nameOrGuid, findById);

    internal static bool SetParamValue(IGH_Param param, JsonElement val) =>
        PlayerDataTreeHelper.SetParamValue(param, val);

    internal static Dictionary<string, object?> ExtractOutputs(
        IEnumerable<IGH_DocumentObject> objects,
        IReadOnlyList<string>? requestedOutputs) =>
        PlayerOutputExtractor.ExtractOutputs(objects, requestedOutputs);
}
