using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Autodesk.PackageBuilder;
using Build.Options;
using Microsoft.Extensions.Options;
using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.FileSystem;
using ModularPipelines.Git.Extensions;
using ModularPipelines.Modules;
using Shouldly;
using Sourcy.DotNet;
using File = ModularPipelines.FileSystem.File;

namespace Build.Modules;

/// <summary>
///     Create one Autodesk .bundle package per add-in project: the rebar add-in and the MCP bridge ship
///     as separate bundles so either can be installed, updated or removed on its own.
/// </summary>
[DependsOn<ResolveVersioningModule>]
[DependsOn<CompileProjectModule>]
public sealed partial class CreateBundleModule(IOptions<BuildOptions> buildOptions, IOptions<BundleOptions> bundleOptions) : Module
{
    protected override async Task ExecuteModuleAsync(IModuleContext context, CancellationToken cancellationToken)
    {
        var versioningResult = await context.GetModule<ResolveVersioningModule>();
        var versioning = versioningResult.ValueOrDefault!;

        FileInfo[] addins = [Projects.HPRebar, Projects.HPRebar_McpBridge];

        foreach (var addin in addins)
        {
            await context.SubModule(addin.Name, async () => await BundleAsync(context, addin, versioning, cancellationToken));
        }
    }

    private async Task BundleAsync(IModuleContext context, FileInfo project, ResolveVersioningResult versioning, CancellationToken cancellationToken)
    {
        var bundleTarget = new File(project.FullName);
        var targetDirectories = bundleTarget.Folder!
            .GetFolder("bin")
            .GetFolders(folder => folder.Name == "publish")
            .ToArray();

        targetDirectories.ShouldNotBeEmpty($"No content were found to create a bundle for {bundleTarget.NameWithoutExtension}");

        var outputFolder = context.Git().RootDirectory.GetFolder(buildOptions.Value.OutputDirectory);
        var bundleFolder = outputFolder.CreateFolder($"{bundleTarget.NameWithoutExtension}.bundle");
        var contentFolder = bundleFolder.CreateFolder("Contents");
        var manifestFile = bundleFolder.GetFile("PackageContents.xml");

        PackFiles(targetDirectories, contentFolder);
        GenerateManifest(bundleTarget, targetDirectories, manifestFile, versioning);

        var outputFile = outputFolder.GetFile($"{bundleFolder.Name}.zip");
        context.Files.Zip.ZipFolder(bundleFolder, outputFile.Path);
        await bundleFolder.DeleteAsync(cancellationToken);

        context.Summary.KeyValue("Artifacts", $"Bundle {bundleTarget.NameWithoutExtension}", outputFile.Path);
    }

    private static void PackFiles(Folder[] targetDirectories, Folder contentFolder)
    {
        foreach (var targetDirectory in targetDirectories)
        {
            TryParseVersion(targetDirectory.Path, out var version)
                .ShouldBeTrue($"Could not parse version from directory name: {targetDirectory.Path}");

            var versionFolder = contentFolder.CreateFolder(version);
            foreach (var filePath in targetDirectory.GetFiles(file => file.Exists))
            {
                var relativePath = Path.GetRelativePath(targetDirectory.Path, filePath.Path);
                var destinationPath = versionFolder.GetFile(relativePath);
                if (!destinationPath.Folder!.Exists)
                {
                    destinationPath.Folder!.Create();
                }

                filePath.CopyTo(destinationPath.Path);
            }
        }
    }

    /// <summary>
    ///     Generate the Autodesk manifest.
    /// </summary>
    private void GenerateManifest(File bundleTarget, Folder[] targetDirectories, File manifestDirectory, ResolveVersioningResult versioning)
    {
        BuilderUtils.Build<PackageContentsBuilder>(builder =>
        {
            builder.ApplicationPackage.Create()
                .ProductType(ProductTypes.Application)
                .AutodeskProduct(AutodeskProducts.Revit)
                .Name(bundleTarget.NameWithoutExtension)
                .AppVersion(versioning.Version);

            builder.CompanyDetails.Create(bundleOptions.Value.VendorName)
                .Email(bundleOptions.Value.VendorEmail)
                .Url(bundleOptions.Value.VendorUrl);

            foreach (var targetDirectory in targetDirectories)
            {
                TryParseVersion(targetDirectory.Path, out var version)
                    .ShouldBeTrue($"Could not parse version from directory name: {targetDirectory.Path}");

                var addinManifests = targetDirectory.GetFiles(file => file.Extension == ".addin");
                foreach (var addinManifest in addinManifests)
                {
                    var relativePath = Path.GetRelativePath(targetDirectory.Path, addinManifest.Path);

                    builder.Components.CreateEntry($"Revit {version}")
                        .RevitPlatform(int.Parse(version))
                        .AppName(bundleTarget.NameWithoutExtension)
                        .ModuleName($"./Contents/{version}/{relativePath}");
                }
            }
        }, manifestDirectory);
    }
    
    /// <summary>
    ///     Parse a version string from the given input.
    /// </summary>
    private static bool TryParseVersion(string input, [NotNullWhen(true)] out string? version)
    {
        version = null;
        var match = VersionRegex().Match(input);
        if (!match.Success) return false;

        switch (match.Value.Length)
        {
            case 4:
                version = match.Value;
                return true;
            case 2:
                version = $"20{match.Value}";
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    ///     A regular expression to match the last sequence of numeric characters in a string.
    /// </summary>
    [GeneratedRegex(@"(\d+)(?!.*\d)")]
    private static partial Regex VersionRegex();
}