using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace HPDcePro.Core.AutoStr.Steps
{
    public sealed class AutoStrStep01_TaoSheetSet : AutoStrStepBase
    {
        public override int StepNumber => 1;
        public override string StepName => "Khởi tạo thư mục dự án và SheetSet";

        private static readonly string[] s_subFolders = new[]
        {
            "01.SheetSet",
            "02.Xref",
            "03.DuongPhanTichLuc",
            "04.Etabs",
            "05.ExcelTinhToan",
            "06.MatBangMong",
            "07.MatBangKetCau",
            "08.ChiTietCotVach",
            "09.ChiTietDam",
            "10.ChiTietSan",
            "11.CauKienPhu",
            "12.InAn"
        };

        protected override Task<AutoStrStepResult> ExecuteStepCoreAsync(
            AutoStrStepContext context, CancellationToken cancellationToken)
        {
            var createdPaths = new List<string>();
            string root = context.ProjectRootPath;

            if (!string.IsNullOrWhiteSpace(root))
            {
                if (!Directory.Exists(root))
                {
                    Directory.CreateDirectory(root);
                }

                foreach (var folder in s_subFolders)
                {
                    string path = Path.Combine(root, folder);
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    createdPaths.Add(path);
                }
            }

            context.SetArtifact("SheetSetInitialized", true);
            context.SetArtifact("ProjectSubFolders", createdPaths);

            return Task.FromResult(AutoStrStepResult.Success(
                StepNumber,
                StepName,
                $"Đã khởi tạo hệ thống {s_subFolders.Length} thư mục cấu trúc dự án.",
                outputFiles: createdPaths));
        }
    }
}
