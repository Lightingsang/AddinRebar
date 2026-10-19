namespace HPBim.Client.Tests.Connection;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using HPBim.Client.Connection;
using HPBim.Core.Execution;
using Xunit;

public sealed class IntentJournalConcurrencyTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly string _tempDir;
    private readonly string _journalPath;

    public IntentJournalConcurrencyTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "hpbim_multiwriter_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _journalPath = Path.Combine(_tempDir, "intent_journal.jsonl");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }
        catch (IOException)
        {
            // Transient test teardown file lock cleanup skip
        }
    }

    [Fact]
    public async Task MultiWriter_TwoInstancesWriting1000UniqueIntents_ColdRestartRecoversAllWithoutLoss()
    {
        using var journal1 = new IntentJournal(_journalPath);
        using var journal2 = new IntentJournal(_journalPath);

        var instance1Guids = Enumerable.Range(0, 500).Select(_ => Guid.NewGuid()).ToList();
        var instance2Guids = Enumerable.Range(0, 500).Select(_ => Guid.NewGuid()).ToList();
        var allExpectedGuids = new HashSet<Guid>(instance1Guids.Concat(instance2Guids));
        Assert.Equal(1000, allExpectedGuids.Count);

        var task1 = Task.Run(async () =>
        {
            foreach (var id in instance1Guids)
            {
                var record = new IntentJournalRecord
                {
                    OperationId = id,
                    PlanHash = "hash-inst-1",
                    Status = IntentStatus.InFlight,
                    ItemCount = 3,
                    TargetDoc = new DocumentIdentitySnapshot { Title = "Project1.rvt" }
                };
                await journal1.RecordIntentAsync(record);
            }
        });

        var task2 = Task.Run(async () =>
        {
            foreach (var id in instance2Guids)
            {
                var record = new IntentJournalRecord
                {
                    OperationId = id,
                    PlanHash = "hash-inst-2",
                    Status = IntentStatus.InFlight,
                    ItemCount = 7,
                    TargetDoc = new DocumentIdentitySnapshot { Title = "Project2.rvt" }
                };
                await journal2.RecordIntentAsync(record);
            }
        });

        await Task.WhenAll(task1, task2);

        string[] lines = await File.ReadAllLinesAsync(_journalPath);
        Assert.Equal(1000, lines.Length);

        foreach (string line in lines)
        {
            Assert.False(string.IsNullOrWhiteSpace(line));
            var parsed = JsonSerializer.Deserialize<IntentJournalRecord>(line, JsonOptions);
            Assert.NotNull(parsed);
            Assert.Contains(parsed.OperationId, allExpectedGuids);
        }

        using var coldJournal = new IntentJournal(_journalPath);
        var loadedIncomplete = await coldJournal.GetIncompleteIntentsAsync();
        Assert.Equal(1000, loadedIncomplete.Count);

        var loadedGuids = loadedIncomplete.Select(r => r.OperationId).ToHashSet();
        Assert.Equal(1000, loadedGuids.Count);
        Assert.True(allExpectedGuids.SetEquals(loadedGuids));
    }

    [Fact]
    public async Task MultiWriter_InterleavedAppendsAndStatusUpdates_PreservesConsistencyAcrossInstances()
    {
        using var journal1 = new IntentJournal(_journalPath);
        using var journal2 = new IntentJournal(_journalPath);

        var guids1 = Enumerable.Range(0, 100).Select(_ => Guid.NewGuid()).ToList();
        var guids2 = Enumerable.Range(0, 100).Select(_ => Guid.NewGuid()).ToList();

        var task1 = Task.Run(async () =>
        {
            foreach (var id in guids1)
            {
                await journal1.RecordIntentAsync(new IntentJournalRecord
                {
                    OperationId = id,
                    Status = IntentStatus.InFlight,
                    PlanHash = "p1"
                });
                await journal1.UpdateStatusAsync(id, IntentStatus.Committed);
            }
        });

        var task2 = Task.Run(async () =>
        {
            foreach (var id in guids2)
            {
                await journal2.RecordIntentAsync(new IntentJournalRecord
                {
                    OperationId = id,
                    Status = IntentStatus.InFlight,
                    PlanHash = "p2"
                });
                await journal2.UpdateStatusAsync(id, IntentStatus.OutcomeUnknown, "Transient error");
            }
        });

        await Task.WhenAll(task1, task2);

        using var coldJournal = new IntentJournal(_journalPath);
        var incomplete = await coldJournal.GetIncompleteIntentsAsync();
        Assert.Equal(100, incomplete.Count);
        Assert.All(incomplete, r => Assert.Equal(IntentStatus.OutcomeUnknown, r.Status));

        foreach (var id in guids1)
        {
            var rec = await coldJournal.GetIntentAsync(id);
            Assert.NotNull(rec);
            Assert.Equal(IntentStatus.Committed, rec.Status);
        }
    }

    [Fact]
    public async Task MultiWriter_CacheRefreshedOnExternalAppendAndStatusUpdate_VisibleImmediately()
    {
        using var journal1 = new IntentJournal(_journalPath);
        using var journal2 = new IntentJournal(_journalPath);

        var opId = Guid.NewGuid();
        var initial = new IntentJournalRecord
        {
            OperationId = opId,
            PlanHash = "plan-cross-proc",
            Status = IntentStatus.InFlight,
            ItemCount = 4
        };

        await journal1.RecordIntentAsync(initial);

        var retrievedBy2 = await journal2.GetIntentAsync(opId);
        Assert.NotNull(retrievedBy2);
        Assert.Equal(IntentStatus.InFlight, retrievedBy2.Status);
        Assert.Equal("plan-cross-proc", retrievedBy2.PlanHash);

        await journal2.UpdateStatusAsync(opId, IntentStatus.Committed);

        var retrievedBy1 = await journal1.GetIntentAsync(opId);
        Assert.NotNull(retrievedBy1);
        Assert.Equal(IntentStatus.Committed, retrievedBy1.Status);
    }

    [Fact]
    public async Task MultiWriter_ConcurrentCompactionWhileWriting_ZeroRecordLossAndNoSharingViolation()
    {
        using var journal1 = new IntentJournal(_journalPath);
        using var journal2 = new IntentJournal(_journalPath);
        using var journal3 = new IntentJournal(_journalPath);

        var guids1 = Enumerable.Range(0, 100).Select(_ => Guid.NewGuid()).ToList();
        var guids2 = Enumerable.Range(0, 100).Select(_ => Guid.NewGuid()).ToList();

        var task1 = Task.Run(async () =>
        {
            foreach (var id in guids1)
            {
                await journal1.RecordIntentAsync(new IntentJournalRecord
                {
                    OperationId = id,
                    Status = IntentStatus.InFlight,
                    TimestampUtc = DateTime.UtcNow
                });
            }
        });

        var task2 = Task.Run(async () =>
        {
            foreach (var id in guids2)
            {
                await journal2.RecordIntentAsync(new IntentJournalRecord
                {
                    OperationId = id,
                    Status = IntentStatus.InFlight,
                    TimestampUtc = DateTime.UtcNow
                });
            }
        });

        var task3 = Task.Run(async () =>
        {
            for (int i = 0; i < 5; i++)
            {
                await Task.Delay(10);
                await journal3.PruneOldEntriesAsync(TimeSpan.FromDays(1));
            }
        });

        await Task.WhenAll(task1, task2, task3);

        using var coldJournal = new IntentJournal(_journalPath);
        var incomplete = await coldJournal.GetIncompleteIntentsAsync();
        Assert.Equal(200, incomplete.Count);

        var allGuids = new HashSet<Guid>(guids1.Concat(guids2));
        var loadedGuids = incomplete.Select(r => r.OperationId).ToHashSet();
        Assert.True(allGuids.SetEquals(loadedGuids));
    }
}

