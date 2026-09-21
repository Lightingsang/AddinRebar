using System;
using System.Threading;
using System.Windows;

namespace HPPowerBi.McpBridge;

/// <summary>
///     Main application entry point. Enforces a single instance per user session via named Mutex
///     and starts the WPF application.
/// </summary>
public static class Program
{
    private const string MutexName = "Global\\HPPowerBi.McpBridge.SingleInstance";

    [STAThread]
    public static void Main(string[] args)
    {
        using var mutex = new Mutex(true, MutexName, out var isNewInstance);
        if (!isNewInstance)
        {
            MessageBox.Show(
                "An instance of HP Power BI MCP Bridge is already running.",
                "HP Power BI MCP Bridge",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
