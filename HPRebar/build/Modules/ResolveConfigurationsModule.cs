using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;
using ModularPipelines.Context;
using ModularPipelines.Modules;
using Shouldly;
using Sourcy.DotNet;
using File = ModularPipelines.FileSystem.File;

namespace Build.Modules;

/// <summary>
///     Resolve solution configurations required to compile the add-in for all supported Revit versions.
/// </summary>
public sealed class ResolveConfigurationsModule : Module<string[]>
{
    protected override async Task<string[]?> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var solutionModel = await LoadSolutionModelAsync(context, cancellationToken);
        var configurations = solutionModel.BuildTypes
            .Where(configuration => configuration.Contains("Release.R", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        configurations.ShouldNotBeEmpty("No solution configurations have been found");

        return configurations;
    }

    /// <summary>
    ///     Reads HPRebar.slnx by its Sourcy-resolved path. The repository holds other solutions
    ///     (McpShared, HPAutoCad), so searching the git root for "any .slnx" would pick one of those.
    /// </summary>
    private static async Task<SolutionModel> LoadSolutionModelAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var solution = new File(Solutions.HPRebar.FullName);
        solution.Exists.ShouldBeTrue($"Solution file not found: {solution.Path}");

        await using var slnxStream = solution.GetStream();
        return await SolutionSerializers.SlnXml.OpenAsync(slnxStream, cancellationToken);
    }
}