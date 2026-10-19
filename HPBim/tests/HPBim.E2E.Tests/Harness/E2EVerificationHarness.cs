namespace HPBim.E2E.Tests.Harness;

using System;
using System.IO;
using System.Threading.Tasks;
using HPBim.Cli.CommandLine;
using HPBim.Cli.Commands;
using HPBim.Cli.Formatting;
using HPBim.Client.Connection;
using HPBim.Core.Models;
using HPBim.Core.Serialization;

public static class E2EVerificationHarness
{
    public static BimModel LoadBenchmarkTownhouseModel()
    {
        string candidatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "NhaPho_5x18m.json");
        if (File.Exists(candidatePath))
        {
            return BimModelSerializer.DeserializeFromFile(candidatePath);
        }

        string directPath = @"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\NhaPho_5x18m.json";
        if (File.Exists(directPath))
        {
            return BimModelSerializer.DeserializeFromFile(directPath);
        }

        throw new FileNotFoundException("Could not find benchmark NhaPho_5x18m.json file.");
    }

    public static async Task<int> RunGenerateCliAsync(string filePath, bool dryRun, E2EFakeRevitPipeClient fakeClient)
    {
        var cmd = new GenerateCommand(new ConsoleFormatter(), fakeClient);
        var argList = dryRun
            ? new[] { "generate", "--input", filePath, "--dry-run" }
            : new[] { "generate", "--input", filePath };
        var args = CliParser.Parse(argList);
        return await cmd.ExecuteAsync(args).ConfigureAwait(false);
    }
}
