namespace HPAutoCad.SmartPlot.Pdf;

/// <summary>
/// Service responsible for merging multiple PDF documents into a single document.
/// </summary>
public interface IPdfMergeService
{
    /// <summary>
    /// Merges multiple PDF files into a single destination PDF file asynchronously.
    /// </summary>
    /// <param name="sourcePdfFiles">List of absolute paths to the source PDF files in order.</param>
    /// <param name="destinationPdfPath">Absolute path for the merged output PDF file.</param>
    /// <param name="deleteSourceFilesAfterMerge">Whether to delete source files upon completion.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>True if the merge succeeded, false otherwise.</returns>
    Task<bool> MergeAsync(
        IReadOnlyList<string> sourcePdfFiles,
        string destinationPdfPath,
        bool deleteSourceFilesAfterMerge = true,
        CancellationToken ct = default);

    /// <summary>
    /// Merges multiple PDF files into a single destination PDF file synchronously.
    /// </summary>
    /// <param name="sourcePdfFiles">List of absolute paths to the source PDF files in order.</param>
    /// <param name="destinationPdfPath">Absolute path for the merged output PDF file.</param>
    /// <param name="deleteSourceFilesAfterMerge">Whether to delete source files upon completion.</param>
    /// <returns>True if the merge succeeded, false otherwise.</returns>
    bool Merge(
        IReadOnlyList<string> sourcePdfFiles,
        string destinationPdfPath,
        bool deleteSourceFilesAfterMerge = true);
}
