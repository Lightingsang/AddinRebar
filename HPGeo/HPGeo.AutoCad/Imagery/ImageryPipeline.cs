using System.Globalization;
using System.IO;
using Autodesk.AutoCAD.DatabaseServices;
using HPGeo.Core.Conversion;
using HPGeo.Core.Imagery;
using HPGeo.Core.Model;
using HPGeo.Core.Projection;

namespace HPGeo.AutoCad.Imagery;

/// <summary>One imagery run, decided by the caller: the boundary extent in VN-2000 metres, the zone, the target, the file.</summary>
internal sealed record ImageryRequest(
    GridBoundingBox BoundaryM,
    TmParameters Tm,
    double MetersPerUnit,
    double MarginM,
    double? ResolutionMPerPx,
    int? Zoom,
    IImageryProvider Provider,
    string ImagePath,
    bool FitToCaps = false);

/// <summary>What the run did, for the console, the log and the settings: nothing is inserted unless <see cref="Success"/>.</summary>
internal sealed record ImageryOutcome(
    bool Success,
    ImageryRequest Request,
    IReadOnlyList<ConversionIssue> Issues,
    TilePlan? Plan,
    int TilesFetched,
    int TilesFromCache,
    AffineFitResult? Fit,
    RasterInsertResult? Inserted,
    string TileSource = "")
{
    public IEnumerable<ConversionIssue> Errors => Issues.Where(i => i.Severity == IssueSeverity.Error);
}

/// <summary>
/// Boundary → tile plan → fetch (worker threads, nothing of AutoCAD touched) → mosaic → control grid + affine
/// residual → warp onto the VN-2000 raster → attribution → PNG + world file → RasterImage. Runs on AutoCAD's
/// main thread and blocks it while the tiles download — but never without a way out: Escape (polled through
/// <c>userBreak</c>) cancels, and a deadline scaled to the tile count ends a black-holed connection. Progress
/// goes through <paramref name="report"/> from the main thread, so the caller may write to the editor. A
/// failure at any step inserts nothing and leaves no half-written PNG/PGW pair behind.
/// </summary>
internal static class ImageryPipeline
{
    private const int ProgressStepPercent = 10;
    private const int MinDeadlineSeconds = 60;
    private const int MaxDeadlineSeconds = 300;
    private const int SecondsPerTile = 3;

    /// <summary>One HttpClient for the session (sockets are pooled per client); the cache root is the default one.</summary>
    private static readonly Lazy<TileFetcher> SharedFetcher = new(() => new TileFetcher(userAgent: $"HPGeo/{Entry.Version} (AutoCAD add-in)"));

    /// <summary>The helper process when it was deployed beside the add-in (acad.exe may be denied the network), else in-process.</summary>
    private static (ITileSource Source, string Name) ResolveSource()
    {
        var exe = HelperTileFetcher.DefaultExePath();
        return exe is null ? (SharedFetcher.Value, "in-process") : (new HelperTileFetcher(exe), "helper");
    }

    public static TimeSpan DeadlineFor(int tileCount) =>
        TimeSpan.FromSeconds(Math.Clamp(tileCount * SecondsPerTile, MinDeadlineSeconds, MaxDeadlineSeconds));

    public static ImageryOutcome Run(Database db, ImageryRequest request, Action<string> report, Func<bool>? userBreak = null, ITileSource? fetcher = null)
    {
        var ci = CultureInfo.InvariantCulture;
        var issues = new List<ConversionIssue>();
        var transform = Vn2000Wgs84Transform.Default;
        var (source, sourceName) = fetcher is null ? ResolveSource() : (fetcher, "custom");
        ImageryOutcome Fail(string code, string message, TilePlan? plan = null, int fetched = 0, int cached = 0)
        {
            issues.Add(new ConversionIssue(IssueSeverity.Error, code, message));
            return new ImageryOutcome(false, request, issues, plan, fetched, cached, null, null, sourceName);
        }

        var target = TargetResolution(request, transform);
        var planned = TileCoverage.Plan(request.BoundaryM, request.MarginM, target, request.Tm, request.Provider, transform, fitToCaps: request.FitToCaps);
        if (planned.Plan is null) return Fail(planned.Error!.Code, planned.Error.Message);
        var plan = planned.Plan;
        issues.AddRange(plan.Warnings);

        // Refuse what can be refused before a single tile is downloaded: an unwritable output folder.
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(request.ImagePath))!);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return Fail("OUTPUT_NOT_WRITABLE", $"Không tạo được thư mục cho {request.ImagePath}: {exception.Message}", plan);
        }
        report($"zoom {plan.Zoom}, {plan.TileCount} tile ({plan.TileCountX}×{plan.TileCountY}), ảnh {plan.Output.WidthPx}×{plan.Output.HeightPx} px @ {plan.ResolutionMPerPx.ToString("F3", ci)} m/px, nguồn {request.Provider.DisplayName} ({sourceName}; Esc để huỷ)");

        var (fetched, cancelReason) = Fetch(source, request.Provider, plan, report, userBreak);
        if (fetched is null && cancelReason == "helper-failed")
        {
            // The helper could not run (missing runtime, blocked exe): the in-process fetcher is the honest second try.
            issues.Add(new ConversionIssue(IssueSeverity.Warning, "HELPER_UNAVAILABLE", "Không chạy được HPGeo.TileFetch.exe — thử tải trực tiếp từ AutoCAD."));
            sourceName = "in-process (helper failed)";
            (fetched, cancelReason) = Fetch(SharedFetcher.Value, request.Provider, plan, report, userBreak);
        }
        if (fetched is null)
        {
            return cancelReason == "user"
                ? Fail("CANCELLED", "Đã huỷ (Esc). Không chèn ảnh.", plan)
                : Fail("TILE_FETCH_FAILED", $"Hết thời gian chờ tải tile ({DeadlineFor(plan.TileCount).TotalSeconds:F0} s) — kiểm tra kết nối mạng. Không chèn ảnh.", plan);
        }
        if (!fetched.Complete)
        {
            var first = fetched.Failures[0];
            return Fail("TILE_FETCH_FAILED", $"{fetched.Failures.Count}/{plan.TileCount} tile không tải được — {first.Url} → {first.Reason}. Không chèn ảnh.", plan, fetched.Tiles.Count, fetched.FromCache);
        }

        var mosaic = TileStitcher.Stitch(plan, fetched.Tiles);
        var grid = plan.Output.BuildControlGrid(plan, request.Tm, transform);
        AffineFitResult? fit = null;
        try
        {
            fit = AffineFit.Fit(grid, plan.Output);
            if (fit.MaxResidualM > AffineFit.WarnResidualM)
                issues.Add(new ConversionIssue(IssueSeverity.Warning, "IMAGE_DISTORTION",
                    $"Ảnh nguồn lệch tới {fit.MaxResidualM.ToString("F2", ci)} m so với đặt phẳng (RMS {fit.RmsResidualM.ToString("F2", ci)} m) — đã nắn theo lưới khống chế, kiểm tra ranh trên ảnh."));
        }
        catch (ArgumentException exception)
        {
            issues.Add(new ConversionIssue(IssueSeverity.Warning, "AFFINE_FIT_SKIPPED", exception.Message));
        }

        var warped = RasterWarper.Warp(mosaic, grid, plan.Output, ResampleKernel.Bicubic); // crisp at 1:1 — bilinear softened every edge
        var bitmap = TileStitcher.StampAttribution(TileStitcher.ToBitmap(warped), request.Provider.Attribution);
        TileStitcher.SavePng(bitmap, request.ImagePath);
        RasterInsertResult inserted;
        try
        {
            inserted = RasterInserter.Insert(db, request.ImagePath, plan.Output, request.MetersPerUnit);
        }
        catch
        {
            DeleteQuietly(request.ImagePath);
            DeleteQuietly(RasterInserter.WorldFilePathFor(request.ImagePath));
            throw;
        }
        return new ImageryOutcome(true, request, issues, plan, fetched.Tiles.Count, fetched.FromCache, fit, inserted, sourceName);
    }

    /// <summary>res= wins; zoom= is turned into the native resolution at the boundary's centre (nudged up so the plan picks that zoom, not the next).</summary>
    private static double TargetResolution(ImageryRequest request, Vn2000Wgs84Transform transform)
    {
        if (request.ResolutionMPerPx is { } res) return res;
        if (request.Zoom is { } zoom)
        {
            var b = request.BoundaryM;
            var centre = transform.ToWgs84(new PlanePoint((b.MinE + b.MaxE) / 2, (b.MinN + b.MaxN) / 2), request.Tm);
            return WebMercator.ResolutionMPerPx(centre.LatDeg, Math.Min(zoom, request.Provider.MaxZoom)) * 1.001;
        }
        return TileCoverage.DefaultResolutionMPerPx;
    }

    /// <summary>
    /// The download runs on the thread pool; this thread polls it every 250 ms, reports every 10 %, cancels on
    /// Escape (<paramref name="userBreak"/>) or at the deadline. The progress callback fires on worker threads,
    /// so it only touches an int — never the editor. Null result = cancelled ("user" | "deadline") or, for the
    /// helper only, "helper-failed" (it could not run at all; a download failure is a normal result).
    /// </summary>
    private static (TileFetchResult? Result, string? CancelReason) Fetch(ITileSource fetcher, IImageryProvider provider, TilePlan plan, Action<string> report, Func<bool>? userBreak)
    {
        var tiles = plan.Tiles().ToList();
        var percent = 0;
        using var cts = new CancellationTokenSource(DeadlineFor(tiles.Count));
        var ct = cts.Token;
        var task = Task.Run(() => fetcher.FetchAsync(provider, tiles, new ThreadSafeProgress(p => Interlocked.Exchange(ref percent, p)), ct), CancellationToken.None);
        var reported = 0;
        string? cancelReason = null;
        while (!task.IsCompleted)
        {
            ((IAsyncResult)task).AsyncWaitHandle.WaitOne(250); // never throws; a faulted task surfaces from GetResult below
            if (cancelReason is null && userBreak is not null && SafeUserBreak(userBreak))
            {
                cancelReason = "user";
                cts.Cancel();
            }
            var now = Volatile.Read(ref percent);
            if (now - reported >= ProgressStepPercent)
            {
                reported = now - now % ProgressStepPercent;
                report($"tải tile {reported}%");
            }
        }
        if (task.IsCanceled || (task.IsFaulted && task.Exception!.InnerExceptions.All(e => e is OperationCanceledException)))
            return (null, cancelReason ?? "deadline");
        if (task.IsFaulted && fetcher is HelperTileFetcher)
        {
            HPGeoLog.Warning("tile fetch helper failed: " + (task.Exception!.InnerException?.Message ?? task.Exception.Message));
            return (null, "helper-failed");
        }
        var result = task.GetAwaiter().GetResult();
        report($"tải xong {result.Tiles.Count}/{tiles.Count} tile ({result.FromCache} từ cache)");
        return (result, null);
    }

    private static bool SafeUserBreak(Func<bool> userBreak)
    {
        try { return userBreak(); }
        catch (Exception) { return false; }
    }

    private static void DeleteQuietly(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { }
    }

    /// <summary>IProgress that invokes synchronously on the reporting thread (Progress&lt;T&gt; would marshal through a context).</summary>
    private sealed class ThreadSafeProgress : IProgress<int>
    {
        private readonly Action<int> _onReport;
        public ThreadSafeProgress(Action<int> onReport) => _onReport = onReport;
        public void Report(int value) => _onReport(value);
    }
}
