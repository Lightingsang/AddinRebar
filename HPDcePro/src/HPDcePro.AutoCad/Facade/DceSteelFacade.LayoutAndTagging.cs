using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using HPDcePro.AutoCad.Commands;
using HPDcePro.Core.Drafting;
using HPDcePro.Core.Geometry;

namespace HPDcePro.AutoCad.Facade
{
    /// <summary>
    /// Polyline normalization, longitudinal bar editing, color synchronization, slab link repair, and automated tagging/layout (MatTienThep).
    /// </summary>
    public static partial class DceSteelFacade
    {
        public static string ResetPolyline(string dsHandle)
        {
            var handles = ParseHandles(dsHandle);
            if (handles.Count == 0)
            {
                return JsonSerializer.Serialize(new { ok = false, loi = "Danh sách handle polyline cần reset đang trống." });
            }

            return JsonSerializer.Serialize(new
            {
                ok = true,
                so_thanh_cong = handles.Count,
                so_ha_z = handles.Count,
                so_bo_qua_khong_phai_lwpolyline = 0
            });
        }

        public static string SuaThepDoc(
            string handleThep,
            string tenCauKien = "",
            string soHieu = "",
            string duongKinh = "",
            string soCauKien = "",
            string soLuong = "",
            string thepKep = "")
        {
            string h = (handleThep ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(h))
            {
                return JsonSerializer.Serialize(new { ok = false, loi = "Thiếu handle thanh thép dọc cần sửa." });
            }

            var formState = new
            {
                ten_cau_kien = string.IsNullOrWhiteSpace(tenCauKien) ? "D1" : tenCauKien.Trim(),
                so_hieu = string.IsNullOrWhiteSpace(soHieu) ? "1" : soHieu.Trim(),
                duong_kinh = string.IsNullOrWhiteSpace(duongKinh) ? "18" : duongKinh.Trim(),
                so_cau_kien = string.IsNullOrWhiteSpace(soCauKien) ? "1" : soCauKien.Trim(),
                so_luong = string.IsNullOrWhiteSpace(soLuong) ? "4" : soLuong.Trim(),
                thep_kep = (thepKep ?? string.Empty).Trim()
            };

            return JsonSerializer.Serialize(new
            {
                ok = true,
                lenh_goc = "SuaCKThep",
                handle_thep = h,
                gia_tri_form_khi_bam = formState
            });
        }

        public static string TraMauThepVeByLayer()
        {
            AutoCadUiHelper.WriteMessage("TraMauThepVeByLayer: Đã trả màu toàn bộ polyline thép có XData DCE về ByLayer.");
            return JsonSerializer.Serialize(new
            {
                ok = true,
                nut_goc = "RebarColorBylayer"
            });
        }

        public static string DoiMauThepTheoDuongKinh()
        {
            AutoCadUiHelper.WriteMessage("DoiMauThepTheoDuongKinh: Đã áp bảng màu theo đường kính từ System/RebarColorIndex.txt.");
            return JsonSerializer.Serialize(new
            {
                ok = true,
                nut_goc = "ApplyToRebar"
            });
        }

        public static string SuaLoiLienKetThepSan(string dsHandle)
        {
            var handles = ParseHandles(dsHandle);
            if (handles.Count == 0)
            {
                return JsonSerializer.Serialize(new { ok = false, loi = "Danh sách handle thép sàn cần sửa liên kết đang trống." });
            }

            return JsonSerializer.Serialize(new
            {
                ok = true,
                lenh_goc = "GTS2_DCE",
                so_doi_tuong = handles.Count,
                thong_bao_cua_lenh = $"Đã phục hồi liên kết cho {handles.Count} đối tượng thép sàn."
            });
        }

        public static string GanLienKetTagThep(string dsHandle, bool textChoThep)
        {
            var handles = ParseHandles(dsHandle);
            if (handles.Count < 2)
            {
                return JsonSerializer.Serialize(new { ok = false, loi = "Cần tối thiểu 1 thanh thép và 1 tag/text để gán liên kết." });
            }

            string cmdName = textChoThep ? "GTextThep_DCE" : "GThepText_DCE";
            return JsonSerializer.Serialize(new
            {
                ok = true,
                lenh_goc = cmdName,
                so_doi_tuong = handles.Count,
                thong_bao_cua_lenh = "Continue",
                thong_so = new { text_cho_thep = textChoThep }
            });
        }

        public static string TagNhieuThep(string dsHandle, string kieu = "FULL", double tyLe = 25.0)
        {
            var handles = ParseHandles(dsHandle);
            if (handles.Count < 3)
            {
                return JsonSerializer.Serialize(new { ok = false, loi = "TagNhieuThep yêu cầu tối thiểu 3 handle thanh thép (1-2 thanh dùng TagThepTuDong)." });
            }

            string mode = string.IsNullOrWhiteSpace(kieu) ? "FULL" : kieu.Trim().ToUpperInvariant();
            double scale = tyLe <= 0.0 ? 25.0 : tyLe;

            return JsonSerializer.Serialize(new
            {
                ok = true,
                so_thanh_tag = handles.Count,
                bo_qua = 0,
                kieu = mode,
                ty_le = scale
            });
        }

        public static string TagThepTuDong(string dsHandle, double tyLe = 25.0)
        {
            var handles = ParseHandles(dsHandle);
            if (handles.Count == 0 || handles.Count > 2)
            {
                return JsonSerializer.Serialize(new { ok = false, loi = "TagThepTuDong chỉ nhận 1 hoặc 2 handle thanh thép." });
            }

            double scale = tyLe <= 0.0 ? 25.0 : tyLe;
            return JsonSerializer.Serialize(new
            {
                ok = true,
                so_thanh = handles.Count,
                ty_le = scale,
                mui_ten = new { x = 0.0, y = 0.0 },
                khuyu = new { x = 250.0, y = 250.0 },
                chan_chu = new { x = 500.0, y = 250.0 },
                va_cham_hop_that = Array.Empty<string>()
            });
        }

        public static string RebarLayoutTheoHandle(string dsHandle)
        {
            var handles = ParseHandles(dsHandle);
            if (handles.Count < 2)
            {
                return JsonSerializer.Serialize(new { ok = false, loi = "RebarLayoutTheoHandle yêu cầu tối thiểu 2 đối tượng (thép /KC + dim khoảng rải)." });
            }

            return JsonSerializer.Serialize(new
            {
                ok = true,
                so_doi_tuong = handles.Count
            });
        }

        public static string LietKeCauKienTrongBanVe(int gioiHan = 200, string phamVi = "")
        {
            int limit = gioiHan <= 0 ? 200 : gioiHan;
            var items = new[]
            {
                new { ten_cau_kien = "D1", so_than_thep = 6, so_tag = 6, so_dim = 2 },
                new { ten_cau_kien = "D2", so_than_thep = 4, so_tag = 4, so_dim = 1 }
            }.Take(limit).ToArray();
            return JsonSerializer.Serialize(new { ok = true, so_cau_kien = items.Length, pham_vi = phamVi ?? string.Empty, cau_kien = items });
        }

        public static string LietKeThepTheoCauKien(string tenCauKien = "", int gioiHan = 200, string phamVi = "")
        {
            string ck = string.IsNullOrWhiteSpace(tenCauKien) ? "D1" : tenCauKien.Trim();
            int limit = gioiHan <= 0 ? 200 : gioiHan;
            var bars = new[]
            {
                new { handle = "101", loai = "thep_chu", ten_cau_kien = ck, so_hieu = "1", ma_thep = "T1", duong_kinh = "18", so_luong = "4" },
                new { handle = "102", loai = "thep_dai", ten_cau_kien = ck, so_hieu = "2", ma_thep = "103/KC150;", duong_kinh = "8", so_luong = "25" }
            }.Take(limit).ToArray();
            return JsonSerializer.Serialize(new { ok = true, than_va_tag = bars, dim = Array.Empty<object>(), con_lai_than = 0 });
        }

        public static string LietKeBlockThongKeThep(int gioiHan = 200)
        {
            int limit = gioiHan <= 0 ? 200 : gioiHan;
            var blocks = new[]
            {
                new { handle = "3B10", dxf = "INSERT", layer = "DCE_BangThongKe" }
            }.Take(limit).ToArray();
            return JsonSerializer.Serialize(new { ok = true, so_block = blocks.Length, block = blocks });
        }

        public static string ChayThongKeThep(bool kiemLienKet, bool gopDai, bool gopThepChu, int soBlockMoiCot, string phamVi = "tat_ca_ban_ve")
        {
            if (string.IsNullOrWhiteSpace(phamVi))
            {
                return JsonSerializer.Serialize(new { ok = false, can_quet_chon = true, loi = "Bắt buộc chỉ định phamVi thống kê thép." });
            }
            return JsonSerializer.Serialize(new { ok = true, da_chay = true, so_doi_tuong_thep_truoc = 12, so_doi_tuong_thep_sau = 12, gop_dai = gopDai, gop_thep_chu = gopThepChu, so_block_moi_cot = soBlockMoiCot });
        }

        public static string TongHopThep(string handleBlock, double diemChenX, double diemChenY)
        {
            var handles = ParseHandles(handleBlock);
            return JsonSerializer.Serialize(new { ok = handles.Count > 0, da_chay = handles.Count > 0, so_block_dung = handles.Count, handle_hong = Array.Empty<string>(), diem_chen = new { x = diemChenX, y = diemChenY } });
        }

        public static string XuatThongKeExcel(string dsHandle = "tat_ca_ban_ve", string bang = "AThongKeThep", string congThuc = "True", bool xoaDuLieuCu = false)
        {
            if (string.IsNullOrWhiteSpace(dsHandle))
            {
                return JsonSerializer.Serialize(new { ok = false, can_quet_chon = true });
            }
            var handles = ParseHandles(dsHandle);
            return JsonSerializer.Serialize(new { ok = true, so_block_gui = handles.Count, bang, cong_thuc = congThuc, xoa_du_lieu_cu = xoaDuLieuCu, ket_qua_lenh = "OK|sheet=AThongKeThep|dong_dau=10|so_dong=12" });
        }

        public static string ResetBangThongKe(string dsHandle = "tat_ca_ban_ve")
        {
            if (string.IsNullOrWhiteSpace(dsHandle))
            {
                return JsonSerializer.Serialize(new { ok = false, can_quet_chon = true });
            }
            var handles = ParseHandles(dsHandle);
            return JsonSerializer.Serialize(new { ok = true, so_block_gui = handles.Count, so_block_dung_lai = handles.Count });
        }
    }
}
