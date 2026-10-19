using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Windows;

namespace HPKata.E2E.Tests.Common;

/// <summary>
/// Dedicated single STA thread executor for WPF tests.
/// Ensures all WPF Application and Window instances run on the same Dispatcher thread.
/// </summary>
internal static class E2EWpfHelper
{
    private static readonly BlockingCollection<Action> ActionQueue = new();
    private static readonly Thread StaThread;

    static E2EWpfHelper()
    {
        StaThread = new Thread(StaThreadLoop)
        {
            IsBackground = true,
            Name = "HPKata_E2E_WpfThread"
        };
        StaThread.SetApartmentState(ApartmentState.STA);
        StaThread.Start();
    }

    private static void StaThreadLoop()
    {
        try
        {
            if (Application.Current == null)
            {
                new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            }
        }
        catch (InvalidOperationException)
        {
            // Already created in AppDomain
        }

        foreach (var action in ActionQueue.GetConsumingEnumerable())
        {
            action();
        }
    }

    public static void RunOnSta(Action action)
    {
        Exception? error = null;
        using var done = new ManualResetEventSlim(false);

        ActionQueue.Add(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                done.Set();
            }
        });

        done.Wait();

        if (error != null)
        {
            throw new InvalidOperationException($"STA execution failed: {error.Message}", error);
        }
    }
}
