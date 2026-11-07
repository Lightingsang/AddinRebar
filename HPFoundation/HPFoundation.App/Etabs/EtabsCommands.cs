using System.Text.Json;
using HPFoundation.Core.Modeling;

namespace HPFoundation.App.Etabs;

/// <summary>Runs one model spec in a private ETABS and writes the forces beside it.</summary>
internal static class EtabsCommands
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public static int RunSpec(string specPath, string outputFolder)
    {
        var spec = JsonSerializer.Deserialize<ModelSpec>(File.ReadAllText(specPath))
            ?? throw new InvalidOperationException($"No model spec in {specPath}.");
        Directory.CreateDirectory(outputFolder);

        AnalysisResult result;
        using (var etabs = EtabsSession.Start())
        {
            var pointNames = ModelBuilder.Build(etabs.Model, spec);
            result = AnalysisRunner.Run(etabs.Model, Path.Combine(outputFolder, "model.EDB"), pointNames);
        }

        File.WriteAllText(Path.Combine(outputFolder, "analysis-result.json"), JsonSerializer.Serialize(result, Json));
        var holds = EquilibriumCheck.Holds(spec, result);
        Console.WriteLine($"Nodes {spec.Nodes.Count}, stations {result.Stations.Count}");
        Console.WriteLine($"Load {EquilibriumCheck.AppliedLoadT(spec):0.###} T, reaction {result.TotalReactionT:0.###} T, {(holds ? "balanced" : "NOT BALANCED")}");
        return holds ? 0 : 1;
    }
}
