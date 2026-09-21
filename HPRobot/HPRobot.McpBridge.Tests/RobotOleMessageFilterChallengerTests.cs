using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HPRobot.McpBridge.Com;
using Xunit;

namespace HPRobot.McpBridge.Tests;

/// <summary>
///     Empirical adversarial tests challenging COM IOleMessageFilter & ComInteropHelper:
///     1. Verification that SERVERCALL_RETRYLATER (2) returns 250ms retry interval under the 30s ceiling.
///     2. Verification that SERVERCALL_RETRYLATER (2) returns -1 (cancel call) when dwTickCount >= 30,000ms.
///     3. Verification that non-retry rejection types (e.g. SERVERCALL_REJECTED = 1) return -1 immediately.
///     4. Verification of HandleInComingCall (SERVERCALL_ISHANDLED = 0) and MessagePending (PENDINGMSG_WAITDEFPROCESS = 2).
///     5. Verification of RegisterMessageFilter lifecycle and COM object release safety.
/// </summary>
public sealed class RobotOleMessageFilterChallengerTests
{
    private readonly object _filterInstance;
    private readonly MethodInfo _retryRejectedCallMethod;
    private readonly MethodInfo _handleInComingCallMethod;
    private readonly MethodInfo _messagePendingMethod;

    public RobotOleMessageFilterChallengerTests()
    {
        var filterType = typeof(ComInteropHelper).GetNestedType("RobotOleMessageFilter", BindingFlags.NonPublic);
        Assert.NotNull(filterType);

        _filterInstance = Activator.CreateInstance(filterType!)!;
        Assert.NotNull(_filterInstance);

        _retryRejectedCallMethod = filterType.GetMethod("RetryRejectedCall", BindingFlags.Public | BindingFlags.Instance)!;
        _handleInComingCallMethod = filterType.GetMethod("HandleInComingCall", BindingFlags.Public | BindingFlags.Instance)!;
        _messagePendingMethod = filterType.GetMethod("MessagePending", BindingFlags.Public | BindingFlags.Instance)!;

        Assert.NotNull(_retryRejectedCallMethod);
        Assert.NotNull(_handleInComingCallMethod);
        Assert.NotNull(_messagePendingMethod);
    }

    private int InvokeRetryRejectedCall(IntPtr htaskCallee, int dwTickCount, int dwRejectType)
    {
        return (int)_retryRejectedCallMethod.Invoke(_filterInstance, new object[] { htaskCallee, dwTickCount, dwRejectType })!;
    }

    private int InvokeHandleInComingCall(int dwCallType, IntPtr htaskCaller, int dwTickCount, IntPtr lpInterfaceInfo)
    {
        return (int)_handleInComingCallMethod.Invoke(_filterInstance, new object[] { dwCallType, htaskCaller, dwTickCount, lpInterfaceInfo })!;
    }

    private int InvokeMessagePending(IntPtr htaskCallee, int dwTickCount, int dwPendingType)
    {
        return (int)_messagePendingMethod.Invoke(_filterInstance, new object[] { htaskCallee, dwTickCount, dwPendingType })!;
    }

    // =========================================================================
    // 1. RetryRejectedCall: SERVERCALL_RETRYLATER (2) & Timeout Bounds
    // =========================================================================

    [Theory]
    [InlineData(0, 250)]
    [InlineData(100, 250)]
    [InlineData(1000, 250)]
    [InlineData(15000, 250)]
    [InlineData(29999, 250)]
    public void RetryRejectedCall_WhenServerCallRetryLater_Under30Seconds_ReturnsRetryInterval(int tickCount, int expected)
    {
        const int SERVERCALL_RETRYLATER = 2;
        var result = InvokeRetryRejectedCall(IntPtr.Zero, tickCount, SERVERCALL_RETRYLATER);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(30000, -1)]
    [InlineData(30001, -1)]
    [InlineData(45000, -1)]
    [InlineData(60000, -1)]
    [InlineData(int.MaxValue, -1)]
    public void RetryRejectedCall_WhenServerCallRetryLater_AtOrAbove30Seconds_ReturnsNegativeOne(int tickCount, int expected)
    {
        const int SERVERCALL_RETRYLATER = 2;
        var result = InvokeRetryRejectedCall(IntPtr.Zero, tickCount, SERVERCALL_RETRYLATER);
        Assert.Equal(expected, result);
    }

    // =========================================================================
    // 2. RetryRejectedCall: Non-Retry Rejection Types
    // =========================================================================

    [Theory]
    [InlineData(1)]  // SERVERCALL_REJECTED
    [InlineData(0)]  // None / unknown
    [InlineData(3)]  // Arbitrary invalid code
    [InlineData(-1)] // Negative
    public void RetryRejectedCall_WhenNotServerCallRetryLater_ReturnsNegativeOneImmediately(int rejectType)
    {
        var result = InvokeRetryRejectedCall(IntPtr.Zero, dwTickCount: 0, dwRejectType: rejectType);
        Assert.Equal(-1, result);
    }

    // =========================================================================
    // 3. HandleInComingCall & MessagePending
    // =========================================================================

    [Fact]
    public void HandleInComingCall_AlwaysReturnsServerCallIsHandled()
    {
        const int SERVERCALL_ISHANDLED = 0;
        var result = InvokeHandleInComingCall(dwCallType: 1, IntPtr.Zero, dwTickCount: 100, IntPtr.Zero);
        Assert.Equal(SERVERCALL_ISHANDLED, result);
    }

    [Fact]
    public void MessagePending_AlwaysReturnsPendingMsgWaitDefProcess()
    {
        const int PENDINGMSG_WAITDEFPROCESS = 2;
        var result = InvokeMessagePending(IntPtr.Zero, dwTickCount: 100, dwPendingType: 1);
        Assert.Equal(PENDINGMSG_WAITDEFPROCESS, result);
    }

    // =========================================================================
    // 4. RegisterMessageFilter Lifecycle in STA Apartment
    // =========================================================================

    [Fact]
    public void RegisterMessageFilter_OnStaThread_AcquiresAndRestoresScopeCleanly()
    {
        // Must run inside STA apartment to interact with CoRegisterMessageFilter
        var staThread = new Thread(() =>
        {
            using (var scope1 = ComInteropHelper.RegisterMessageFilter())
            {
                Assert.NotNull(scope1);

                // Nested filter registration
                using (var scope2 = ComInteropHelper.RegisterMessageFilter())
                {
                    Assert.NotNull(scope2);
                }
            }
        });
        staThread.SetApartmentState(ApartmentState.STA);
        staThread.Start();
        staThread.Join(2000);

        Assert.False(staThread.IsAlive);
    }

    // =========================================================================
    // 5. Safe COM Object Release Helpers
    // =========================================================================

    [Fact]
    public void ReleaseComObject_HandlesNullAndNonComObjectsSafely()
    {
        // Null references must be ignored cleanly
        ComInteropHelper.ReleaseComObject(null);
        ComInteropHelper.FinalReleaseComObject(null);

        // Plain managed objects (not COM wrappers) must not throw
        var managedObj = new object();
        ComInteropHelper.ReleaseComObject(managedObj);
        ComInteropHelper.FinalReleaseComObject(managedObj);
    }
}
