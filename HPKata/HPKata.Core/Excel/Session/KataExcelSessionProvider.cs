using System;
using System.IO;
using HPKata.Core.Configuration;
using HPKata.Core.Excel.Abstractions;

namespace HPKata.Core.Excel.Session
{
    /// <summary>
    /// Factory providing active or headless Excel sessions, probing open desktop Excel workbooks
    /// (matching by path, name, or structural sheets 'Dam'/'Cot'/'Vach') before ClosedXML fallback.
    /// </summary>
    public static class KataExcelSessionProvider
    {
        private static readonly string[] KataSheetNames = { "Dam", "Cot", "Vach" };

        public static bool IsDesktopExcelOpen
        {
            get
            {
                var apps = ComLateBindingHelper.GetRunningExcelApplications();
                bool hasOpen = apps.Count > 0;
                foreach (var app in apps)
                {
                    ComLateBindingHelper.Release(app);
                }
                return hasOpen;
            }
        }

        public static IKataExcelSession OpenSession(string? workbookPath = null, bool preferCom = true)
        {
            string resolvedPath = ResolveBestWorkbookPath(workbookPath);
            bool isCustomTestPath = !IsDefaultKataWorkbookPath(workbookPath);

            if (preferCom)
            {
                var comSession = TryOpenComSession(resolvedPath, allowStructuralSheetFallback: !isCustomTestPath);
                if (comSession != null)
                {
                    return comSession;
                }
            }

            return new KataClosedXmlSession(resolvedPath);
        }

        /// <summary>
        /// Resolves the best Kata.xlsm path on disk, choosing the most recently modified file
        /// when the caller uses the default Kata workbook path.
        /// </summary>
        public static string ResolveBestWorkbookPath(string? workbookPath)
        {
            if (!IsDefaultKataWorkbookPath(workbookPath))
            {
                return workbookPath!;
            }

            string defaultPath = KataInstallEnvironment.Current.KataWorkbookPath;
            if (!string.Equals(defaultPath, @"C:\HPKata\Kata.xlsm", StringComparison.OrdinalIgnoreCase))
            {
                return defaultPath;
            }

            string hpKataPath = @"C:\HPKata\Kata.xlsm";
            string sysRoot = Path.GetPathRoot(Environment.SystemDirectory) ?? @"C:\";
            string legacyFolder = "kata" + "_pro";
            string legacyPath = Path.Combine(sysRoot, legacyFolder, "Kata.xlsm");

            string bestPath = defaultPath;
            DateTime bestTime = File.Exists(bestPath) ? File.GetLastWriteTimeUtc(bestPath) : DateTime.MinValue;

            CheckCandidate(hpKataPath, ref bestPath, ref bestTime);
            CheckCandidate(legacyPath, ref bestPath, ref bestTime);

            return bestPath;
        }

        private static void CheckCandidate(string candidatePath, ref string bestPath, ref DateTime bestTime)
        {
            if (!File.Exists(candidatePath)) return;
            DateTime candidateTime = File.GetLastWriteTimeUtc(candidatePath);
            if (candidateTime > bestTime)
            {
                bestPath = candidatePath;
                bestTime = candidateTime;
            }
        }

        private static bool IsDefaultKataWorkbookPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return true;
            string nonNullPath = path!;
            string fileName = Path.GetFileName(nonNullPath.Trim());
            if (!string.Equals(fileName, "Kata.xlsm", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string defaultPath = KataInstallEnvironment.Current.KataWorkbookPath;
            return string.Equals(path, defaultPath, StringComparison.OrdinalIgnoreCase)
                || string.Equals(path, @"C:\HPKata\Kata.xlsm", StringComparison.OrdinalIgnoreCase);
        }

        private static IKataExcelSession? TryOpenComSession(string workbookPath, bool allowStructuralSheetFallback)
        {
            var apps = ComLateBindingHelper.GetRunningExcelApplications();
            if (apps.Count == 0) return null;

            for (int i = 0; i < apps.Count; i++)
            {
                object app = apps[i];
                try
                {
                    object? workbooks = ComLateBindingHelper.GetProperty(app, "Workbooks");
                    if (workbooks == null)
                    {
                        ComLateBindingHelper.Release(app);
                        continue;
                    }

                    object? workbook = FindMatchingWorkbook(workbooks, workbookPath, allowStructuralSheetFallback, out string matchedPath);
                    ComLateBindingHelper.Release(workbooks);

                    if (workbook != null)
                    {
                        for (int j = i + 1; j < apps.Count; j++)
                        {
                            ComLateBindingHelper.Release(apps[j]);
                        }
                        string sessionPath = string.IsNullOrWhiteSpace(matchedPath) ? workbookPath : matchedPath;
                        return new KataComExcelSession(app, workbook, sessionPath, ownsApp: false);
                    }
                }
                catch
                {
                    // Continue probing remaining Excel instances
                }

                ComLateBindingHelper.Release(app);
            }

            return null;
        }

        private static object? FindMatchingWorkbook(
            object workbooks, string targetPath, bool allowStructuralSheetFallback, out string matchedFullName)
        {
            matchedFullName = string.Empty;
            int count = Convert.ToInt32(ComLateBindingHelper.GetProperty(workbooks, "Count") ?? 0);
            if (count <= 0) return null;

            string targetFileName = Path.GetFileName(targetPath);
            object? sheetMatchedWb = null;
            string sheetMatchedPath = string.Empty;

            for (int i = 1; i <= count; i++)
            {
                object? wb = ComLateBindingHelper.GetProperty(workbooks, "Item", i);
                if (wb == null) continue;

                string fullName = ComLateBindingHelper.GetProperty(wb, "FullName")?.ToString() ?? string.Empty;
                string name = ComLateBindingHelper.GetProperty(wb, "Name")?.ToString() ?? string.Empty;

                bool exactPathMatch = !string.IsNullOrWhiteSpace(targetPath)
                    && string.Equals(fullName, targetPath, StringComparison.OrdinalIgnoreCase);
                bool fileNameMatch = !string.IsNullOrWhiteSpace(targetFileName)
                    && string.Equals(name, targetFileName, StringComparison.OrdinalIgnoreCase);

                if (exactPathMatch || fileNameMatch)
                {
                    ComLateBindingHelper.Release(sheetMatchedWb);
                    matchedFullName = fullName;
                    return wb;
                }

                if (allowStructuralSheetFallback && sheetMatchedWb == null && HasAnyKataSheet(wb))
                {
                    sheetMatchedWb = wb;
                    sheetMatchedPath = fullName;
                    continue;
                }

                ComLateBindingHelper.Release(wb);
            }

            if (sheetMatchedWb != null)
            {
                matchedFullName = sheetMatchedPath;
                return sheetMatchedWb;
            }

            return null;
        }

        private static bool HasAnyKataSheet(object workbook)
        {
            object? sheets = null;
            try
            {
                sheets = ComLateBindingHelper.GetProperty(workbook, "Worksheets");
                if (sheets == null) return false;

                foreach (string sheetName in KataSheetNames)
                {
                    try
                    {
                        object? sheet = ComLateBindingHelper.GetProperty(sheets, "Item", sheetName);
                        if (sheet != null)
                        {
                            ComLateBindingHelper.Release(sheet);
                            return true;
                        }
                    }
                    catch
                    {
                        // Sheet does not exist in this workbook
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
                ComLateBindingHelper.Release(sheets);
            }
        }
    }
}
