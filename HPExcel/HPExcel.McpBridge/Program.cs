using System;
using System.Threading;
using System.Windows;

namespace HPExcel.McpBridge;

/// <summary>
///     Main application entry point. Enforces a single instance per user session via named Mutex
///     "Global\HPExcel.McpBridge.SingleInstance" and starts the WPF application.
/// </summary>
public static class Program
{
    private const string MutexName = "Global\\HPExcel.McpBridge.SingleInstance";

    [STAThread]
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                Serilog.Log.Fatal(ex, "Unhandled AppDomain exception in HPExcel.McpBridge");
            }
        };

        using var mutex = new Mutex(true, MutexName, out var isNewInstance);
        if (!isNewInstance)
        {
            MessageBox.Show(
                "An instance of HPExcel MCP Bridge is already running.",
                "HPExcel MCP Bridge",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
