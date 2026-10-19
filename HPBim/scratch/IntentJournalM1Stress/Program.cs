namespace IntentJournalM1Stress;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using HPBim.Client.Connection;
using HPBim.Core.Execution;

public static class Program
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static async Task<int> Main(string[] args)
    {
        if (args.Length >= 3 && args[0] == "worker-append")
        {
            return await RunWorkerAppendAsync(args[1], args[2]);
        }

        if (args.Length >= 4 && args[0] == "worker-update")
        {
            return await RunWorkerUpdateAsync(args[1], args[2], args[3]);
        }

        return await RunOrchestratorAsync();
    }

    private static async Task<int> RunWorkerAppendAsync(string journalPath, string guidFilePath)
    {
        var lines = await File.ReadAllLinesAsync(guidFilePath);
        using var journal = new IntentJournal(journalPath);
        foreach (var line in lines)
        {
            if (Guid.TryParse(line.Trim(), out var guid))
            {
                var record = new IntentJournalRecord
                {
                    OperationId = guid,
                    PlanHash = $"plan-{guid:N}",
                    Status = IntentStatus.InFlight,
                    ItemCount = 10,
                    TargetDoc = new DocumentIdentitySnapshot { Title = "MultiProcTest.rvt" }
                };
                await journal.RecordIntentAsync(record);
            }
        }
        return 0;
    }

    private static async Task<int> RunWorkerUpdateAsync(string journalPath, string guidFilePath, string statusStr)
    {
        var status = Enum.Parse<IntentStatus>(statusStr, ignoreCase: true);
        var lines = await File.ReadAllLinesAsync(guidFilePath);
        using var journal = new IntentJournal(journalPath);
        foreach (var line in lines)
        {
            if (Guid.TryParse(line.Trim(), out var guid))
            {
                await journal.UpdateStatusAsync(guid, status);
            }
        }
        return 0;
    }

    private static async Task<int> RunOrchestratorAsync()
    {
        Console.WriteLine("================================================================================");
        Console.WriteLine("Milestone 1 Empirical Challenger — Multi-Process & Stress Verification Harness");
        Console.WriteLine("================================================================================\n");

        int failed = 0;
        string tempDir = Path.Combine(Path.GetTempPath(), "hpbim_m1_adv_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            failed += await RunTestAsync("M1-ADV-1: 2 Separate OS Processes Writing 1,000 Unique Intents (500 each)",
                () => TestMultiProcessWriting1000IntentsAsync(tempDir));

            failed += await RunTestAsync("M1-ADV-2: 10 Concurrent Instances Storming 1,000 Records with Updates",
                () => TestMultiInstance10WayStormAsync(tempDir));

            failed += await RunTestAsync("M1-ADV-3: High-Frequency Compaction Concurrently with Active Appends",
                () => TestConcurrentCompactionStressAsync(tempDir));

            failed += await RunTestAsync("M1-ADV-4: Rapid Interleaved Cross-Process Append & Update (200 records)",
                () => TestInterleavedCrossProcessAppendAndUpdateAsync(tempDir));
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            }
            catch (IOException)
            {
                // Teardown cleanup tolerance
            }
        }

        Console.WriteLine("\n================================================================================");
        if (failed == 0)
        {
            Console.WriteLine("ALL M1 ADVERSARIAL STRESS TESTS PASSED SUCCESSFULLY! (0 failures)");
            Console.WriteLine("================================================================================");
            return 0;
        }

        Console.WriteLine($"M1 ADVERSARIAL TESTS FAILED: {failed} failure(s) detected!");
        Console.WriteLine("================================================================================");
        return 1;
    }

    private static async Task TestMultiProcessWriting1000IntentsAsync(string tempDir)
    {
        string journalPath = Path.Combine(tempDir, "proc1000_journal.jsonl");
        string guidFile1 = Path.Combine(tempDir, "proc1_guids.txt");
        string guidFile2 = Path.Combine(tempDir, "proc2_guids.txt");

        var guids1 = Enumerable.Range(0, 500).Select(_ => Guid.NewGuid()).ToList();
        var guids2 = Enumerable.Range(0, 500).Select(_ => Guid.NewGuid()).ToList();
        var allExpected = new HashSet<Guid>(guids1.Concat(guids2));

        await File.WriteAllLinesAsync(guidFile1, guids1.Select(g => g.ToString("D")));
        await File.WriteAllLinesAsync(guidFile2, guids2.Select(g => g.ToString("D")));

        var exePath = Environment.ProcessPath ?? "dotnet";
        var isDotnet = !exePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || exePath.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase);
        var currentAssembly = typeof(Program).Assembly.Location;

        var proc1 = StartSubprocess(isDotnet, exePath, currentAssembly, $"worker-append \"{journalPath}\" \"{guidFile1}\"");
        var proc2 = StartSubprocess(isDotnet, exePath, currentAssembly, $"worker-append \"{journalPath}\" \"{guidFile2}\"");

        await Task.WhenAll(proc1.WaitForExitAsync(), proc2.WaitForExitAsync());

        if (proc1.ExitCode != 0 || proc2.ExitCode != 0)
        {
            string err1 = await proc1.StandardError.ReadToEndAsync();
            string err2 = await proc2.StandardError.ReadToEndAsync();
            throw new Exception($"Process exit codes failed: Proc1={proc1.ExitCode} ({err1}), Proc2={proc2.ExitCode} ({err2})");
        }

        string[] diskLines = await File.ReadAllLinesAsync(journalPath);
        if (diskLines.Length != 1000)
        {
            throw new Exception($"Expected 1,000 lines on disk, but found {diskLines.Length}");
        }

        using var cold = new IntentJournal(journalPath);
        var incomplete = await cold.GetIncompleteIntentsAsync();
        if (incomplete.Count != 1000)
        {
            throw new Exception($"Cold read expected 1,000 incomplete intents, got {incomplete.Count}");
        }

        var loadedGuids = incomplete.Select(r => r.OperationId).ToHashSet();
        if (!allExpected.SetEquals(loadedGuids))
        {
            throw new Exception("Mismatch in loaded GUIDs between expectations and disk contents!");
        }
    }

    private static async Task TestMultiInstance10WayStormAsync(string tempDir)
    {
        string journalPath = Path.Combine(tempDir, "storm10_journal.jsonl");
        const int instanceCount = 10;
        const int opsPerInstance = 100;
        var allExpected = new HashSet<Guid>();

        var tasks = Enumerable.Range(0, instanceCount).Select(async instIdx =>
        {
            using var journal = new IntentJournal(journalPath);
            var localGuids = Enumerable.Range(0, opsPerInstance).Select(_ => Guid.NewGuid()).ToList();
            lock (allExpected)
            {
                foreach (var g in localGuids) allExpected.Add(g);
            }

            foreach (var g in localGuids)
            {
                await journal.RecordIntentAsync(new IntentJournalRecord
                {
                    OperationId = g,
                    PlanHash = $"storm-{instIdx}",
                    Status = IntentStatus.Pending
                });
                await journal.UpdateStatusAsync(g, IntentStatus.InFlight);
                if (instIdx % 2 == 0)
                {
                    await journal.UpdateStatusAsync(g, IntentStatus.Committed);
                }
            }
        });

        await Task.WhenAll(tasks);

        using var cold = new IntentJournal(journalPath);
        int totalFound = 0;
        foreach (var g in allExpected)
        {
            var rec = await cold.GetIntentAsync(g);
            if (rec != null) totalFound++;
        }

        if (totalFound != instanceCount * opsPerInstance)
        {
            throw new Exception($"Storm test lost records! Expected {instanceCount * opsPerInstance}, got {totalFound}");
        }
    }

    private static async Task TestConcurrentCompactionStressAsync(string tempDir)
    {
        string journalPath = Path.Combine(tempDir, "compact_stress_journal.jsonl");
        using var writerJournal1 = new IntentJournal(journalPath);
        using var writerJournal2 = new IntentJournal(journalPath);
        using var compactorJournal = new IntentJournal(journalPath);

        var guids = Enumerable.Range(0, 200).Select(_ => Guid.NewGuid()).ToList();
        var allExpected = new HashSet<Guid>(guids);

        var writeTask1 = Task.Run(async () =>
        {
            foreach (var g in guids.Take(100))
            {
                await writerJournal1.RecordIntentAsync(new IntentJournalRecord { OperationId = g, Status = IntentStatus.InFlight });
            }
        });

        var writeTask2 = Task.Run(async () =>
        {
            foreach (var g in guids.Skip(100))
            {
                await writerJournal2.RecordIntentAsync(new IntentJournalRecord { OperationId = g, Status = IntentStatus.InFlight });
            }
        });

        var compactTask = Task.Run(async () =>
        {
            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(5);
                await compactorJournal.PruneOldEntriesAsync(TimeSpan.FromDays(1));
            }
        });

        await Task.WhenAll(writeTask1, writeTask2, compactTask);

        using var cold = new IntentJournal(journalPath);
        var incomplete = await cold.GetIncompleteIntentsAsync();
        if (incomplete.Count != 200)
        {
            throw new Exception($"Expected 200 records surviving concurrent compaction, got {incomplete.Count}");
        }
    }

    private static async Task TestInterleavedCrossProcessAppendAndUpdateAsync(string tempDir)
    {
        string journalPath = Path.Combine(tempDir, "interleaved_journal.jsonl");
        string guidFile = Path.Combine(tempDir, "interleaved_guids.txt");
        var guids = Enumerable.Range(0, 200).Select(_ => Guid.NewGuid()).ToList();
        await File.WriteAllLinesAsync(guidFile, guids.Select(g => g.ToString("D")));

        var exePath = Environment.ProcessPath ?? "dotnet";
        var isDotnet = !exePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || exePath.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase);
        var currentAssembly = typeof(Program).Assembly.Location;

        var procAppend = StartSubprocess(isDotnet, exePath, currentAssembly, $"worker-append \"{journalPath}\" \"{guidFile}\"");
        await Task.Delay(20);
        var procUpdate = StartSubprocess(isDotnet, exePath, currentAssembly, $"worker-update \"{journalPath}\" \"{guidFile}\" Committed");

        await Task.WhenAll(procAppend.WaitForExitAsync(), procUpdate.WaitForExitAsync());

        if (procAppend.ExitCode != 0 || procUpdate.ExitCode != 0)
        {
            string errA = await procAppend.StandardError.ReadToEndAsync();
            string errU = await procUpdate.StandardError.ReadToEndAsync();
            throw new Exception($"Interleaved process failure: Append={procAppend.ExitCode} ({errA}), Update={procUpdate.ExitCode} ({errU})");
        }

        using var cold = new IntentJournal(journalPath);
        foreach (var g in guids)
        {
            var rec = await cold.GetIntentAsync(g);
            if (rec == null)
            {
                throw new Exception($"Record {g} lost during interleaved cross-process run!");
            }
        }
    }

    private static Process StartSubprocess(bool isDotnet, string exePath, string assemblyPath, string arguments)
    {
        string hostExe = Path.ChangeExtension(assemblyPath, ".exe");
        string runtimeConfig = Path.ChangeExtension(assemblyPath, ".runtimeconfig.json");

        var psi = new ProcessStartInfo
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };

        if (File.Exists(hostExe))
        {
            psi.FileName = hostExe;
            psi.Arguments = arguments;
        }
        else
        {
            psi.FileName = exePath;
            psi.Arguments = $"exec --runtimeconfig \"{runtimeConfig}\" \"{assemblyPath}\" {arguments}";
        }

        var proc = Process.Start(psi) ?? throw new InvalidOperationException("Failed to launch subprocess");
        return proc;
    }

    private static async Task<int> RunTestAsync(string testName, Func<Task> testActionAsync)
    {
        Console.Write($"[TEST] {testName} ... ");
        try
        {
            await testActionAsync();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("PASS");
            Console.ResetColor();
            return 0;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"FAIL: {ex.Message}");
            Console.ResetColor();
            Console.WriteLine($"      StackTrace: {ex.StackTrace}");
            return 1;
        }
    }
}
