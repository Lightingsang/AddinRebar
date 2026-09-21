using System.IO;
using HPAutoCad.SmartPlot.Pdf;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using Xunit;

namespace HPAutoCad.Tests.SmartPlot;

public sealed class PdfMergeServiceStressTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly PdfMergeService _service = new();

    public PdfMergeServiceStressTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"PdfStressTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
        }
        catch
        {
            // Suppress cleanup exceptions
        }
    }

    private string CreateValidPdf(string fileName, int pageCount = 1)
    {
        var filePath = Path.Combine(_tempDirectory, fileName);
        using var doc = new PdfDocument();
        for (int i = 0; i < pageCount; i++)
        {
            var page = doc.AddPage();
            page.Width = PdfSharp.Drawing.XUnit.FromPoint(595);
            page.Height = PdfSharp.Drawing.XUnit.FromPoint(842);
        }
        doc.Save(filePath);
        return filePath;
    }

    private string CreateCorruptPdf(string fileName)
    {
        var filePath = Path.Combine(_tempDirectory, fileName);
        File.WriteAllText(filePath, "This is not a valid PDF file! Garbage content %%% %%EOF");
        return filePath;
    }

    private string CreateZeroByteFile(string fileName)
    {
        var filePath = Path.Combine(_tempDirectory, fileName);
        File.WriteAllBytes(filePath, []);
        return filePath;
    }

    [Fact]
    public void Merge_SinglePdfFile_PreservesPageCountAndCleansUp()
    {
        var source = CreateValidPdf("single.pdf", 3);
        var dest = Path.Combine(_tempDirectory, "single_out.pdf");

        var result = _service.Merge([source], dest, deleteSourceFilesAfterMerge: true);

        Assert.True(result);
        Assert.True(File.Exists(dest));
        Assert.False(File.Exists(source));

        using var output = PdfReader.Open(dest, PdfDocumentOpenMode.Import);
        Assert.Equal(3, output.PageCount);
    }

    [Fact]
    public void Merge_CorruptPdfFile_ThrowsException_AndCheckSourceFilePreservation()
    {
        // ARRANGE: A valid source PDF and a corrupted file
        var validFile = CreateValidPdf("valid_keep.pdf", 2);
        var corruptFile = CreateCorruptPdf("corrupt.pdf");
        var dest = Path.Combine(_tempDirectory, "out_corrupt_test.pdf");

        Assert.True(File.Exists(validFile));
        Assert.True(File.Exists(corruptFile));

        // ACT & ASSERT: Merging should fail with an exception
        Assert.ThrowsAny<Exception>(() =>
        {
            _service.Merge([validFile, corruptFile], dest, deleteSourceFilesAfterMerge: true);
        });

        // BUG CHECK: Did the service delete the valid source file even though merge failed?
        // In resilient software, source files MUST NOT be deleted if the merge operation fails.
        bool validFileStillExists = File.Exists(validFile);
        Assert.True(validFileStillExists, "DATA LOSS BUG: Valid source file was deleted by finally block even though merge failed!");
    }

    [Fact]
    public void Merge_DestinationFileLocked_ThrowsException_AndCheckSourceFilePreservation()
    {
        // ARRANGE: Two valid source files, but destination is locked by another process
        var file1 = CreateValidPdf("src1.pdf", 1);
        var file2 = CreateValidPdf("src2.pdf", 1);
        var dest = Path.Combine(_tempDirectory, "locked_dest.pdf");

        using (var lockStream = new FileStream(dest, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
        {
            // ACT & ASSERT: Merging should fail because destination is locked
            Assert.ThrowsAny<Exception>(() =>
            {
                _service.Merge([file1, file2], dest, deleteSourceFilesAfterMerge: true);
            });
        }

        // BUG CHECK: Were source files deleted even though destination could not be written?
        bool file1Exists = File.Exists(file1);
        bool file2Exists = File.Exists(file2);

        Assert.True(file1Exists, "DATA LOSS BUG: Source file 1 was deleted even though destination was locked!");
        Assert.True(file2Exists, "DATA LOSS BUG: Source file 2 was deleted even though destination was locked!");
    }

    [Fact]
    public async Task MergeAsync_CancellationMidOperation_CheckSourceFilePreservation()
    {
        // ARRANGE
        var file1 = CreateValidPdf("cancel_src1.pdf", 5);
        var file2 = CreateValidPdf("cancel_src2.pdf", 5);
        var dest = Path.Combine(_tempDirectory, "canceled_dest.pdf");

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Pre-canceled

        // ACT & ASSERT
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await _service.MergeAsync([file1, file2], dest, deleteSourceFilesAfterMerge: true, cts.Token);
        });

        // BUG CHECK: Were source files deleted when canceled before completion?
        bool file1Exists = File.Exists(file1);
        bool file2Exists = File.Exists(file2);

        Assert.True(file1Exists, "DATA LOSS BUG: Source file 1 was deleted upon cancellation!");
        Assert.True(file2Exists, "DATA LOSS BUG: Source file 2 was deleted upon cancellation!");
    }

    [Fact]
    public void MergeInternal_CancellationInsideTryBlock_DeletesSourceFilesBug()
    {
        // ARRANGE: Call MergeInternal directly with pre-canceled token so it throws inside try block
        var file1 = CreateValidPdf("cancel_try1.pdf", 2);
        var file2 = CreateValidPdf("cancel_try2.pdf", 2);
        var dest = Path.Combine(_tempDirectory, "cancel_try_dest.pdf");

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Pre-canceled

        var method = typeof(PdfMergeService).GetMethod("MergeInternal", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        // ACT & ASSERT
        var ex = Assert.Throws<System.Reflection.TargetInvocationException>(() =>
        {
            method!.Invoke(null, [new List<string> { file1, file2 }, dest, true, cts.Token]);
        });

        Assert.IsAssignableFrom<OperationCanceledException>(ex.InnerException);

        // BUG CHECK: Did finally delete the source files even though the operation was canceled?
        bool file1Exists = File.Exists(file1);
        bool file2Exists = File.Exists(file2);

        Assert.True(file1Exists, "DATA LOSS BUG: Source file 1 was deleted when canceled inside try block!");
        Assert.True(file2Exists, "DATA LOSS BUG: Source file 2 was deleted when canceled inside try block!");
    }

    [Fact]
    public void Merge_DestinationIsSameAsSource_CheckOutputPreservation()
    {
        // ARRANGE: Destination is the same path as one of the input files
        var file1 = CreateValidPdf("same_path_1.pdf", 1);
        var file2 = CreateValidPdf("same_path_2.pdf", 1);

        // ACT: Merge into file1 with deleteSourceFilesAfterMerge: true
        _service.Merge([file1, file2], file1, deleteSourceFilesAfterMerge: true);

        // BUG CHECK: Did finally delete the destination file because it was in existingFiles?
        bool destExists = File.Exists(file1);
        Assert.True(destExists, "BUG: Destination file was deleted by finally block because it matched a source path!");
    }

    [Fact]
    public void Merge_ZeroByteFile_ThrowsException_AndCheckSourceFilePreservation()
    {
        var validFile = CreateValidPdf("valid_for_zero.pdf", 1);
        var zeroFile = CreateZeroByteFile("empty_zero.pdf");
        var dest = Path.Combine(_tempDirectory, "out_zero.pdf");

        Assert.ThrowsAny<Exception>(() =>
        {
            _service.Merge([validFile, zeroFile], dest, deleteSourceFilesAfterMerge: true);
        });

        Assert.True(File.Exists(validFile), "DATA LOSS BUG: Valid source file was deleted when zero-byte file caused merge failure!");
    }

    [Fact]
    public void Merge_SourceFileLockedByReader_ThrowsException_AndCheckSourcePreservation()
    {
        var file1 = CreateValidPdf("locked_read1.pdf", 1);
        var file2 = CreateValidPdf("locked_read2.pdf", 1);
        var dest = Path.Combine(_tempDirectory, "out_locked_read.pdf");

        // Lock file2 exclusively
        using (var lockStream = new FileStream(file2, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            Assert.ThrowsAny<Exception>(() =>
            {
                _service.Merge([file1, file2], dest, deleteSourceFilesAfterMerge: true);
            });
        }

        // File1 should not have been deleted
        Assert.True(File.Exists(file1), "DATA LOSS BUG: File 1 was deleted when File 2 could not be read!");
    }

    [Fact]
    public void Merge_PartialNonExistentFiles_ChecksBehavior()
    {
        var file1 = CreateValidPdf("real_part.pdf", 2);
        var missingFile = Path.Combine(_tempDirectory, "phantom.pdf");
        var dest = Path.Combine(_tempDirectory, "out_part.pdf");

        // If one file is missing, does it throw or silently skip?
        // Let's test the actual behavior:
        try
        {
            var result = _service.Merge([file1, missingFile], dest, deleteSourceFilesAfterMerge: false);
            // If it succeeded, it silently skipped phantom.pdf
            Assert.True(result);
            using var output = PdfReader.Open(dest, PdfDocumentOpenMode.Import);
            Assert.Equal(2, output.PageCount);
        }
        catch (FileNotFoundException)
        {
            // Or if it throws, that is strict behavior
        }
    }

    [Fact]
    public void Merge_StressTest_100Pages_20Files_PreservesAllPages()
    {
        const int fileCount = 20;
        const int pagesPerFile = 5;
        var files = new List<string>(fileCount);

        for (int i = 0; i < fileCount; i++)
        {
            files.Add(CreateValidPdf($"perf_{i}.pdf", pagesPerFile));
        }

        var dest = Path.Combine(_tempDirectory, "perf_merged_100.pdf");

        var result = _service.Merge(files, dest, deleteSourceFilesAfterMerge: true);

        Assert.True(result);
        Assert.True(File.Exists(dest));

        using var output = PdfReader.Open(dest, PdfDocumentOpenMode.Import);
        Assert.Equal(fileCount * pagesPerFile, output.PageCount);

        foreach (var f in files)
        {
            Assert.False(File.Exists(f));
        }
    }
}
