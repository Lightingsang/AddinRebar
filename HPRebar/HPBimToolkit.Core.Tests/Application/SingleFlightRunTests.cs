using HPBimToolkit.Core.Application;
using HPBimToolkit.Core.Contracts;
using Xunit;

namespace HPBimToolkit.Core.Tests.Application;

public sealed class SingleFlightRunTests
{
    private readonly SingleFlightRun<string> _runs = new("other scope busy");
    private int _started;

    private Func<Task<object?>> Start(TaskCompletionSource<object?> completion) => () =>
    {
        _started++;
        return completion.Task;
    };

    [Fact]
    public void Run_SameKeyWhileRunning_JoinsTheRunInProgress()
    {
        var completion = new TaskCompletionSource<object?>();
        var first = _runs.Run("model", CancellationToken.None, Start(completion));

        var second = _runs.Run("model", CancellationToken.None, Start(new TaskCompletionSource<object?>()));

        Assert.Same(first, second);
        Assert.Equal(1, _started);
    }

    [Fact]
    public void Run_OtherKeyWhileRunning_AnswersBusy()
    {
        _runs.Run("model", CancellationToken.None, Start(new TaskCompletionSource<object?>()));

        // BUSY is thrown synchronously, before any task exists: the dispatcher answers without queueing anything.
        Action call = () => _runs.Run("activeView", CancellationToken.None, Start(new TaskCompletionSource<object?>()));
        var error = Assert.IsType<BridgeException>(Record.Exception(call));

        Assert.Equal(BridgeErrorCodes.Busy, error.Code);
        Assert.Equal("other scope busy", error.Message);
        Assert.Equal(1, _started);
    }

    [Theory]
    [InlineData("model")]
    [InlineData("activeView")]
    public void Run_AfterTheRunEnded_StartsAFreshOne(string nextKey)
    {
        var completion = new TaskCompletionSource<object?>();
        var first = _runs.Run("model", CancellationToken.None, Start(completion));
        completion.SetResult(1);

        var next = _runs.Run(nextKey, CancellationToken.None, Start(new TaskCompletionSource<object?>()));

        Assert.NotSame(first, next);
        Assert.Equal(2, _started);
    }

    // A retry after a modal dialog timed the first request out must never inherit its cancellation nor answer BUSY.
    [Theory]
    [InlineData("model")]
    [InlineData("activeView")]
    public void Run_WhenTheRunningRequestWasCancelled_AbandonsItForAFreshRun(string nextKey)
    {
        using var timedOut = new CancellationTokenSource();
        var stale = _runs.Run("model", timedOut.Token, Start(new TaskCompletionSource<object?>()));
        timedOut.Cancel();

        var fresh = _runs.Run(nextKey, CancellationToken.None, Start(new TaskCompletionSource<object?>()));

        Assert.NotSame(stale, fresh);
        Assert.Equal(2, _started);
    }
}
