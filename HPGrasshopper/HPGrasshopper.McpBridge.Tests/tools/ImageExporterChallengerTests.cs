namespace HPGrasshopper.McpBridge.Tests.Tools;

using System;
using System.Drawing;
using System.Reflection;
using Grasshopper.GUI.Canvas;
using HPGrasshopper.McpBridge.Tools;
using Xunit;

public sealed class ImageExporterChallengerTests
{
    static ImageExporterChallengerTests()
    {
        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
        {
            var requestedName = new AssemblyName(args.Name).Name;
            if (string.Equals(requestedName, "Eto", StringComparison.OrdinalIgnoreCase))
            {
                var etoPath = @"C:\Program Files\Rhino 8\System\Eto.dll";
                if (System.IO.File.Exists(etoPath)) return Assembly.LoadFrom(etoPath);
            }
            return null;
        };
    }

    [Fact]
    public void ZoomClamping_SubMinZoom_ClampsToMinZoom()
    {
        var (w, h) = ImageExporter.CalculateDimensions(4000, 3000, 0.05);
        Assert.Equal(400, w);
        Assert.Equal(300, h);
    }

    [Fact]
    public void ZoomClamping_AboveMaxZoom_ClampsToMaxZoom()
    {
        var (w, h) = ImageExporter.CalculateDimensions(1000, 800, 10.0);
        Assert.Equal(5000, w);
        Assert.Equal(4000, h);
    }

    [Fact]
    public void ZoomClamping_NegativeInfinity_ClampsToMinZoom()
    {
        var (w, h) = ImageExporter.CalculateDimensions(4000, 3000, double.NegativeInfinity);
        Assert.Equal(400, w);
        Assert.Equal(300, h);
    }

    [Fact]
    public void ZoomClamping_PositiveInfinity_ClampsToMaxZoom()
    {
        var (w, h) = ImageExporter.CalculateDimensions(1000, 800, double.PositiveInfinity);
        Assert.Equal(5000, w);
        Assert.Equal(4000, h);
    }

    [Fact]
    public void ZoomClamping_NaN_HandlesDefensivelyWithoutException()
    {
        var (w, h) = ImageExporter.CalculateDimensions(1000, 800, double.NaN);
        Assert.True(w >= 200);
        Assert.True(h >= 200);
        Assert.False(double.IsNaN(w));
        Assert.False(double.IsNaN(h));
    }

    [Fact]
    public void Viewport_DuplicateAndRestore_PreservesZoomAndFocus()
    {
        var vp = new GH_Viewport();
        vp.Zoom = 2.5f;

        var dup = vp.Duplicate();
        Assert.NotNull(dup);
        Assert.Equal(2.5f, dup.Zoom);

        vp.Zoom = 0.5f;
        Assert.Equal(0.5f, vp.Zoom);

        vp.Set(dup);
        Assert.Equal(2.5f, vp.Zoom);
        Assert.Equal(dup.Zoom, vp.Zoom);
    }

    [Fact]
    public void Viewport_TryRestore_NullCanvasOrNullSaved_SafelyNoOp()
    {
        var restoreMethod = typeof(ImageExporter).GetMethod(
            "TryRestoreViewport",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(restoreMethod);

        var canvas = new GH_Canvas();
        restoreMethod!.Invoke(null, new object?[] { canvas, null });
    }

    [Fact]
    public void Viewport_PreservedAndRestoredUnderExceptionSimulation()
    {
        var canvas = new GH_Canvas();
        canvas.Viewport.Zoom = 1.75f;
        canvas.Viewport.Target = new Point(200, 400);

        var dupMethod = typeof(ImageExporter).GetMethod(
            "TryDuplicateViewport",
            BindingFlags.NonPublic | BindingFlags.Static);
        var restoreMethod = typeof(ImageExporter).GetMethod(
            "TryRestoreViewport",
            BindingFlags.NonPublic | BindingFlags.Static);
        var applyMethod = typeof(ImageExporter).GetMethod(
            "ApplyViewModeAndZoom",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(dupMethod);
        Assert.NotNull(restoreMethod);
        Assert.NotNull(applyMethod);

        var savedViewport = dupMethod!.Invoke(null, new object[] { canvas });
        Assert.NotNull(savedViewport);

        bool exceptionThrown = false;
        try
        {
            applyMethod!.Invoke(null, new object[] { canvas, "FullCanvas", 3.0 });
            Assert.NotEqual(1.75f, canvas.Viewport.Zoom);
            throw new OperationCanceledException("Simulated rendering cancellation/failure");
        }
        catch (OperationCanceledException)
        {
            exceptionThrown = true;
        }
        finally
        {
            restoreMethod!.Invoke(null, new object[] { canvas, savedViewport });
        }

        Assert.True(exceptionThrown);
        Assert.Equal(1.75f, canvas.Viewport.Zoom);
        Assert.Equal(200, canvas.Viewport.Target.X);
        Assert.Equal(400, canvas.Viewport.Target.Y);
    }
}
