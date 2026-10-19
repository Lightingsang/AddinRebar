namespace HPGrasshopper.E2E.Tests.Harness;

using System;

/// <summary>
/// Backward-compatibility adapter delegating to genuine GrasshopperE2EBridge.
/// </summary>
[Obsolete("Use GrasshopperE2EBridge directly.")]
public sealed class GrasshopperE2EFakeBridge : GrasshopperE2EBridge
{
}
