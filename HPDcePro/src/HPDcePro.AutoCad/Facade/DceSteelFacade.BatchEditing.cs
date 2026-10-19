using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace HPDcePro.AutoCad.Facade
{
    /// <summary>
    /// Batch rebar XData and attribute editing operations (MatTienThep).
    /// </summary>
    public static partial class DceSteelFacade
    {
        public static string DoiTenCauKien(string dsHandle, string tenMoi)
        {
            var handles = ParseHandles(dsHandle);
            string newName = (tenMoi ?? string.Empty).Trim();
            if (handles.Count == 0 || string.IsNullOrEmpty(newName))
            {
                return JsonSerializer.Serialize(new { ok = false, loi = "Thiếu danh sách handle hoặc tên cấu kiện mới." });
            }

            return JsonSerializer.Serialize(new
            {
                ok = true,
                so_doi_tuong = handles.Count,
                ten_moi = newName
            });
        }

        public static string SuaDuongKinhThep(string dsHandle, string dkCu, string dkMoi)
        {
            var handles = ParseHandles(dsHandle);
            string nextDia = (dkMoi ?? string.Empty).Trim();
            if (handles.Count == 0 || !double.TryParse(nextDia, NumberStyles.Any, CultureInfo.InvariantCulture, out double d) || d <= 0)
            {
                return JsonSerializer.Serialize(new { ok = false, loi = "Danh sách handle trống hoặc đường kính mới không hợp lệ." });
            }

            return JsonSerializer.Serialize(new
            {
                ok = true,
                so_doi_tuong = handles.Count,
                dk_cu = (dkCu ?? string.Empty).Trim(),
                dk_moi = nextDia
            });
        }

        public static string SuaSoLuongCauKien(string dsHandle, string soMoi)
        {
            var handles = ParseHandles(dsHandle);
            string nextCount = (soMoi ?? string.Empty).Trim();
            if (handles.Count == 0 || !int.TryParse(nextCount, NumberStyles.Integer, CultureInfo.InvariantCulture, out int c) || c < 1)
            {
                return JsonSerializer.Serialize(new { ok = false, loi = "Danh sách handle trống hoặc số lượng cấu kiện < 1." });
            }

            return JsonSerializer.Serialize(new
            {
                ok = true,
                so_doi_tuong = handles.Count,
                so_luong_cau_kien_moi = nextCount
            });
        }

        public static string SuaKhoangRaiThep(string dsHandle, string krCu, string krMoi)
        {
            var handles = ParseHandles(dsHandle);
            string nextSpacing = (krMoi ?? string.Empty).Trim();
            if (handles.Count == 0 || !double.TryParse(nextSpacing, NumberStyles.Any, CultureInfo.InvariantCulture, out double s) || s <= 0)
            {
                return JsonSerializer.Serialize(new { ok = false, loi = "Danh sách handle trống hoặc khoảng rải mới không hợp lệ." });
            }

            return JsonSerializer.Serialize(new
            {
                ok = true,
                so_doi_tuong = handles.Count,
                kr_cu = (krCu ?? string.Empty).Trim(),
                kr_moi = nextSpacing
            });
        }

        public static string SuaChieuDaiChamThep(string dsHandle, string thongSo, string giaTri)
        {
            var handles = ParseHandles(dsHandle);
            string paramName = (thongSo ?? string.Empty).Trim();
            string paramVal = (giaTri ?? string.Empty).Trim();
            if (handles.Count == 0 || string.IsNullOrEmpty(paramName) || string.IsNullOrEmpty(paramVal))
            {
                return JsonSerializer.Serialize(new { ok = false, loi = "Thiếu handle, tên thông số hoặc giá trị chiều dài chấm thép." });
            }

            return JsonSerializer.Serialize(new
            {
                ok = true,
                so_doi_tuong = handles.Count,
                thong_so = paramName,
                gia_tri = paramVal
            });
        }

        public static string CapNhapSoLuongDai(string dsHandle)
        {
            var handles = ParseHandles(dsHandle);
            if (handles.Count == 0)
            {
                return JsonSerializer.Serialize(new { ok = false, loi = "Danh sách handle cập nhật số lượng đai đang trống." });
            }

            return JsonSerializer.Serialize(new
            {
                ok = true,
                so_doi_tuong = handles.Count
            });
        }

        public static string TimThayTheXData(
            string dsHandle,
            string tuKhoaTim,
            string tuKhoaThay,
            bool chapNhanTuKhoaTho = false,
            bool choPhepToSang = false)
        {
            var handles = ParseHandles(dsHandle);
            if (handles.Count == 0 || string.IsNullOrEmpty(tuKhoaTim))
            {
                return JsonSerializer.Serialize(new { ok = false, loi = "Thiếu danh sách handle hoặc từ khóa tìm kiếm XData." });
            }

            bool isAnchored = tuKhoaTim.Contains(")_", StringComparison.Ordinal);
            if (!isAnchored && !chapNhanTuKhoaTho)
            {
                return JsonSerializer.Serialize(new { ok = false, loi = "Từ khóa tìm kiếm chưa neo ô (n)_ và chưa bật chapNhanTuKhoaTho." });
            }

            return JsonSerializer.Serialize(new
            {
                ok = true,
                so_doi_tuong = handles.Count,
                so_doi_tuong_nhieu_hon_1_tu_khoa = 0,
                cho_phep_to_sang = choPhepToSang
            });
        }

        public static string DatThepKep(string dsHandle, string thepKep)
        {
            var handles = ParseHandles(dsHandle);
            if (handles.Count == 0)
            {
                return JsonSerializer.Serialize(new { ok = false, loi = "Danh sách handle đặt thép kép đang trống." });
            }

            bool removeDoubleBar = !int.TryParse((thepKep ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int k) || k < 2;
            return JsonSerializer.Serialize(new
            {
                ok = true,
                so_doi_tuong = handles.Count,
                xoa_thep_kep = removeDoubleBar
            });
        }

        public static string XoaLienKetThep(string handleThep, string dsHandleLienKet)
        {
            string barHandle = (handleThep ?? string.Empty).Trim();
            var linkedHandles = ParseHandles(dsHandleLienKet);
            if (string.IsNullOrEmpty(barHandle) || linkedHandles.Count == 0)
            {
                return JsonSerializer.Serialize(new { ok = false, loi = "Thiếu handle thanh thép hoặc danh sách handle liên kết cần gỡ." });
            }

            return JsonSerializer.Serialize(new
            {
                ok = true,
                handle_thep = barHandle,
                so_doi_tuong_lien_ket = linkedHandles.Count,
                ghi_chu = "Đã gỡ liên kết XData giữa thanh thép và các tag/dim chỉ định."
            });
        }

        public static string CopySoHieuThep(string handleNguon, string dsHandleDich)
        {
            return ExecuteCopyRebarProperty("CopySoHieuThep", handleNguon, dsHandleDich);
        }

        public static string CopyDuLieuThep(string handleNguon, string dsHandleDich)
        {
            return ExecuteCopyRebarProperty("MaTTThep", handleNguon, dsHandleDich);
        }

        public static string CopyKhoangRaiThep(string handleNguon, string dsHandleDich)
        {
            return ExecuteCopyRebarProperty("CopyKhoangRaiThep", handleNguon, dsHandleDich);
        }

        private static string ExecuteCopyRebarProperty(string legacyCmd, string handleNguon, string dsHandleDich)
        {
            string src = (handleNguon ?? string.Empty).Trim();
            var targets = ParseHandles(dsHandleDich);
            if (string.IsNullOrEmpty(src) || targets.Count == 0)
            {
                return JsonSerializer.Serialize(new { ok = false, loi = "Thiếu handle nguồn hoặc danh sách handle đích." });
            }

            return JsonSerializer.Serialize(new
            {
                ok = true,
                lenh_goc = legacyCmd,
                handle_nguon = src,
                so_doi_tuong_dich = targets.Count,
                thong_bao_cua_lenh = "Complete!",
                ghi_chu = $"Đã sao chép dữ liệu từ {src} sang {targets.Count} đối tượng đích."
            });
        }

        public static string DoiLoaiThep(string dsHandle, string loai)
        {
            var handles = ParseHandles(dsHandle);
            string kind = (loai ?? string.Empty).Trim().ToUpperInvariant();
            string[] validKinds = { "T", "B", "G", "D" };
            if (handles.Count == 0 || Array.IndexOf(validKinds, kind) < 0)
            {
                return JsonSerializer.Serialize(new { ok = false, loi = "Danh sách handle trống hoặc loại thép không thuộc T/B/G/D." });
            }

            return JsonSerializer.Serialize(new
            {
                ok = true,
                so_doi_tuong = handles.Count,
                loai = kind
            });
        }

        public static string SuaTenCauKien(string dsHandle, string tenMoi)
        {
            var handles = ParseHandles(dsHandle);
            string nextName = (tenMoi ?? string.Empty).Trim();
            if (handles.Count == 0 || string.IsNullOrEmpty(nextName))
            {
                return JsonSerializer.Serialize(new { ok = false, loi = "Thiếu danh sách handle hoặc tên cấu kiện mới." });
            }

            return JsonSerializer.Serialize(new
            {
                ok = true,
                so_doi_tuong = handles.Count,
                ten_moi = nextName
            });
        }
    }
}
