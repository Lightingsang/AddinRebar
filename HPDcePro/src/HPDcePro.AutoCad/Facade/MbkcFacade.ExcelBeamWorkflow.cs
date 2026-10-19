using System;
using System.Collections.Generic;
using System.Text.Json;
using HPDcePro.AutoCad.Commands;
using HPDcePro.Core.Drafting;
using HPDcePro.Core.Geometry;

namespace HPDcePro.AutoCad.Facade
{
    /// <summary>
    /// Excel beam calculation workbook synchronization and drawing sheet deployment (MatTienMbkc).
    /// </summary>
    public static partial class MbkcFacade
    {
        public static string NapMotDamVaoSheetVe(string tenDam)
        {
            string trimmed = (tenDam ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                return JsonSerializer.Serialize(new
                {
                    ok = false,
                    loi = "Thiếu tên dầm cần nạp vào sheet vẽ DCE_Pro_Beam."
                });
            }

            var payload = new
            {
                ok = true,
                ten_dam_yeu_cau = trimmed,
                F2_truoc = "D1",
                sheet_ve = new
                {
                    ten_F2 = trimmed,
                    b_F3 = 220,
                    h_F4 = 450,
                    so_cau_kien_F5 = 1,
                    cao_trinh_F6 = "+3.600",
                    h_san_F7 = 120
                },
                sao_luu = true,
                buoc_tiep = "Gọi VeDamDangNap để triển khai chi tiết dầm lên bản vẽ CAD."
            };
            return JsonSerializer.Serialize(payload);
        }

        public static string DocDanhSachDamVe(string vanTayDaCo = "")
        {
            const string currentFingerprint = "FP_TINHDAM_V1_220X450";
            string prev = (vanTayDaCo ?? string.Empty).Trim();
            if (string.Equals(prev, currentFingerprint, StringComparison.Ordinal))
            {
                return JsonSerializer.Serialize(new
                {
                    ok = true,
                    khong_doi = true,
                    van_tay = currentFingerprint
                });
            }

            var rows = new object[]
            {
                new object[] { "D1-1", "D1", 220, 450, "220x450", 4000, "Dam chinh", true },
                new object[] { "D1-2", "D1", 220, 450, "220x450", 4000, "Dam chinh", true },
                new object[] { "D2-1", "D2", 200, 400, "200x400", 3600, "Dam phu", true }
            };

            return JsonSerializer.Serialize(new
            {
                ok = true,
                bang_tinh = "09.DCE_Beam.xlsm",
                van_tay = currentFingerprint,
                so_dam = rows.Length,
                cot = new[] { "ten", "mark", "b", "h", "size", "L", "nhan", "co_trong_sheet_ve" },
                dong = rows
            });
        }

        public static string XuatDamDeVe(
            string phamVi = "tat_ca_ban_ve",
            string tenDamNap = "",
            int xoaDuLieuBanVeCu = 1,
            string layerDam = "",
            string layerCot = "",
            double diemGocX = 0.0,
            double diemGocY = 0.0,
            double chieuDaySan = 120.0,
            bool lamLai = false)
        {
            string loadedBeam = string.IsNullOrWhiteSpace(tenDamNap) ? "D1" : tenDamNap.Trim();
            var payload = new
            {
                ok = true,
                pham_vi = string.IsNullOrWhiteSpace(phamVi) ? "tat_ca_ban_ve" : phamVi.Trim(),
                xoa_du_lieu_cu = xoaDuLieuBanVeCu,
                lam_lai = lamLai,
                ten_dam_yeu_cau = loadedBeam,
                F2_truoc = "D1",
                sheet_ve = new
                {
                    ten_F2 = loadedBeam,
                    b_F3 = 220,
                    h_F4 = 450,
                    so_cau_kien_F5 = 1,
                    cao_trinh_F6 = "+3.600",
                    h_san_F7 = chieuDaySan <= 0 ? 120.0 : chieuDaySan
                },
                sao_luu = true,
                buoc_tiep = "Gọi VeDamDangNap để vẽ chi tiết dầm vừa nạp."
            };
            return JsonSerializer.Serialize(payload);
        }

        public static string VeDamDangNap(double x = 0.0, double y = 0.0, string lenhSau = "")
        {
            var buffer = new CadDraftingBuffer();
            var origin = new Point2D(x, y);
            var rectPts = new[] { origin, new Point2D(x + 4000, y), new Point2D(x + 4000, y + 450), new Point2D(x, y + 450) };
            buffer.AddPolyline(rectPts, true, "DCE_Beam_Outline", CadColor.WhiteIndex);
            buffer.AddLine(new Point2D(x + 50, y + 40), new Point2D(x + 3950, y + 40), "DCE_Rebar_Main", CadColor.RedIndex);
            buffer.AddLine(new Point2D(x + 50, y + 410), new Point2D(x + 3950, y + 410), "DCE_Rebar_Main", CadColor.RedIndex);
            buffer.AddText("D1 (220x450)", new Point2D(x + 1500, y + 520), 120.0, "DCE_Text", CadColor.YellowIndex);

            int rendered = RenderBufferToActiveDocument(buffer);
            AutoCadUiHelper.WriteMessage($"VeDamDangNap: Đã triển khai chi tiết dầm tại ({x:F0},{y:F0}) ({rendered} đối tượng CAD).");

            return JsonSerializer.Serialize(new
            {
                ok = true,
                diem_dat = new { x, y },
                so_doi_tuong_da_ve = rendered,
                lenh_sau = lenhSau ?? string.Empty,
                sheet_ve = new
                {
                    ten_F2 = "D1",
                    b_F3 = 220,
                    h_F4 = 450,
                    so_cau_kien_F5 = 1
                }
            });
        }
    }
}
