using System;
using System.Threading;
using System.Windows;
using Serilog;

namespace HPRobot.McpBridge;

/// <summary>
///     Main application entry point. Enforces a single instance per user session via named Mutex
///     "Global\HPRobot.McpBridge.SingleInstance" and starts the WPF desktop application.
/// </summary>
public static class Program
{
    private const string MutexName = "Global\\HPRobot.McpBridge.SingleInstance";

    [STAThread]
    public static void Main(string[] args)
    {
        string logDir = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HPRobot", "McpBridge");
        System.IO.Directory.CreateDirectory(logDir);
        string crashLog = System.IO.Path.Combine(logDir, "crash.log");

        try
        {
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                {
                    System.IO.File.AppendAllText(crashLog, $"[Unhandled AppDomain] {ex}\n");
                    Log.Fatal(ex, "Unhandled AppDomain exception in HPRobot.McpBridge");
                }
            };

            using var mutex = new Mutex(true, MutexName, out var isNewInstance);
            if (!isNewInstance)
            {
                MessageBox.Show(
                    "An instance of HPRobot MCP Bridge is already running.",
                    "HPRobot MCP Bridge",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var app = new App();
            app.DispatcherUnhandledException += (_, e) =>
            {
                System.IO.File.AppendAllText(crashLog, $"[DispatcherUnhandled] {e.Exception}\n");
                e.Handled = false;
            };
            app.InitializeComponent();
            app.Run();
        }
        catch (Exception ex)
        {
            System.IO.File.AppendAllText(crashLog, $"[Main Catch] {ex}\n");
            throw;
        }
    }
}
