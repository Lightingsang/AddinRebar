using System;
using System.IO;
using System.Linq;
using Installer;
using WixSharp;
using WixSharp.CommonTasks;
using WixSharp.Controls;

var customName = args.FirstOrDefault(a => a.StartsWith("--name="))?.Substring("--name=".Length)
                 ?? Environment.GetEnvironmentVariable("INSTALLER_PRODUCT_NAME")
                 ?? "HPAutoCad";

var outDirArg = args.FirstOrDefault(a => a.StartsWith("--outdir="))?.Substring("--outdir=".Length)
                ?? "output";

var cleanArgs = args.Where(a => !a.StartsWith("--name=") && !a.StartsWith("--outdir=")).ToArray();

if (cleanArgs.Length < 2)
{
    Console.WriteLine("Usage: Installer.exe <version> <bundleDirectory> [--name=<name>] [--outdir=<dir>]");
    return 1;
}

var versionStr = cleanArgs[0];
var bundleDirectory = Path.GetFullPath(cleanArgs[1]);

if (!Directory.Exists(bundleDirectory))
{
    Console.WriteLine($"Error: Bundle directory not found: {bundleDirectory}");
    return 1;
}

var versioning = Versioning.CreateFromVersionString(versionStr);
var outDir = Path.GetFullPath(outDirArg);

string ResolveResource(string relativePath)
{
    if (System.IO.File.Exists(relativePath)) return Path.GetFullPath(relativePath);
    var candidate = Path.Combine(AppContext.BaseDirectory, relativePath);
    if (System.IO.File.Exists(candidate)) return Path.GetFullPath(candidate);
    var candidate2 = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", relativePath));
    if (System.IO.File.Exists(candidate2)) return candidate2;
    return relativePath;
}

var bannerImage = ResolveResource(@"install\Resources\Icons\BannerImage.png");
var backgroundImage = ResolveResource(@"install\Resources\Icons\BackgroundImage.png");
var shellIcon = ResolveResource(@"install\Resources\Icons\ShellIcon.ico");

Project CreateBaseProject(Guid upgradeCode)
{
    var p = new Project
    {
        OutDir = outDir,
        Name = customName,
        Platform = Platform.x64,
        UI = WUI.WixUI_FeatureTree,
        MajorUpgrade = MajorUpgrade.Default,
        GUID = upgradeCode,
        BannerImage = bannerImage,
        BackgroundImage = backgroundImage,
        Version = versioning.VersionPrefix,
        ControlPanelInfo =
        {
            Manufacturer = Environment.UserName,
            ProductIcon = shellIcon
        }
    };
    p.RemoveDialogsBetween(NativeDialogs.WelcomeDlg, NativeDialogs.CustomizeDlg);
    return p;
}

Feature CreateFeatures()
{
    var root = new Feature
    {
        Name = "AutoCAD Add-in",
        Description = "AutoCAD add-in installation files",
        Display = FeatureDisplay.expand
    };
    var sub = new Feature
    {
        Name = "2026",
        Description = "Install add-in for AutoCAD 2026 (AI MCP Bridge & HPGeoLink KMZ)",
        Display = FeatureDisplay.expand
    };
    root.Add(sub);
    return sub;
}

BuildSingleUserMsi();
BuildMultiUserMsi();
return 0;

void BuildSingleUserMsi()
{
    var p = CreateBaseProject(new Guid("B7E2045A-4B6A-4A73-8C61-591DF82C0E93"));
    var versionFeature = CreateFeatures();
    var bundleDirEntity = Generator.CreateDirTree(bundleDirectory, versionFeature, "HPAutoCad.bundle");
    p.Scope = InstallScope.perUser;
    p.OutFileName = $"{customName}-{versioning.Version}-SingleUser";
    p.Dirs =
    [
        new Dir(@"%AppDataFolder%\Autodesk\ApplicationPlugins", bundleDirEntity)
    ];
    p.BuildMsi();
}

void BuildMultiUserMsi()
{
    var p = CreateBaseProject(new Guid("B7E2045A-4B6A-4A73-8C61-591DF82C0E94"));
    var versionFeature = CreateFeatures();
    var bundleDirEntity = Generator.CreateDirTree(bundleDirectory, versionFeature, "HPAutoCad.bundle");
    p.Scope = InstallScope.perMachine;
    p.Package.AttributesDefinition = "Scope=perMachine";
    p.OutFileName = $"{customName}-{versioning.Version}-MultiUser";
    p.Dirs =
    [
        new Dir(@"%ProgramFiles%\Autodesk\ApplicationPlugins", bundleDirEntity)
    ];
    p.BuildMsi();
}
