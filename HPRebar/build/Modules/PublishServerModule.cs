using Build.Options;
using Microsoft.Extensions.Options;
using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Modules;
using Shouldly;
using Sourcy.DotNet;

namespace Build.Modules;

/// <summary>
///     Publish the MCP server as a framework-dependent single-file exe and zip it. It is not a Revit
///     add-in: the host AI launches it, so it ships beside the bundles rather than inside one.
/// </summary>
[DependsOn<ResolveVersioningModule>]
[DependsOn<CompileProjectModule>]
public sealed class PublishServerModule(IOptions<BuildOptions> buildOptions) : Module
{
    protected override async Task ExecuteModuleAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var versioningResult = await context.GetModule<ResolveVersioningModule>();
        var versioning = versioningResult.ValueOrDefault!;

        var outputFolder = context.Git().RootDirectory.GetFolder(buildOptions.Value.OutputDirectory);
        var publishFolder = outputFolder.CreateFolder(Projects.HPRebar_Mcp_Server.Name.Replace(".csproj", string.Empty));

        await context.DotNet().Publish(new DotNetPublishOptions
        {
            ProjectSolution = Projects.HPRebar_Mcp_Server.FullName,
            Configuration = "Release",
            Runtime = "win-x64",
            Output = publishFolder.Path,
            Properties =
            [
                ("PublishSingleFile", "true"),
                ("SelfContained", "false"),
                ("IncludeNativeLibrariesForSelfExtract", "true"),
                ("VersionPrefix", versioning.VersionPrefix),
                ("VersionSuffix", versioning.VersionSuffix!)
            ]
        }, cancellationToken: cancellationToken);

        publishFolder.GetFiles(file => file.Extension == ".exe").ShouldNotBeEmpty("dotnet publish produced no executable");

        var outputFile = outputFolder.GetFile($"{publishFolder.Name}.zip");
        context.Files.Zip.ZipFolder(publishFolder, outputFile.Path);

        context.Summary.KeyValue("Artifacts", "MCP server", outputFile.Path);
    }
}
