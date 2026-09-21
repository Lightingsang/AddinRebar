using System;
using System.IO;
using System.Text.Json;
using Serilog;

namespace HPPowerBi.McpBridge.ExternalTools;

public sealed record RegistrationResult(
    bool Success,
    string FilePath,
    bool IsElevatedDirectory,
    string? ErrorMessage = null);

/// <summary>
///     Manages the auto-registration of HPPowerBi.McpBridge in Power BI Desktop's External Tools ribbon.
///     Writes HPPowerBi.pbitool.json to %CommonProgramFiles%\Microsoft Shared\Power BI Desktop\External Tools\
///     with fallback to %LocalAppData%\Microsoft\Power BI Desktop\External Tools\.
/// </summary>
public static class ExternalToolsRegistrar
{
    public const string ToolFileName = "HPPowerBi.pbitool.json";
    public const string ToolName = "HP Power BI MCP";
    public const string ToolDescription = "AI-assisted MCP Bridge connecting Power BI Desktop to AI coding agents";
    public const string ToolTooltip = "HP MCP Bridge for Power BI: AI automation, DAX evaluation, and tabular modeling.";
    public const string ToolArguments = "\"%server%\" \"%database%\"";

    /// <summary>
    ///     Primary system-wide directory:
    ///     %CommonProgramFiles%\Microsoft Shared\Power BI Desktop\External Tools\
    /// </summary>
    public static string GetPrimaryDirectory()
    {
        var commonProgramFiles = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles);
        return Path.Combine(commonProgramFiles, "Microsoft Shared", "Power BI Desktop", "External Tools");
    }

    /// <summary>
    ///     Fallback per-user directory:
    ///     %LocalAppData%\Microsoft\Power BI Desktop\External Tools\
    /// </summary>
    public static string GetFallbackDirectory()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "Microsoft", "Power BI Desktop", "External Tools");
    }

    /// <summary>
    ///     Generates the .pbitool.json schema payload.
    /// </summary>
    public static string GenerateJson(string executablePath, string? iconData = null)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
            throw new ArgumentException("Executable path cannot be empty.", nameof(executablePath));

        var toolDefinition = new
        {
            version = "1.0",
            name = ToolName,
            operationType = "ExternalTool",
            description = ToolDescription,
            tooltip = ToolTooltip,
            executable = executablePath,
            arguments = ToolArguments,
            iconData = iconData ?? string.Empty
        };

        return JsonSerializer.Serialize(toolDefinition, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    /// <summary>
    ///     Registers the tool into the External Tools directory.
    ///     Attempts primary CommonProgramFiles directory first; on UnauthorizedAccessException, falls back to LocalAppData.
    /// </summary>
    public static RegistrationResult Register(string? executablePath = null, string? customDirectory = null, string? iconData = null)
    {
        var targetExe = executablePath ?? Environment.ProcessPath ?? "HPPowerBi.McpBridge.exe";
        var json = GenerateJson(targetExe, iconData);

        if (!string.IsNullOrWhiteSpace(customDirectory))
        {
            return TryWriteToDirectory(customDirectory, json, isElevated: false);
        }

        // 1. Try primary system directory
        var primaryDir = GetPrimaryDirectory();
        try
        {
            Directory.CreateDirectory(primaryDir);
            var filePath = Path.Combine(primaryDir, ToolFileName);
            File.WriteAllText(filePath, json);
            Log.Information("Registered External Tool in primary directory: '{Path}'", filePath);
            return new RegistrationResult(true, filePath, IsElevatedDirectory: true);
        }
        catch (UnauthorizedAccessException ex)
        {
            Log.Information(ex, "Cannot write to primary directory '{Dir}' without elevation, trying fallback directory", primaryDir);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Unexpected error writing to primary directory '{Dir}'", primaryDir);
        }

        // 2. Try user fallback directory
        var fallbackDir = GetFallbackDirectory();
        return TryWriteToDirectory(fallbackDir, json, isElevated: false);
    }

    private static RegistrationResult TryWriteToDirectory(string directory, string json, bool isElevated)
    {
        try
        {
            Directory.CreateDirectory(directory);
            var filePath = Path.Combine(directory, ToolFileName);
            File.WriteAllText(filePath, json);
            Log.Information("Registered External Tool at '{Path}'", filePath);
            return new RegistrationResult(true, filePath, isElevated);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to register External Tool in directory '{Dir}'", directory);
            return new RegistrationResult(false, Path.Combine(directory, ToolFileName), isElevated, ex.Message);
        }
    }

    /// <summary>
    ///     Checks if the tool registration file exists and matches the current executable.
    /// </summary>
    public static bool IsRegistered(string? executablePath = null, string? customDirectory = null)
    {
        var targetExe = executablePath ?? Environment.ProcessPath;

        if (!string.IsNullOrWhiteSpace(customDirectory))
        {
            var p = Path.Combine(customDirectory, ToolFileName);
            return IsFileValid(p, targetExe);
        }

        var primaryFile = Path.Combine(GetPrimaryDirectory(), ToolFileName);
        if (IsFileValid(primaryFile, targetExe))
            return true;

        var fallbackFile = Path.Combine(GetFallbackDirectory(), ToolFileName);
        return IsFileValid(fallbackFile, targetExe);
    }

    private static bool IsFileValid(string filePath, string? expectedExe)
    {
        if (!File.Exists(filePath))
            return false;

        if (string.IsNullOrWhiteSpace(expectedExe))
            return true;

        try
        {
            var content = File.ReadAllText(filePath);
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("executable", out var exeProp))
            {
                var registeredExe = exeProp.GetString();
                return string.Equals(registeredExe, expectedExe, StringComparison.OrdinalIgnoreCase);
            }
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Error reading external tool file at '{Path}'", filePath);
        }

        return false;
    }

    /// <summary>
    ///     Unregisters the external tool by deleting the .pbitool.json file.
    /// </summary>
    public static bool Unregister(string? customDirectory = null)
    {
        var removed = false;

        if (!string.IsNullOrWhiteSpace(customDirectory))
        {
            var p = Path.Combine(customDirectory, ToolFileName);
            if (File.Exists(p)) { File.Delete(p); removed = true; }
            return removed;
        }

        var primaryFile = Path.Combine(GetPrimaryDirectory(), ToolFileName);
        if (File.Exists(primaryFile))
        {
            try { File.Delete(primaryFile); removed = true; }
            catch (Exception ex) { Log.Warning(ex, "Could not delete primary external tool file '{Path}'", primaryFile); }
        }

        var fallbackFile = Path.Combine(GetFallbackDirectory(), ToolFileName);
        if (File.Exists(fallbackFile))
        {
            try { File.Delete(fallbackFile); removed = true; }
            catch (Exception ex) { Log.Warning(ex, "Could not delete fallback external tool file '{Path}'", fallbackFile); }
        }

        return removed;
    }
}
