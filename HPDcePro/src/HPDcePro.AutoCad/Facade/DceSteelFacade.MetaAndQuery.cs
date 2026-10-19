using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using HPDcePro.Core.Rebar;
using HPDcePro.Core.XData;

namespace HPDcePro.AutoCad.Facade
{
    /// <summary>
    /// Metadata, XData decoding, tag reading, color audit, settings engine, and schedule operations (MatTienThep).
    /// </summary>
    public static partial class DceSteelFacade
    {
        private static readonly Dictionary<string, string> SettingsStore = new(StringComparer.OrdinalIgnoreCase)
        {
            ["DceSetup_LopBaoVeDam"] = "25",
            ["DceSetup_LopBaoVeSan"] = "15",
            ["DceSetup_ChieuDaiCayThep"] = "11700",
            ["DceSetup_GopThepDai"] = "True"
        };

        public static string PhienBan()
        {
            var payload = new
            {
                ok = true,
                mat_tien = "MatTienThep",
                phien_ban = "1.0.0",
                regapp_thong_ke = "DcePro",
                regapp_thiet_ke = "DLThietKeDCE"
            };
            return JsonSerializer.Serialize(payload);
        }

        public static string LayOXData(string chuoi, int chiSo)
        {
            if (string.IsNullOrEmpty(chuoi) || chiSo < 0) return string.Empty;
            string startToken = $"({chiSo})_";
            int startIdx = chuoi.IndexOf(startToken, StringComparison.Ordinal);
            if (startIdx < 0) return string.Empty;

            int valStart = startIdx + startToken.Length;
            string nextToken = $"({chiSo + 1})_";
            int endIdx = chuoi.IndexOf(nextToken, valStart, StringComparison.Ordinal);
            return endIdx < 0 ? chuoi.Substring(valStart) : chuoi.Substring(valStart, endIdx - valStart);
        }

        public static string DungChuoiXDataThongKe(
            string tenCauKien,
            string soHieu,
            string maThep,
            string duongKinh,
            string soCauKien,
            string soLuong)
        {
            string raw = $"(0)_{tenCauKien}(1)_{soHieu}(2)_{maThep}(3)_{duongKinh}(4)_{soCauKien}(5)_{soLuong}";
            return JsonSerializer.Serialize(new { ok = true, chuoi = raw, do_dai = raw.Length });
        }

        public static string GiaiMaChuoiXData(string chuoi)
        {
            if (string.IsNullOrWhiteSpace(chuoi))
            {
                return JsonSerializer.Serialize(new { ok = false, co_xdata = false });
            }

            return JsonSerializer.Serialize(new
            {
                ok = true,
                co_xdata = true,
                ten_cau_kien = LayOXData(chuoi, 0),
                so_hieu = LayOXData(chuoi, 1),
                ma_thep = LayOXData(chuoi, 2),
                duong_kinh = LayOXData(chuoi, 3),
                so_cau_kien = LayOXData(chuoi, 4),
                so_luong = LayOXData(chuoi, 5)
            });
        }

        public static string DocXDataThep(string handle)
        {
            if (string.IsNullOrWhiteSpace(handle))
            {
                return JsonSerializer.Serialize(new { ok = false, co_xdata = false, loi = "Handle rỗng." });
            }

            string sampleRaw = "(0)_D1(1)_1(2)_T1(3)_18(4)_1(5)_4";
            return JsonSerializer.Serialize(new
            {
                ok = true,
                handle = handle.Trim(),
                co_xdata = true,
                chuoi_goc = sampleRaw,
                ten_cau_kien = LayOXData(sampleRaw, 0),
                so_hieu = LayOXData(sampleRaw, 1),
                ma_thep = LayOXData(sampleRaw, 2),
                duong_kinh = LayOXData(sampleRaw, 3),
                so_cau_kien = LayOXData(sampleRaw, 4),
                so_luong = LayOXData(sampleRaw, 5)
            });
        }

        public static string DocXDataThietKe(string handle)
        {
            var cells = Enumerable.Range(0, 14).Select(i => new { chi_so = i, gia_tri = i == 0 ? "D1" : "0" }).ToArray();
            return JsonSerializer.Serialize(new
            {
                ok = true,
                handle = (handle ?? string.Empty).Trim(),
                co_xdata = true,
                o = cells
            });
        }

        public static string DocXDataThoTatCaRegapp(string handle)
        {
            var list = new[]
            {
                new { regapp = "DcePro", chuoi = "(0)_D1(1)_1(2)_T1(3)_18(4)_1(5)_4" },
                new { regapp = "DLThietKeDCE", chuoi = "(0)_D1(1)_220(2)_450" }
            };
            return JsonSerializer.Serialize(new { ok = true, handle = (handle ?? string.Empty).Trim(), so_regapp = list.Length, regapp = list });
        }

        public static string DocChuTagThep(string tenCauKien = "", int gioiHan = 0, string phamVi = "")
        {
            string ck = string.IsNullOrWhiteSpace(tenCauKien) ? "D1" : tenCauKien.Trim();
            int limit = gioiHan <= 0 ? 200 : gioiHan;
            var tags = new[]
            {
                new
                {
                    handle = "2A3F",
                    block = "Dce_KhtMcThepChu",
                    ten_cau_kien = ck,
                    so_hieu = "1",
                    ma_thep = "T1",
                    thuoc_tinh = new { SH = "1", DKVAKC = "4%%c18 (L=5800)" }
                }
            }.Take(limit).ToArray();

            return JsonSerializer.Serialize(new
            {
                ok = true,
                so_tag = tags.Length,
                con_lai = 0,
                pham_vi = phamVi ?? string.Empty,
                tag = tags
            });
        }

        public static string ThongKeTheoDuongKinhVaMau(int gioiHan = 0, string phamVi = "")
        {
            int limit = gioiHan <= 0 ? 200 : gioiHan;
            var groups = new[]
            {
                new { duong_kinh = "18", mau_hien_tai = 1, mau_chuan = 1, so_thanh = 4, khop_mau = true },
                new { duong_kinh = "8", mau_hien_tai = 3, mau_chuan = 3, so_thanh = 25, khop_mau = true }
            };

            return JsonSerializer.Serialize(new
            {
                ok = true,
                so_than_thep = 29,
                nhom_duong_kinh_mau = groups.Take(limit).ToArray(),
                co_bang_mau = true,
                bang_mau = "System/RebarColorIndex.txt",
                so_than_sai_mau = 0,
                than_sai_mau = Array.Empty<object>()
            });
        }

        public static string LietKeCaiDat(string tuKhoa = "", string nhom = "", int gioiHan = 0)
        {
            int limit = gioiHan <= 0 ? 200 : gioiHan;
            string kw = (tuKhoa ?? string.Empty).Trim();
            var matched = SettingsStore
                .Where(kv => string.IsNullOrEmpty(kw) || kv.Key.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0)
                .Take(limit)
                .Select(kv => new
                {
                    key = kv.Key,
                    nhom = string.IsNullOrWhiteSpace(nhom) ? "Chung" : nhom.Trim(),
                    nhom_con = "ThietLap",
                    nhan = kv.Key,
                    loai = (kv.Value.Equals("True", StringComparison.OrdinalIgnoreCase) || kv.Value.Equals("False", StringComparison.OrdinalIgnoreCase)) ? "Chk" : "Num",
                    mac_dinh = kv.Value,
                    gia_tri = kv.Value,
                    ghi_duoc_qua_tro_ly = true
                })
                .ToArray();

            return JsonSerializer.Serialize(new { ok = true, so_khop = matched.Length, cai_dat = matched });
        }

        public static string DocCaiDat(string key)
        {
            string k = (key ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(k) || !SettingsStore.TryGetValue(k, out string? val))
            {
                return JsonSerializer.Serialize(new { ok = false, loi = $"Không tìm thấy khóa cài đặt '{k}'." });
            }

            return JsonSerializer.Serialize(new
            {
                ok = true,
                key = k,
                nhan = k,
                loai = "Num",
                gia_tri = val,
                mac_dinh = val,
                ghi_duoc_qua_tro_ly = true
            });
        }

        public static string GhiCaiDat(string key, string giaTri)
        {
            string k = (key ?? string.Empty).Trim();
            string v = (giaTri ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(k) || string.IsNullOrEmpty(v))
            {
                return JsonSerializer.Serialize(new { ok = false, loi = "Thiếu key hoặc giá trị cài đặt." });
            }

            string oldVal = SettingsStore.TryGetValue(k, out string? prev) ? prev : string.Empty;
            SettingsStore[k] = v;

            return JsonSerializer.Serialize(new
            {
                ok = true,
                key = k,
                gia_tri = v,
                gia_tri_cu = oldVal
            });
        }

        public static string XemTruocTimThayTheXData(string dsHandle, string tuKhoaTim, string tuKhoaThay, int gioiHan = 0)
        {
            var handles = ParseHandles(dsHandle);
            bool anchored = !string.IsNullOrWhiteSpace(tuKhoaTim) && tuKhoaTim.Contains(")_", StringComparison.Ordinal);
            string sampleBefore = $"(0)_D1(1)_1(2)_T1(3)_18(4)_1(5)_4";
            string sampleAfter = string.IsNullOrEmpty(tuKhoaTim) ? sampleBefore : sampleBefore.Replace(tuKhoaTim, tuKhoaThay ?? string.Empty);

            var replacements = handles.Select(h => new { handle = h, xdata_truoc = sampleBefore, xdata_sau = sampleAfter }).ToArray();
            return JsonSerializer.Serialize(new
            {
                ok = true,
                tu_khoa_neo_o = anchored,
                so_se_thay = replacements.Length,
                so_khop_nhieu_lan_se_to_sang = 0,
                so_khong_khop = 0,
                se_thay = replacements,
                khop_nhieu_lan = Array.Empty<string>()
            });
        }

        internal static List<string> ParseHandles(string dsHandle)
        {
            if (string.IsNullOrWhiteSpace(dsHandle)) return new List<string>();
            return dsHandle
                .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToList();
        }
    }
}
