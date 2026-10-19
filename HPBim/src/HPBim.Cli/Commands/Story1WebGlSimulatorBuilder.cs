namespace HPBim.Cli.Commands;

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
///     Pure C# builder that compiles a self-contained 3D WebGL Digital Twin Simulator.
///     Renders 2D CAD wireframes, 3D extruded beams, columns, walls, and interactive QA/QC inspector.
/// </summary>
public static class Story1WebGlSimulatorBuilder
{
    public static async Task<string> BuildHtmlAsync(
        string beamsJson,
        string cadLinesJson,
        string columnsJson,
        string wallsJson,
        string gridsJson,
        CancellationToken ct = default)
    {
        string templatePath = Path.Combine(AppContext.BaseDirectory, "Templates", "Story1WebGlViewerTemplate.html");
        if (!File.Exists(templatePath))
        {
            templatePath = Path.Combine("HPBim", "src", "HPBim.Cli", "Templates", "Story1WebGlViewerTemplate.html");
        }

        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"Không tìm thấy file template WebGL tại: {templatePath}");
        }

        string template = await File.ReadAllTextAsync(templatePath, Encoding.UTF8, ct).ConfigureAwait(false);

        var sb = new StringBuilder(template);
        sb.Replace("__BEAMS_DATA__", beamsJson);
        sb.Replace("__CAD_LINES_DATA__", cadLinesJson);
        sb.Replace("__COLUMNS_DATA__", columnsJson);
        sb.Replace("__WALLS_DATA__", wallsJson);
        sb.Replace("__GRIDS_DATA__", gridsJson);

        return sb.ToString();
    }
}
