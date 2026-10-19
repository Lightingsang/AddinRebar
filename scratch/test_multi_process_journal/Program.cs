namespace Scratch.TestMultiProcess;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HPBim.Client.Connection;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Usage: write <journalPath> <guidListOut> <count>");
            Console.WriteLine("   or: verify <journalPath> <guidFile1> <guidFile2>");
            return 1;
        }

        string mode = args[0];
        string journalPath = args[1];

        if (mode == "write")
        {
            string guidListOut = args[2];
            int count = int.Parse(args[3]);

            var guids = Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToList();
            await File.WriteAllLinesAsync(guidListOut, guids.Select(g => g.ToString()));

            using var journal = new IntentJournal(journalPath);
            foreach (var guid in guids)
            {
                await journal.RecordIntentAsync(new IntentJournalRecord
                {
                    OperationId = guid,
                    Status = IntentStatus.InFlight,
                    TimestampUtc = DateTime.UtcNow,
                    PlanHash = "hash-" + guid.ToString("N").Substring(0, 8)
                });
            }

            Console.WriteLine($"SUCCESS: Wrote {count} records");
            return 0;
        }

        if (mode == "verify")
        {
            string guidFile1 = args[2];
            string guidFile2 = args[3];

            var set1 = (await File.ReadAllLinesAsync(guidFile1)).Select(Guid.Parse).ToHashSet();
            var set2 = (await File.ReadAllLinesAsync(guidFile2)).Select(Guid.Parse).ToHashSet();
            var allExpected = new HashSet<Guid>(set1.Concat(set2));

            Console.WriteLine($"Expected unique GUIDs: {allExpected.Count}");

            using var journal = new IntentJournal(journalPath);
            var incomplete = await journal.GetIncompleteIntentsAsync();

            Console.WriteLine($"Cold journal loaded: {incomplete.Count} incomplete intents");

            var loadedGuids = incomplete.Select(r => r.OperationId).ToHashSet();

            if (incomplete.Count != allExpected.Count)
            {
                Console.WriteLine($"ERROR: Count mismatch! Expected {allExpected.Count}, got {incomplete.Count}");
                return 2;
            }

            if (!allExpected.SetEquals(loadedGuids))
            {
                Console.WriteLine("ERROR: Loaded GUIDs do not match expected GUIDs!");
                return 3;
            }

            // Also verify raw lines in journal file
            string[] rawLines = await File.ReadAllLinesAsync(journalPath);
            Console.WriteLine($"Raw lines on disk: {rawLines.Length}");

            if (rawLines.Length != allExpected.Count)
            {
                Console.WriteLine($"ERROR: Raw line count mismatch! Expected {allExpected.Count}, got {rawLines.Length}");
                return 4;
            }

            Console.WriteLine("VERIFICATION SUCCESS: All 1,000 distinct records matched 100%!");
            return 0;
        }

        return 1;
    }
}
