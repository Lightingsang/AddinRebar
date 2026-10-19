namespace HPBim.Cli.Commands;

using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Core.Qc;

/// <summary>
/// Generates and persists the official audit artifacts (JSON and Markdown dossier).
/// Adheres to Clean Code C1/C5: Presentation and artifact persistence isolated from orchestrator.
/// </summary>
public static class FoundationAuditDossierWriter
{
    private static readonly JsonSerializerOptions IndentedJson = new() { WriteIndented = true };

    public static async Task SaveAuditArtifactsAsync(
        FoundationAuditReport report,
        IReadOnlyList<GridAuditItem> grids,
        IReadOnlyList<LevelAuditItem> levels,
        IReadOnlyList<PileAuditItem> piles,
        IReadOnlyList<FootingAuditItem> footings,
        IReadOnlyList<BeamAuditItem> beams,
        CancellationToken ct)
    {
        Directory.CreateDirectory("state");
        string jsonPath = Path.Combine("state", "ket_qua_kiem_toan_phan_mong.json");
        await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(report, IndentedJson), ct).ConfigureAwait(false);

        string mdPath = Path.Combine("state", "Bien_Ban_Nghiem_Thu_Phan_Mong.md");
        string md = GenerateMarkdownDossier(report, grids, levels, piles, footings, beams);
        await File.WriteAllTextAsync(mdPath, md, ct).ConfigureAwait(false);
    }

    public static string GenerateMarkdownDossier(
        FoundationAuditReport report,
        IReadOnlyList<GridAuditItem> grids,
        IReadOnlyList<LevelAuditItem> levels,
        IReadOnlyList<PileAuditItem> piles,
        IReadOnlyList<FootingAuditItem> footings,
        IReadOnlyList<BeamAuditItem> beams)
    {
        return $"""
        # BIÊN BẢN NGHIỆM THU ĐỘC LẬP PHÂN ĐOẠN PHẦN MÓNG
        **Dự án:** Hội trường Tân Đông Hiệp (KC-HT-90-TLTT-16.dwg)  
        **Mô hình Revit:** TemplateAIModel.rvt  
        **Thời gian kiểm toán:** {report.AuditedAtUtc:yyyy-MM-dd HH:mm:ss} UTC  
        **Đơn vị kiểm toán:** Kỹ sư QA/QC Độc lập (100% C# .NET Engine)  
        **Nguyên tắc áp dụng:** ĐIỀU 0 — Tuyệt đối không phỏng đoán, kiểm toán hình học chân thực.

        ---

        ## 1. Kết Quả Kiểm Toán 8 Bước

        | STT | Hạng mục kiểm toán | Tiêu chuẩn dung sai | Kết quả thực tế | Đánh giá |
        | :--- | :--- | :--- | :--- | :--- |
        | Bước 1 | Hệ trục & Cao trình | 27 trục, 10 cao trình, ghim 100%, sai số <= 1.0mm | {report.Steps[0].Summary} | {(report.Steps[0].Passed ? "ĐÚNG" : "SAI, CẦN SỬA")} |
        | Bước 2 | Số lượng cấu kiện & Dầm ma | 196 cọc, 72 đài móng, 188-192 giằng, 0 dầm ma | {report.Steps[1].Summary} | {(report.Steps[1].Passed ? "ĐÚNG" : "SAI, CẦN SỬA")} |
        | Bước 3 | Vị trí & Tọa độ mặt bằng | Sai số tim cọc, trọng tâm đài <= 2.0mm | {report.Steps[2].Summary} | {(report.Steps[2].Passed ? "ĐÚNG" : "SAI, CẦN SỬA")} |
        | Bước 4 | Kích thước tiết diện | Sai số kích thước b x h = 0.0mm | {report.Steps[3].Summary} | {(report.Steps[3].Passed ? "ĐÚNG" : "SAI, CẦN SỬA")} |
        | Bước 5 | Cao độ & Cao trình ngàm | Đỉnh đài/dầm -1600mm, đầu cọc ngàm 100mm | {report.Steps[4].Summary} | {(report.Steps[4].Passed ? "ĐÚNG" : "SAI, CẦN SỬA")} |
        | Bước 6 | Quan hệ kết cấu (Bao hàm & Tựa) | 100% cọc trong đài (khoảng hở >= 150mm), dầm liên tục | {report.Steps[5].Summary} | {(report.Steps[5].Passed ? "ĐÚNG" : "SAI, CẦN SỬA")} |
        | Bước 7 | Đối soát chi tiết CAD | Khớp chi tiết bản vẽ móng ĐC-1 đến ĐC-6 | {report.Steps[6].Summary} | {(report.Steps[6].Passed ? "ĐÚNG" : "SAI, CẦN SỬA")} |
        | Bước 8 | Cập nhật sổ cái điều phối | Đồng bộ 100% vào bim_coordination_ledger.json | {report.Steps[7].Summary} | {(report.Steps[7].Passed ? "ĐÚNG" : "SAI, CẦN SỬA")} |

        ---

        ## 2. Kết Luận & Quyết Định Nghiệm Thu
        - **Trạng thái:** {(report.AllPassed ? "**CHÍNH THỨC NGHIỆM THU ĐẠT (PASS)**" : "**YÊU CẦU KHẮC PHỤC (REQUEST CHANGES)**")}
        - **Đóng băng phân đoạn (Freeze Zone):** {(report.AllPassed ? "Phân đoạn Phần Móng ĐƯỢC PHÉP ĐÓNG BĂNG để chuyển sang Tầng Thân." : "Tạm dừng, chưa đóng băng phân đoạn.")}
        """;
    }
}
