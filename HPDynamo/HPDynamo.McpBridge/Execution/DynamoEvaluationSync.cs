namespace HPDynamo.McpBridge.Execution;

using System;
using System.Threading;
using System.Windows.Threading;
using Dynamo.Models;
using Serilog;

/// <summary>
///     Synchronizes Dynamo graph evaluations by awaiting EvaluationCompleted without blocking the Revit UI thread message pump.
/// </summary>
public static class DynamoEvaluationSync
{
    public static bool RunAndWait(
        DynamoModel model,
        Action runAction,
        TimeSpan timeout,
        CancellationToken cancellationToken,
        Action<string?> log)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(runAction);

        var completed = false;
        var evaluationSuccess = false;
        var frame = new DispatcherFrame();

        void OnEvaluationCompleted(object? sender, EvaluationCompletedEventArgs e)
        {
            completed = true;
            evaluationSuccess = e.EvaluationSucceeded;
            frame.Continue = false;
        }

        model.EvaluationCompleted += OnEvaluationCompleted;
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        using var registration = linkedCts.Token.Register(() => frame.Continue = false);

        try
        {
            if (linkedCts.Token.IsCancellationRequested)
            {
                return CheckCompletion(false, false, cancellationToken, linkedCts.Token, timeout, log);
            }

            runAction();
            if (!completed && !linkedCts.Token.IsCancellationRequested)
            {
                Dispatcher.PushFrame(frame);
            }

            return CheckCompletion(completed, evaluationSuccess, cancellationToken, linkedCts.Token, timeout, log);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Exception encountered while waiting for Dynamo evaluation");
            return false;
        }
        finally
        {
            model.EvaluationCompleted -= OnEvaluationCompleted;
            if (linkedCts.Token.IsCancellationRequested || !completed)
            {
                DrainScheduler(model);
            }
        }
    }

    private static void DrainScheduler(DynamoModel model)
    {
        try
        {
            if (model.Scheduler == null || (!model.Scheduler.HasTaskInProgress && !model.Scheduler.HasPendingTasks)) return;

            var sw = System.Diagnostics.Stopwatch.StartNew();
            while ((model.Scheduler.HasTaskInProgress || model.Scheduler.HasPendingTasks) && sw.ElapsedMilliseconds < 500)
                Thread.Sleep(10);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to drain Dynamo scheduler in evaluation sync");
        }
    }

    private static bool CheckCompletion(
        bool completed,
        bool evaluationSuccess,
        CancellationToken ct,
        CancellationToken linkedCt,
        TimeSpan timeout,
        Action<string?> log)
    {
        if (linkedCt.IsCancellationRequested)
        {
            if (ct.IsCancellationRequested)
                log("Dynamo evaluation was cancelled.");
            else
                log($"Dynamo evaluation timed out after {timeout.TotalSeconds:F1}s.");
            return false;
        }

        if (!completed)
        {
            log("Dynamo evaluation did not complete.");
            return false;
        }

        return evaluationSuccess;
    }
}
