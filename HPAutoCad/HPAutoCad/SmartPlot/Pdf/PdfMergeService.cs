using System.IO;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace HPAutoCad.SmartPlot.Pdf;

/// <summary>
/// In-process PDF merging service implemented via PdfSharp v6.
/// Merges multiple single-page or multi-page PDFs into a unified PDF file
/// and provides resilient cleanup of temporary source files.
/// </summary>
public sealed class PdfMergeService : IPdfMergeService
{
    /// <inheritdoc />
    public async Task<bool> MergeAsync(
        IReadOnlyList<string> sourcePdfFiles,
        string destinationPdfPath,
        bool deleteSourceFilesAfterMerge = true,
        CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();
            return MergeInternal(sourcePdfFiles, destinationPdfPath, deleteSourceFilesAfterMerge, ct);
        }, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public bool Merge(
        IReadOnlyList<string> sourcePdfFiles,
        string destinationPdfPath,
        bool deleteSourceFilesAfterMerge = true)
    {
        return MergeInternal(sourcePdfFiles, destinationPdfPath, deleteSourceFilesAfterMerge, CancellationToken.None);
    }

    private static bool MergeInternal(
        IReadOnlyList<string> sourcePdfFiles,
        string destinationPdfPath,
        bool deleteSourceFilesAfterMerge,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(sourcePdfFiles);
        if (string.IsNullOrWhiteSpace(destinationPdfPath))
        {
            throw new ArgumentException("Destination PDF path cannot be null or empty.", nameof(destinationPdfPath));
        }

        var existingFiles = sourcePdfFiles
            .Where(f => !string.IsNullOrWhiteSpace(f) && File.Exists(f))
            .ToList();

        if (existingFiles.Count == 0)
        {
            throw new FileNotFoundException("None of the specified source PDF files exist.");
        }

        bool mergeSucceeded = false;
        try
        {
            using var outputDocument = new PdfDocument();

            foreach (var filePath in existingFiles)
            {
                ct.ThrowIfCancellationRequested();

                using var inputDocument = PdfReader.Open(filePath, PdfDocumentOpenMode.Import);
                int pageCount = inputDocument.PageCount;
                for (int i = 0; i < pageCount; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    var page = inputDocument.Pages[i];
                    outputDocument.AddPage(page);
                }
            }

            if (outputDocument.PageCount == 0)
            {
                return false;
            }

            var destDir = Path.GetDirectoryName(destinationPdfPath);
            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            outputDocument.Save(destinationPdfPath);
            mergeSucceeded = true;
            return true;
        }
        finally
        {
            if (deleteSourceFilesAfterMerge && mergeSucceeded)
            {
                string fullDestPath = Path.GetFullPath(destinationPdfPath);
                foreach (var file in existingFiles)
                {
                    if (string.Equals(Path.GetFullPath(file), fullDestPath, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    TryDeleteFileWithRetry(file);
                }
            }
        }
    }

    /// <summary>
    /// Resiliently attempts to delete a file, retrying briefly if locked by another process.
    /// </summary>
    private static void TryDeleteFileWithRetry(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return;
        }

        const int maxAttempts = 5;
        const int delayMs = 50;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                File.Delete(filePath);
                return;
            }
            catch (IOException) when (attempt < maxAttempts)
            {
                Thread.Sleep(delayMs);
            }
            catch (UnauthorizedAccessException) when (attempt < maxAttempts)
            {
                Thread.Sleep(delayMs);
            }
            catch
            {
                // Silently cease retries on permanent failures or access limits
                break;
            }
        }
    }
}
