using System.Diagnostics;
using System.IO;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.PlottingServices;
using HPAutoCad.Core.SmartPlot.Models;
using HPAutoCad.Core.SmartPlot.Services;
using HPAutoCad.SmartPlot.Pdf;

namespace HPAutoCad.SmartPlot.Cad.Plot;

/// <summary>
/// Core plotting engine orchestrating AutoCAD's native PlotEngine pipeline,
/// applying non-destructive PlotSettings via PlotInfo.OverrideSettings,
/// managing BACKGROUNDPLOT/CMDECHO system variables, driving native PlotProgressDialog,
/// and coordinating single or merged PDF publishing.
/// </summary>
public sealed class AutoCadPlotEngine : IAutoCadPlotEngine
{
    private readonly IFileNameService _fileNameService;
    private readonly IPdfMergeService _pdfMergeService;

    public AutoCadPlotEngine(
        IFileNameService? fileNameService = null,
        IPdfMergeService? pdfMergeService = null)
    {
        _fileNameService = fileNameService ?? new FileNameService();
        _pdfMergeService = pdfMergeService ?? new PdfMergeService();
    }

    /// <inheritdoc />
    public Task<PlotResult> PlotAsync(
        PlotConfiguration config,
        IReadOnlyList<PlotItem> items,
        IProgress<PlotProgressUpdate>? progress = null,
        CancellationToken ct = default)
    {
        var doc = Application.DocumentManager.MdiActiveDocument;
        if (doc is null)
        {
            return Task.FromResult(PlotResult.Failed("No active AutoCAD document found."));
        }

        return PlotAsync(doc, config, items, progress, ct);
    }

    /// <inheritdoc />
    public async Task<PlotResult> PlotAsync(
        Document doc,
        PlotConfiguration config,
        IReadOnlyList<PlotItem> items,
        IProgress<PlotProgressUpdate>? progress = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(doc);
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(items);

        var stopwatch = Stopwatch.StartNew();

        // Check if AutoCAD is currently busy plotting
        if (PlotFactory.ProcessPlotState != ProcessPlotState.NotPlotting)
        {
            return PlotResult.Failed("Another plot operation is already in progress in AutoCAD. Please wait for it to complete.");
        }

        var selectedItems = items.Where(i => i.IsSelected && i.Bounds.IsValid).ToList();
        if (selectedItems.Count == 0)
        {
            return PlotResult.Failed("No valid plot items selected for plotting.");
        }

        object? originalBackgroundPlot = null;
        object? originalCmdEcho = null;
        var generatedFiles = new List<string>();
        string? finalMergedPdf = null;
        bool mergedSuccessfully = false;
        bool isCancelled = false;
        bool isMerged = config.OutputMode == OutputMode.MergedPdf;

        // Ensure destination directory exists
        if (!string.IsNullOrWhiteSpace(config.OutputFolder) && !Directory.Exists(config.OutputFolder))
        {
            Directory.CreateDirectory(config.OutputFolder);
        }

        try
        {
            using (var docLock = doc.LockDocument())
            {
                // Suppress background plotting and command echoing
                try
                {
                    originalBackgroundPlot = Application.GetSystemVariable("BACKGROUNDPLOT");
                    originalCmdEcho = Application.GetSystemVariable("CMDECHO");
                    Application.SetSystemVariable("BACKGROUNDPLOT", 0);
                    Application.SetSystemVariable("CMDECHO", 0);
                }
                catch
                {
                    // Proceed if system variables cannot be modified
                }

                try
                {
                    var db = doc.Database;

                    using var progressDialog = new PlotProgressDialog(false, selectedItems.Count, true);
                    progressDialog.set_PlotMsgString(PlotMessageIndex.DialogTitle, "Smart Plot Pro - Publishing");
                    progressDialog.set_PlotMsgString(PlotMessageIndex.CancelJobButtonMessage, "Canceling batch plot...");
                    progressDialog.set_PlotMsgString(PlotMessageIndex.CancelSheetButtonMessage, "Canceling current sheet...");
                    progressDialog.set_PlotMsgString(PlotMessageIndex.SheetProgressCaption, "Sheet Progress");
                    progressDialog.set_PlotMsgString(PlotMessageIndex.SheetSetProgressCaption, "Job Progress");
                    progressDialog.LowerPlotProgressRange = 0;
                    progressDialog.UpperPlotProgressRange = 100;
                    progressDialog.PlotProgressPos = 0;
                    progressDialog.OnBeginPlot();
                    progressDialog.IsVisible = true;

                    using var engine = PlotFactory.CreatePublishEngine();
                    bool isPlotStarted = false;

                    try
                    {
                        engine.BeginPlot(progressDialog, null);
                        isPlotStarted = true;

                        for (int i = 0; i < selectedItems.Count; i++)
                        {
                            if (ct.IsCancellationRequested || progressDialog.IsPlotCancelled)
                            {
                                isCancelled = true;
                                break;
                            }

                            var item = selectedItems[i];
                            string sheetName = string.IsNullOrWhiteSpace(item.DisplayName)
                                ? $"Sheet {i + 1}"
                                : item.DisplayName;

                            progress?.Report(new PlotProgressUpdate
                            {
                                CurrentIndex = i + 1,
                                TotalCount = selectedItems.Count,
                                SheetName = sheetName,
                                StatusMessage = $"Plotting ({i + 1}/{selectedItems.Count}): {sheetName}..."
                            });

                            progressDialog.set_PlotMsgString(PlotMessageIndex.SheetName, sheetName);
                            progressDialog.LowerSheetProgressRange = 0;
                            progressDialog.UpperSheetProgressRange = 100;
                            progressDialog.SheetProgressPos = 0;
                            progressDialog.OnBeginSheet();

                            string sheetOutputPath;
                            if (isMerged)
                            {
                                sheetOutputPath = Path.Combine(Path.GetTempPath(), $"HPPlot_{Guid.NewGuid():N}.pdf");
                            }
                            else
                            {
                                string fileName = _fileNameService.Format(
                                    config.FileNamePattern,
                                    item,
                                    config.FileNamePrefix,
                                    doc.Name);

                                sheetOutputPath = Path.Combine(config.OutputFolder, fileName);
                            }

                            var sheetDir = Path.GetDirectoryName(sheetOutputPath);
                            if (!string.IsNullOrEmpty(sheetDir) && !Directory.Exists(sheetDir))
                            {
                                Directory.CreateDirectory(sheetDir);
                            }

                            using (var tr = db.TransactionManager.StartTransaction())
                            {
                                var layoutDict = (DBDictionary)tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead);
                                ObjectId layoutId;
                                if (layoutDict.Contains(item.LayoutName))
                                {
                                    layoutId = layoutDict.GetAt(item.LayoutName);
                                }
                                else
                                {
                                    var space = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForRead);
                                    layoutId = space.LayoutId;
                                }

                                var layout = (Layout)tr.GetObject(layoutId, OpenMode.ForRead);

                                using var plotSettings = new PlotSettings(layout.ModelType);
                                plotSettings.CopyFrom(layout);

                                var psv = PlotSettingsValidator.Current;

                                // Configure printer and paper size
                                psv.SetPlotConfigurationName(plotSettings, config.DeviceName, config.MediaName);
                                psv.RefreshLists(plotSettings);

                                // Configure plot window area
                                psv.SetPlotType(plotSettings, Autodesk.AutoCAD.DatabaseServices.PlotType.Window);
                                psv.SetPlotWindowArea(plotSettings, new Extents2d(item.MinX, item.MinY, item.MaxX, item.MaxY));
                                psv.SetPlotCentered(plotSettings, config.CenterPlot);

                                // Scale settings
                                if (config.FitToPaper)
                                {
                                    psv.SetUseStandardScale(plotSettings, false);
                                    psv.SetStdScaleType(plotSettings, StdScaleType.ScaleToFit);
                                }
                                else
                                {
                                    psv.SetUseStandardScale(plotSettings, false);
                                    psv.SetCustomPrintScale(plotSettings, new CustomScale(config.CustomScaleNumerator, config.CustomScaleDenominator));
                                }

                                // Auto orientation detection
                                var rotation = DetermineRotation(config.Orientation, item.Bounds);
                                psv.SetPlotRotation(plotSettings, rotation);

                                // Style sheet
                                if (!string.IsNullOrWhiteSpace(config.PlotStyle))
                                {
                                    try
                                    {
                                        psv.SetCurrentStyleSheet(plotSettings, config.PlotStyle);
                                    }
                                    catch
                                    {
                                        // If style sheet not found, fallback silently
                                    }
                                }

                                // Prepare PlotInfo with non-destructive OverrideSettings
                                using var plotInfo = new PlotInfo
                                {
                                    Layout = layout.ObjectId,
                                    OverrideSettings = plotSettings
                                };

                                var plotInfoValidator = new PlotInfoValidator
                                {
                                    MediaMatchingPolicy = MatchingPolicy.MatchEnabled
                                };
                                plotInfoValidator.Validate(plotInfo);

                                tr.Commit();

                                // Execute sheet plot
                                engine.BeginDocument(plotInfo, doc.Name, null, 1, true, sheetOutputPath);

                                using var pageInfo = new PlotPageInfo();
                                engine.BeginPage(pageInfo, plotInfo, true, null);
                                engine.BeginGenerateGraphics(null);
                                engine.EndGenerateGraphics(null);
                                engine.EndPage(null);
                                engine.EndDocument(null);
                            }

                            progressDialog.SheetProgressPos = 100;
                            progressDialog.OnEndSheet();

                            int overallPercent = (int)((double)(i + 1) / selectedItems.Count * 100);
                            progressDialog.PlotProgressPos = Math.Clamp(overallPercent, 0, 100);

                            generatedFiles.Add(sheetOutputPath);
                        }
                    }
                    finally
                    {
                        if (isPlotStarted)
                        {
                            try { engine.EndPlot(null); } catch { }
                            try { engine.Destroy(); } catch { }
                        }
                        try { progressDialog.OnEndPlot(); } catch { }
                        try { progressDialog.Destroy(); } catch { }
                    }
                }
                finally
                {
                    // Restore system variables on AutoCAD main thread
                    if (originalBackgroundPlot is not null)
                    {
                        try { Application.SetSystemVariable("BACKGROUNDPLOT", originalBackgroundPlot); } catch { }
                    }
                    if (originalCmdEcho is not null)
                    {
                        try { Application.SetSystemVariable("CMDECHO", originalCmdEcho); } catch { }
                    }
                }
            } // docLock disposed here on AutoCAD main thread
        }
        catch (System.Exception ex)
        {
            stopwatch.Stop();

            // Clean up temporary files on error if in merged mode
            if (isMerged)
            {
                foreach (var file in generatedFiles)
                {
                    try { if (File.Exists(file)) File.Delete(file); } catch { }
                }
            }

            return PlotResult.Failed($"Plot execution failed: {ex.Message}", selectedItems.Count, generatedFiles.Count);
        }

        // Handle cancellation
        if (isCancelled || ct.IsCancellationRequested)
        {
            stopwatch.Stop();

            if (isMerged)
            {
                foreach (var file in generatedFiles)
                {
                    try { if (File.Exists(file)) File.Delete(file); } catch { }
                }
            }

            progress?.Report(new PlotProgressUpdate
            {
                CurrentIndex = generatedFiles.Count,
                TotalCount = selectedItems.Count,
                StatusMessage = "Plotting was cancelled by user.",
                IsCompleted = true
            });

            return PlotResult.Failed("Plotting was cancelled by user.", selectedItems.Count, generatedFiles.Count);
        }

        // Handle PDF merging outside docLock on the main thread
        if (isMerged && generatedFiles.Count > 0)
        {
            progress?.Report(new PlotProgressUpdate
            {
                CurrentIndex = generatedFiles.Count,
                TotalCount = selectedItems.Count,
                StatusMessage = "Merging PDF files into single document...",
                IsCompleted = false
            });

            finalMergedPdf = Path.Combine(config.OutputFolder, config.MergedFileName);
            try
            {
                mergedSuccessfully = await _pdfMergeService.MergeAsync(
                    generatedFiles,
                    finalMergedPdf,
                    deleteSourceFilesAfterMerge: true,
                    ct);
            }
            catch (System.Exception ex)
            {
                stopwatch.Stop();
                foreach (var file in generatedFiles)
                {
                    try { if (File.Exists(file)) File.Delete(file); } catch { }
                }

                return PlotResult.Failed($"PDF merge failed: {ex.Message}", selectedItems.Count, generatedFiles.Count);
            }

            if (!mergedSuccessfully)
            {
                stopwatch.Stop();
                foreach (var file in generatedFiles)
                {
                    try { if (File.Exists(file)) File.Delete(file); } catch { }
                }

                return PlotResult.Failed("Failed to merge plotted PDF files.", selectedItems.Count, generatedFiles.Count);
            }
        }

        stopwatch.Stop();

        progress?.Report(new PlotProgressUpdate
        {
            CurrentIndex = selectedItems.Count,
            TotalCount = selectedItems.Count,
            StatusMessage = "Plotting complete!",
            IsCompleted = true
        });

        return PlotResult.Succeeded(
            files: isMerged ? (finalMergedPdf is not null ? [finalMergedPdf] : []) : generatedFiles,
            merged: isMerged ? finalMergedPdf : null,
            elapsed: stopwatch.Elapsed,
            totalSheets: selectedItems.Count,
            plottedSheets: generatedFiles.Count);
    }

    private static PlotRotation DetermineRotation(OrientationMode orientation, PlotBounds bounds)
    {
        return orientation switch
        {
            OrientationMode.Auto => bounds.IsLandscape ? PlotRotation.Degrees000 : PlotRotation.Degrees090,
            OrientationMode.Portrait => PlotRotation.Degrees090,
            OrientationMode.Landscape => PlotRotation.Degrees000,
            _ => PlotRotation.Degrees000
        };
    }
}
