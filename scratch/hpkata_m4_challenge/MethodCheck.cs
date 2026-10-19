using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MethodCheck
{
    public static class Program
    {
        public static void Main()
        {
            string repoRoot = Directory.GetCurrentDirectory();
            string uiDir = Path.Combine(repoRoot, "HPKata", "HPKata.UI");
            string testDir = Path.Combine(repoRoot, "HPKata", "HPKata.UI.Tests");

            var csFiles = Directory.GetFiles(uiDir, "*.cs", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(testDir, "*.cs", SearchOption.AllDirectories))
                .Where(f => !f.Contains("\\obj\\") && !f.Contains("\\bin\\"))
                .ToList();

            Console.WriteLine($"Checking {csFiles.Count} files for Rule M3 (methods <= 50 lines)...");

            int violations = 0;
            foreach (var file in csFiles)
            {
                string[] lines = File.ReadAllLines(file);
                // Rough method block detection
                int braceDepth = 0;
                int methodStartLine = -1;
                string currentMethodName = "";

                for (int i = 0; i < lines.Length; i++)
                {
                    string trimmed = lines[i].Trim();
                    if (braceDepth == 1 && (trimmed.Contains("public ") || trimmed.Contains("private ") || trimmed.Contains("protected ") || trimmed.Contains("internal "))
                        && (trimmed.Contains("(") && !trimmed.Contains("class ") && !trimmed.Contains("interface ") && !trimmed.Contains("delegate ")))
                    {
                        methodStartLine = i + 1;
                        currentMethodName = trimmed;
                    }

                    int openBraces = lines[i].Count(c => c == '{');
                    int closeBraces = lines[i].Count(c => c == '}');
                    braceDepth += (openBraces - closeBraces);

                    if (methodStartLine > 0 && braceDepth <= 1 && closeBraces > 0)
                    {
                        int methodLength = (i + 1) - methodStartLine + 1;
                        if (methodLength > 50)
                        {
                            Console.WriteLine($"[VIOLATION M3] {Path.GetFileName(file)}: Method '{currentMethodName}' length = {methodLength} lines (> 50)");
                            violations++;
                        }
                        methodStartLine = -1;
                    }
                }
            }

            Console.WriteLine($"Method length scan complete: {violations} violations found.");
        }
    }
}
