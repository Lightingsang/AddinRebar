using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using HPKata.Core.Excel;
using HPKata.Core.Excel.Data;

namespace HPKata.Installer.Services
{
    /// <summary>
    /// Pure C# runner that reads beam detailing data from C:\HPKata\Kata.xlsm and executes
    /// the complete beam shop drawing in AutoCAD 2026 via HPAutoCad.Mcp.Server.exe.
    /// </summary>
    public static class AutocadMcpBeamRunner
    {
        public const string DefaultExcelPath = @"C:\HPKata\Kata.xlsm";
        public const string DefaultOutDwgPath = @"C:\HPKata\HPKata_Beam_B01_Demo.dwg";
        private const string AcadExePath = @"C:\Program Files\Autodesk\AutoCAD 2026\acad.exe";
        private const string PipePath = @"\\.\pipe\hpautocad-mcp-2026";

        private delegate bool EnumChildProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool EnumChildWindows(IntPtr hWndParent, EnumChildProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        public static int Execute(string? excelPath, string? outDwgPath, bool closeAcadWhenDone, Action<string> log)
        {
            string resolvedExcel = string.IsNullOrWhiteSpace(excelPath) ? DefaultExcelPath : excelPath;
            string resolvedDwg = string.IsNullOrWhiteSpace(outDwgPath) ? DefaultOutDwgPath : outDwgPath;

            if (!KataExcelAdapterProvider.Current.ReadBeamSheet(resolvedExcel, out KataBeamSheetData beamData))
            {
                log($"[HPKata C#] ERROR: Failed to read sheet 'Dam' from '{resolvedExcel}'.");
                return 1;
            }

            LogBeamSummary(beamData, resolvedExcel, log);
            string workingDwg = Path.Combine(Path.GetDirectoryName(resolvedDwg) ?? @"C:\HPKata", "HPKata_Mcp_Working.dwg");
            PrepareSeedDwg(workingDwg, resolvedDwg, log);

            string serverExe = ResolveMcpServerExe();
            Process? startedAcad = EnsureAutocadRunningWithDrawing(workingDwg, log);
            try
            {
                if (!WaitForPipeAvailable(log))
                {
                    log("[HPKata C#] ERROR: AutoCAD MCP pipe did not come up within timeout.");
                    return 1;
                }

                using var client = new AutocadMcpStdioClient(serverExe);
                if (!WaitForDrawingReady(client, log))
                {
                    log("[HPKata C#] ERROR: AutoCAD MCP bridge did not report an open drawing within timeout.");
                    return 1;
                }

                RunMcpBeamWorkflow(client, beamData, resolvedDwg, log);
                return 0;
            }
            finally
            {
                if (closeAcadWhenDone && startedAcad is { HasExited: false })
                {
                    try { startedAcad.Kill(entireProcessTree: true); startedAcad.WaitForExit(3000); } catch { /* ignore */ }
                }
                CleanupLockFiles(workingDwg, resolvedDwg);
            }
        }

        private static void LogBeamSummary(KataBeamSheetData d, string excelPath, Action<string> log)
        {
            double totalSpanLen = d.Spans.Sum(s => s.ClearLengthMm);
            string spanList = string.Join(" + ", d.Spans.Select(s => $"{s.ClearLengthMm:F0}"));
            log($"[HPKata C#] 1. Đọc thành công sheet 'Dam' từ '{excelPath}':");
            log($"  - Tên dầm: {d.BeamMark} | Tiết diện: {d.DefaultWidthMm:F0}x{d.DefaultHeightMm:F0} mm | Cao trình: {d.ElevationString}");
            log($"  - Thép chủ trên: {d.TopContinuousRebar} | Thép chủ dưới: {d.BotContinuousRebar} | Cốt giá: {d.SkinBarLayers * 2}Ø{d.SkinBarDiameterMm}");
            log($"  - Thép đai: Ø{d.StirrupDiameterMm} ({d.StirrupNearSupport}/{d.StirrupMidSpan}, {d.LegCount} nhánh)");
            log($"  - Số nhịp: {d.Spans.Count} nhịp ({spanList} mm = {totalSpanLen:F0} mm) | Số gối cột: {d.Supports.Count}");
        }

        private static void PrepareSeedDwg(string workingDwgPath, string outDwgPath, Action<string> log)
        {
            foreach (var existing in Process.GetProcessesByName("acad"))
            {
                try { existing.Kill(entireProcessTree: true); existing.WaitForExit(3000); } catch { /* ignore */ }
            }

            string? dir = Path.GetDirectoryName(workingDwgPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            CleanupLockFiles(workingDwgPath, outDwgPath);

            string isoKata = @"C:\HPKata\Iso_kata.dwg";
            if (File.Exists(isoKata))
            {
                File.Copy(isoKata, workingDwgPath, overwrite: true);
                log($"[HPKata C#] 2. Đã khởi tạo bản vẽ nền '{workingDwgPath}' từ '{isoKata}'.");
            }
        }

        private static void CleanupLockFiles(params string[] dwgPaths)
        {
            foreach (string dwg in dwgPaths)
            {
                string dwl = Path.ChangeExtension(dwg, ".dwl");
                string dwl2 = Path.ChangeExtension(dwg, ".dwl2");
                try { if (File.Exists(dwl)) File.Delete(dwl); } catch { /* ignore */ }
                try { if (File.Exists(dwl2)) File.Delete(dwl2); } catch { /* ignore */ }
            }
        }

        private static Process? EnsureAutocadRunningWithDrawing(string dwgPath, Action<string> log)
        {
            var psi = new ProcessStartInfo
            {
                FileName = AcadExePath,
                Arguments = $"/product ACAD /language \"en-US\" \"{dwgPath}\"",
                UseShellExecute = false
            };

            Process proc = Process.Start(psi) ?? throw new InvalidOperationException("Không thể khởi động acad.exe.");
            log($"[HPKata C#] Đã khởi động AutoCAD 2026 (PID: {proc.Id}) mở bản vẽ '{dwgPath}'.");
            return proc;
        }

        private static bool WaitForPipeAvailable(Action<string> log)
        {
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed.TotalSeconds < 60)
            {
                AnswerSecureLoadIfPresent(log);
                if (File.Exists(PipePath))
                {
                    log($"[HPKata C#] Pipe '{PipePath}' đã kết nối sau {sw.Elapsed.TotalSeconds:F0}s. Đang chờ nạp bản vẽ...");
                    Thread.Sleep(4000);
                    return true;
                }
                Thread.Sleep(1000);
            }
            return false;
        }

        private static bool WaitForDrawingReady(AutocadMcpStdioClient client, Action<string> log)
        {
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed.TotalSeconds < 90)
            {
                AnswerSecureLoadIfPresent(log);
                try
                {
                    string ctxJson = client.CallToolRaw("get_autocad_context", new { });
                    if (ctxJson.Contains("\"openDocs\":[\"") || ctxJson.Contains(".dwg"))
                    {
                        log($"[HPKata C#] 3. get_autocad_context sẵn sàng: {ctxJson}");
                        return true;
                    }
                }
                catch
                {
                    // Ignore transient startup errors
                }
                Thread.Sleep(2000);
            }
            return false;
        }

        private static void RunMcpBeamWorkflow(
            AutocadMcpStdioClient client, KataBeamSheetData beamData, string outDwgPath, Action<string> log)
        {
            string code = AutocadBeamScriptBuilder.BuildAutoCadCSharpScript();

            var dryParams = AutocadBeamScriptBuilder.BuildParameters(beamData, outDwgPath + ".DRYRUN");
            string dryResult = client.CallToolRaw("execute_autocad_code", new
            {
                code,
                args = dryParams,
                transaction = "auto",
                dryRun = true,
                timeoutSeconds = 60,
                label = "HPKata Beam B01 DryRun"
            });
            log($"[HPKata C#] 4. Kết quả execute_autocad_code (dryRun = true):\n{dryResult}");

            if (File.Exists(outDwgPath)) File.Delete(outDwgPath);
            var commitParams = AutocadBeamScriptBuilder.BuildParameters(beamData, outDwgPath);
            string commitResult = client.CallToolRaw("execute_autocad_code", new
            {
                code,
                args = commitParams,
                transaction = "auto",
                dryRun = false,
                timeoutSeconds = 60,
                label = "HPKata Draw Beam B01 From Excel"
            });
            log($"[HPKata C#] 5. Kết quả execute_autocad_code (dryRun = false, transaction = auto):\n{commitResult}");

            string dwgCtx = client.CallToolRaw("get_drawing_context", new { includeLayers = true, includeLayouts = false });
            log($"[HPKata C#] 6. get_drawing_context sau khi vẽ:\n{dwgCtx}");

            string rebarEntities = client.CallToolRaw("query_entities", new
            {
                filter = new { layers = new[] { "KATA_THEP_CHU" } },
                mode = "summary",
                limit = 20
            });
            log($"[HPKata C#] 7. query_entities (Layer KATA_THEP_CHU):\n{rebarEntities}");
        }

        private static void AnswerSecureLoadIfPresent(Action<string> log)
        {
            IntPtr dlg = FindWindow("#32770", "Security - Unsigned Executable File");
            if (dlg == IntPtr.Zero) return;

            IntPtr loadOnceBtn = IntPtr.Zero;
            EnumChildWindows(dlg, (hWnd, _) =>
            {
                var sb = new StringBuilder(256);
                GetWindowText(hWnd, sb, 256);
                if (sb.ToString() == "Load Once")
                {
                    loadOnceBtn = hWnd;
                    return false;
                }
                return true;
            }, IntPtr.Zero);

            if (loadOnceBtn != IntPtr.Zero)
            {
                SendMessage(loadOnceBtn, 0x00F5, IntPtr.Zero, IntPtr.Zero);
                log("[HPKata C#] Tự động xác nhận SECURELOAD ('Load Once') cho plugin AutoCAD.");
            }
        }

        public static string ResolveMcpServerExe()
        {
            string baseDir = AppContext.BaseDirectory;
            string[] candidates =
            {
                Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\..\..\HPAutoCad\HPAutoCad.Mcp.Server\bin\Debug\net10.0\HPAutoCad.Mcp.Server.exe")),
                Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\..\..\HPAutoCad\HPAutoCad.Mcp.Server\bin\Release\net10.0\HPAutoCad.Mcp.Server.exe")),
                @"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPAutoCad\HPAutoCad.Mcp.Server\bin\Debug\net10.0\HPAutoCad.Mcp.Server.exe"
            };
            return candidates.FirstOrDefault(File.Exists) ?? candidates[0];
        }
    }
}
