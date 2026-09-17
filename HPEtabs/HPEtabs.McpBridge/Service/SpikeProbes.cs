using System.Diagnostics;
using System.IO;
using ETABSv1;
using Serilog;

namespace HPEtabs.McpBridge.Service;

/// <summary>
///     The three probes the CHM cannot answer and a script is not allowed to run: whether <c>File.Save(path)</c>
///     re-points the model's current file name, what unlocking does to analysis results, and what happens when
///     the COM proxy is called from another apartment. Started from buttons in the bridge window by the user,
///     on a throw-away model, and written to the log; never reachable over the pipe. The two writing probes
///     are the only writes this bridge build performs.
/// </summary>
public static class SpikeProbes
{
    /// <summary>Save(tmp) → GetModelFilename → Save(original) → GetModelFilename. Runs on the STA worker.</summary>
    public static string SaveAsSemantics(cSapModel sapModel)
    {
        var original = sapModel.GetModelFilename(true);
        if (string.IsNullOrWhiteSpace(original) || !Path.IsPathRooted(original)) return "save-as probe skipped: the model has no file path — save it in ETABS first.";

        var tmp = Path.Combine(Path.GetDirectoryName(original)!, Path.GetFileNameWithoutExtension(original) + ".hpetabs-e9-probe.EDB");
        var sw = Stopwatch.StartNew();
        var retTmp = sapModel.File.Save(tmp);
        var afterTmp = sapModel.GetModelFilename(true);
        var saveTmpMs = sw.ElapsedMilliseconds;

        sw.Restart();
        var retBack = sapModel.File.Save(original);
        var afterBack = sapModel.GetModelFilename(true);
        var saveBackMs = sw.ElapsedMilliseconds;

        var renamed = !string.Equals(afterTmp, original, StringComparison.OrdinalIgnoreCase);
        var report = $"probe save-as: Save(tmp) ret={retTmp} in {saveTmpMs} ms → current name {(renamed ? "CHANGED to the tmp file (save-as re-points the model)" : "unchanged (save-as does not re-point)")}; " +
                     $"Save(original) ret={retBack} in {saveBackMs} ms → current name {(string.Equals(afterBack, original, StringComparison.OrdinalIgnoreCase) ? "back to the original" : "= " + Path.GetFileName(afterBack))}. " +
                     $"tmp file {(File.Exists(tmp) ? "exists (" + new FileInfo(tmp).Length / 1024 + " KB) — delete it by hand" : "was not written")}.";
        Log.Information(report);
        return report;
    }

    /// <summary>Case status before → SetModelIsLocked(false) → status after (and whether the lock reads back). Runs on the STA worker.</summary>
    public static string UnlockSemantics(cSapModel sapModel)
    {
        var lockedBefore = sapModel.GetModelIsLocked();
        var before = CaseStatus(sapModel);
        var sw = Stopwatch.StartNew();
        var ret = sapModel.SetModelIsLocked(false);
        var unlockMs = sw.ElapsedMilliseconds;
        var lockedAfter = sapModel.GetModelIsLocked();
        var after = CaseStatus(sapModel);

        var report = $"probe unlock: locked before={lockedBefore}, case status before=[{before}]; SetModelIsLocked(false) ret={ret} in {unlockMs} ms; locked after={lockedAfter}, case status after=[{after}] " +
                     $"({(before == after ? "results UNCHANGED by unlocking" : "results CHANGED by unlocking")}).";
        Log.Information(report);
        return report;
    }

    /// <summary>The same read from the STA worker and from a thread-pool (MTA) thread using the worker's proxy.</summary>
    public static string ApartmentSemantics(cSapModel sapModel)
    {
        string OnSta()
        {
            try { return "STA ok: " + sapModel.GetModelFilename(false); }
            catch (Exception exception) { return $"STA threw {exception.GetType().Name}: {exception.Message}"; }
        }

        var sta = OnSta();
        var mta = Task.Run(() =>
        {
            try { return $"MTA (apartment {Thread.CurrentThread.GetApartmentState()}) ok: " + sapModel.GetModelFilename(false); }
            catch (Exception exception) { return $"MTA threw {exception.GetType().Name} 0x{exception.HResult:X8}: {exception.Message}"; }
        }).GetAwaiter().GetResult();

        var report = $"probe apartment: {sta}; {mta}.";
        Log.Information(report);
        return report;
    }

    private static string CaseStatus(cSapModel sapModel)
    {
        try
        {
            int count = 0;
            string[]? names = null;
            int[]? status = null;
            var ret = sapModel.Analyze.GetCaseStatus(ref count, ref names, ref status);
            if (ret != 0 || names is null || status is null) return $"GetCaseStatus ret={ret}";
            return string.Join(", ", names.Zip(status, (n, s) => $"{n}={StatusName(s)}"));
        }
        catch (Exception exception)
        {
            return $"GetCaseStatus threw {exception.GetType().Name}";
        }
    }

    /// <summary>cAnalyze.GetCaseStatus codes: 1 not run, 2 could not start, 3 not finished, 4 finished.</summary>
    private static string StatusName(int status) => status switch { 1 => "not-run", 2 => "could-not-start", 3 => "not-finished", 4 => "finished", _ => status.ToString() };
}
