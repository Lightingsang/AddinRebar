using System.Security.Cryptography;

namespace HPFoundation.App.Excel;

/// <summary>A private copy of the engineer's workbook, with what is needed to notice the original changing.</summary>
public sealed record WorkbookCopy(string SourcePath, string CopyPath, string SourceSha256, DateTime SourceLastWriteUtc)
{
    public static WorkbookCopy Create(string sourcePath, string runFolder)
    {
        Directory.CreateDirectory(runFolder);
        var copyPath = Path.Combine(runFolder, Path.GetFileName(sourcePath));
        var lastWriteUtc = File.GetLastWriteTimeUtc(sourcePath);

        var bytes = ReadSharedBytes(sourcePath);
        File.WriteAllBytes(copyPath, bytes);

        return new WorkbookCopy(sourcePath, copyPath, Sha256(bytes), lastWriteUtc);
    }

    public bool IsSourceUnchanged() => Sha256(ReadSharedBytes(SourcePath)) == SourceSha256;

    /// <summary>The source lives on a shared drive and may be open in someone's Excel, so never ask for exclusive access.</summary>
    private static byte[] ReadSharedBytes(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    private static string Sha256(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
}
