namespace Installer;

using System.IO;
using WixSharp;

public static class Generator
{
    /// <summary>
    /// Recursively creates a WixSharp Dir tree mirroring the source directory.
    /// </summary>
    public static Dir CreateDirTree(string sourcePath, Feature feature, string dirName)
    {
        var dirInfo = new DirectoryInfo(sourcePath);
        var entities = new List<WixEntity>();

        var files = dirInfo.GetFiles();
        if (files.Length > 0)
        {
            entities.Add(new DirFiles(feature, Path.Combine(sourcePath, "*.*")));
        }

        foreach (var subDir in dirInfo.GetDirectories())
        {
            entities.Add(CreateDirTree(subDir.FullName, feature, subDir.Name));
        }

        return new Dir(feature, dirName, entities.ToArray());
    }
}
