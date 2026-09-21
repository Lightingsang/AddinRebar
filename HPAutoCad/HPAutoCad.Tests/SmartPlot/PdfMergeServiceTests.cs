using System.IO;
using HPAutoCad.SmartPlot.Pdf;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using Xunit;

namespace HPAutoCad.Tests.SmartPlot;

public sealed class PdfMergeServiceTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly PdfMergeService _service = new();

    public PdfMergeServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"PdfMergeTests_{Guid.NewGuid():N}");
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
            // Suppress cleanup exceptions in tests
        }
    }

    private string CreateTestPdf(string fileName, int pageCount, double widthPt = 595.0, double heightPt = 842.0)
    {
        var filePath = Path.Combine(_tempDirectory, fileName);
        using var document = new PdfDocument();
        for (int i = 0; i < pageCount; i++)
        {
            var page = document.AddPage();
            page.Width = PdfSharp.Drawing.XUnit.FromPoint(widthPt);
            page.Height = PdfSharp.Drawing.XUnit.FromPoint(heightPt);
        }
        document.Save(filePath);
        return filePath;
    }

    [Fact]
    public void Merge_MultiplePdfFiles_ProducesMergedPdfWithCorrectPageCount()
    {
        // Arrange
        var file1 = CreateTestPdf("sheet1.pdf", 1, 595, 842); // 1 page (A4 Portrait)
        var file2 = CreateTestPdf("sheet2.pdf", 2, 842, 595); // 2 pages (A4 Landscape)
        var file3 = CreateTestPdf("sheet3.pdf", 1, 1191, 842); // 1 page (A3 Landscape)

        var destination = Path.Combine(_tempDirectory, "output_merged.pdf");

        // Act
        var success = _service.Merge([file1, file2, file3], destination, deleteSourceFilesAfterMerge: false);

        // Assert
        Assert.True(success);
        Assert.True(File.Exists(destination));

        using var merged = PdfReader.Open(destination, PdfDocumentOpenMode.Import);
        Assert.Equal(4, merged.PageCount);

        // Verify page dimensions are preserved
        Assert.Equal(595, Math.Round(merged.Pages[0].Width.Point));
        Assert.Equal(842, Math.Round(merged.Pages[0].Height.Point));

        Assert.Equal(842, Math.Round(merged.Pages[1].Width.Point));
        Assert.Equal(595, Math.Round(merged.Pages[1].Height.Point));

        Assert.Equal(1191, Math.Round(merged.Pages[3].Width.Point));
        Assert.Equal(842, Math.Round(merged.Pages[3].Height.Point));
    }

    [Fact]
    public async Task MergeAsync_MultiplePdfFiles_ProducesMergedPdfAsync()
    {
        // Arrange
        var file1 = CreateTestPdf("async_sheet1.pdf", 2);
        var file2 = CreateTestPdf("async_sheet2.pdf", 3);
        var destination = Path.Combine(_tempDirectory, "async_merged.pdf");

        // Act
        var success = await _service.MergeAsync(
            [file1, file2],
            destination,
            deleteSourceFilesAfterMerge: false,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(success);
        Assert.True(File.Exists(destination));

        using var merged = PdfReader.Open(destination, PdfDocumentOpenMode.Import);
        Assert.Equal(5, merged.PageCount);
    }

    [Fact]
    public void Merge_DeleteSourceFilesTrue_SafelyDeletesSourceFilesAfterMerge()
    {
        // Arrange
        var file1 = CreateTestPdf("temp1.pdf", 1);
        var file2 = CreateTestPdf("temp2.pdf", 1);
        var destination = Path.Combine(_tempDirectory, "deleted_source_merged.pdf");

        Assert.True(File.Exists(file1));
        Assert.True(File.Exists(file2));

        // Act
        var success = _service.Merge([file1, file2], destination, deleteSourceFilesAfterMerge: true);

        // Assert
        Assert.True(success);
        Assert.True(File.Exists(destination));

        using var merged = PdfReader.Open(destination, PdfDocumentOpenMode.Import);
        Assert.Equal(2, merged.PageCount);

        // Source files must have been cleaned up
        Assert.False(File.Exists(file1), "Source file 1 was not deleted.");
        Assert.False(File.Exists(file2), "Source file 2 was not deleted.");
    }

    [Fact]
    public void Merge_DeleteSourceFilesFalse_PreservesSourceFiles()
    {
        // Arrange
        var file1 = CreateTestPdf("keep1.pdf", 1);
        var file2 = CreateTestPdf("keep2.pdf", 1);
        var destination = Path.Combine(_tempDirectory, "kept_source_merged.pdf");

        // Act
        var success = _service.Merge([file1, file2], destination, deleteSourceFilesAfterMerge: false);

        // Assert
        Assert.True(success);
        Assert.True(File.Exists(destination));
        Assert.True(File.Exists(file1));
        Assert.True(File.Exists(file2));
    }

    [Fact]
    public void Merge_CreatesDestinationDirectory_WhenDirectoryDoesNotExist()
    {
        // Arrange
        var file = CreateTestPdf("single.pdf", 1);
        var nestedDir = Path.Combine(_tempDirectory, "Nested", "SubFolder");
        var destination = Path.Combine(nestedDir, "nested_merged.pdf");

        Assert.False(Directory.Exists(nestedDir));

        // Act
        var success = _service.Merge([file], destination, deleteSourceFilesAfterMerge: false);

        // Assert
        Assert.True(success);
        Assert.True(Directory.Exists(nestedDir));
        Assert.True(File.Exists(destination));
    }

    [Fact]
    public void Merge_EmptySourceList_ThrowsArgumentException()
    {
        var destination = Path.Combine(_tempDirectory, "empty.pdf");

        Assert.Throws<FileNotFoundException>(() =>
            _service.Merge([], destination));
    }

    [Fact]
    public void Merge_NullOrWhitespaceDestination_ThrowsArgumentException()
    {
        var file = CreateTestPdf("valid.pdf", 1);

        Assert.Throws<ArgumentException>(() =>
            _service.Merge([file], ""));

        Assert.Throws<ArgumentException>(() =>
            _service.Merge([file], "   "));
    }

    [Fact]
    public void Merge_NonExistentFiles_ThrowsFileNotFoundException()
    {
        var nonExistent1 = Path.Combine(_tempDirectory, "non_existent_1.pdf");
        var nonExistent2 = Path.Combine(_tempDirectory, "non_existent_2.pdf");
        var destination = Path.Combine(_tempDirectory, "should_not_exist.pdf");

        Assert.Throws<FileNotFoundException>(() =>
            _service.Merge([nonExistent1, nonExistent2], destination));
    }

    [Fact]
    public async Task MergeAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var file1 = CreateTestPdf("cancel1.pdf", 1);
        var file2 = CreateTestPdf("cancel2.pdf", 1);
        var destination = Path.Combine(_tempDirectory, "canceled.pdf");

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await _service.MergeAsync([file1, file2], destination, false, cts.Token);
        });
    }

    [Fact]
    public void Merge_StressTest_MergesTenFilesWithMultiplePages()
    {
        // Arrange: 10 files with 5 pages each = 50 pages total
        const int fileCount = 10;
        const int pagesPerFile = 5;
        var files = new List<string>(fileCount);

        for (int i = 0; i < fileCount; i++)
        {
            files.Add(CreateTestPdf($"stress_{i}.pdf", pagesPerFile));
        }

        var destination = Path.Combine(_tempDirectory, "stress_merged.pdf");

        // Act
        var success = _service.Merge(files, destination, deleteSourceFilesAfterMerge: true);

        // Assert
        Assert.True(success);
        Assert.True(File.Exists(destination));

        using var merged = PdfReader.Open(destination, PdfDocumentOpenMode.Import);
        Assert.Equal(fileCount * pagesPerFile, merged.PageCount);

        foreach (var file in files)
        {
            Assert.False(File.Exists(file));
        }
    }
}
