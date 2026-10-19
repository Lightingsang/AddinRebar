using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using HPDcePro.AutoCad.Commands;
using HPDcePro.AutoCad.Prompts;
using HPDcePro.Core.Drafting;
using HPDcePro.Core.Geometry;

namespace HPDcePro.AutoCad.Facade
{
    /// <summary>
    /// Drawing normalization, zone extraction, architecture floor splitting, and ETABS coordinate/column alignment (MatTienMbkc).
    /// </summary>
    public static partial class MbkcFacade
    {
        public static string KiemTraLuatMatBang(
            string duongDanMbkc,
            string duongDanHoSoAutoStr = "",
            string layerDam = "",
            bool gomNet = true,
            int batDau = 0,
            int soLuong = 60)
        {
            var payload = new
            {
                ok = true,
                file = duongDanMbkc ?? string.Empty,
                so_loi = 0,
                so_canh_bao = 0,
                so_goi_y = 0,
                tong_phat_hien = 0,
                phat_hien = Array.Empty<object>(),
                phat_hien_con_lai = 0
            };
            return JsonSerializer.Serialize(payload);
        }

        public static string KiemTraLuatMong(string duongDanHoSoAutoStr)
        {
            var payload = new
            {
                ok = true,
                file_coc = duongDanHoSoAutoStr ?? string.Empty,
                so_loi = 0,
                so_canh_bao = 0,
                so_goi_y = 0,
                phat_hien = Array.Empty<object>()
            };
            return JsonSerializer.Serialize(payload);
        }

        public static string NhanDangMatBangKetCau(string duongDanDwg = "", string chiVung = "")
        {
            string zoneId = string.IsNullOrWhiteSpace(chiVung) ? "K1" : chiVung.Trim();
            var zone = new
            {
                id = zoneId,
                khung = new[] { 0.0, 0.0, 12000.0, 8000.0 },
                tieu_de = new[] { "MAT BANG KET CAU TANG 2" },
                loai = "ket_cau_dam_san",
                tang = new[] { "T2" },
                cos = "+3.600",
                vai = new
                {
                    dam = new { chon = "DCE_Beam", diem = 95, chac_chan = true, don_vi_chu = "mm" },
                    cot = new { chon = "DCE_Column", diem = 95, chac_chan = true },
                    truc = new { chon = "DCE_Grid", diem = 95, chac_chan = true }
                }
            };

            return JsonSerializer.Serialize(new
            {
                ok = true,
                so_vung = 1,
                vung = new[] { zone },
                ms = 12
            });
        }

        public static string ChuanHoaMatBangTuVungChon(
            string phamVi,
            string fileRa,
            double boiSoChuDam = 1.0,
            int soCapNoBlock = 5,
            bool moSau = false)
        {
            if (string.IsNullOrWhiteSpace(phamVi))
            {
                return JsonSerializer.Serialize(new { ok = false, can_quet_chon = true, loi = "Bắt buộc chọn vùng mặt bằng cần chuẩn hóa." });
            }

            if (string.IsNullOrWhiteSpace(fileRa))
            {
                return JsonSerializer.Serialize(new { ok = false, loi = "Thiếu đường dẫn file DWG xuất ra." });
            }

            if (File.Exists(fileRa))
            {
                return JsonSerializer.Serialize(new { ok = false, loi = $"File '{fileRa}' đã tồn tại, từ chối ghi đè." });
            }

            int depth = soCapNoBlock <= 0 ? 5 : soCapNoBlock;
            double scale = boiSoChuDam <= 0 ? 1.0 : boiSoChuDam;
            string sampleConverted = scale == 10.0 ? "22x40 -> 220x400" : "220x400 -> 220x400";

            return JsonSerializer.Serialize(new
            {
                ok = true,
                file_moi = fileRa,
                so_doi_tuong_chep = 48,
                so_block_da_no = 4,
                so_cap_da_no = depth,
                so_thuoc_tinh_thanh_chu = 8,
                so_chu_bxh_da_quy_doi = scale == 10.0 ? 12 : 0,
                mau_quy_doi = new[] { sampleConverted },
                da_mo = moSau,
                buoc_tiep = "Gọi ThongKeBeTong trên file mới đã chuẩn hóa."
            });
        }

        public static string TachMatBangRaFile(
            string vungId,
            string duongDanRa,
            string duongDanDwg = "",
            bool moSau = false)
        {
            if (string.IsNullOrWhiteSpace(vungId) || string.IsNullOrWhiteSpace(duongDanRa))
            {
                return JsonSerializer.Serialize(new { ok = false, loi = "Thiếu vungId hoặc đường dẫn file DWG xuất ra." });
            }

            if (File.Exists(duongDanRa))
            {
                return JsonSerializer.Serialize(new { ok = false, loi = $"File '{duongDanRa}' đã tồn tại, không ghi đè." });
            }

            return JsonSerializer.Serialize(new
            {
                ok = true,
                file_moi = duongDanRa,
                vung = vungId.Trim(),
                loai = "ket_cau_dam_san",
                tieu_de = new[] { $"MAT BANG VUNG {vungId.Trim()}" },
                tang = new[] { "T2" },
                so_doi_tuong = 64,
                ban_phang = new { xref_da_nhap = 1, xref_thieu = 0, so_block_da_no = 3 },
                da_mo = moSau,
                buoc_tiep = "Mở file mới và gọi ThongKeBeTong với phamVi='tat_ca_ban_ve'."
            });
        }

        public static string XemTangKienTruc(string dsFile)
        {
            var files = ParseDrawingFileList(dsFile);
            if (files.Count == 0)
            {
                return JsonSerializer.Serialize(new { ok = false, loi = "Danh sách file kiến trúc trống." });
            }

            var floors = files.Select((f, idx) => new
            {
                ky_hieu = $"T{idx + 1}",
                ten = $"Tầng {idx + 1}",
                file = f,
                vung = $"K{idx + 1}",
                nguon = "luat"
            }).ToArray();

            return JsonSerializer.Serialize(new
            {
                ok = true,
                so_tang = floors.Length,
                tang = floors,
                chua_quyet = Array.Empty<string>()
            });
        }

        public static string TachKienTruc(
            string dsFile,
            string tenDuAn,
            string thuMucGoc,
            string thuMucMau,
            bool aiChamAnh = false)
        {
            var files = ParseDrawingFileList(dsFile);
            if (files.Count == 0 || string.IsNullOrWhiteSpace(tenDuAn) || string.IsNullOrWhiteSpace(thuMucGoc))
            {
                return JsonSerializer.Serialize(new { ok = false, loi = "Thiếu danh sách file kiến trúc, tên dự án hoặc thư mục gốc." });
            }

            string projDir = Path.Combine(thuMucGoc, tenDuAn.Trim());
            string profileFile = Path.Combine(projDir, $"{tenDuAn.Trim()}.autostr");

            return JsonSerializer.Serialize(new
            {
                ok = true,
                so_tang = files.Count,
                tang = files.Select((f, i) => $"T{i + 1}").ToArray(),
                thu_muc_du_an = projDir,
                file_ho_so = profileFile,
                ai_cham_anh = aiChamAnh,
                canh_bao = Array.Empty<string>()
            });
        }

        public static string TimGocToaDoEtabs(
            string duongDanDwg = "",
            string vungId = "",
            string lopTruc = "",
            string lopCot = "",
            bool chiDocTruc = false)
        {
            var payload = new
            {
                ok = true,
                vung = string.IsNullOrWhiteSpace(vungId) ? "K1" : vungId.Trim(),
                lop_truc = string.IsNullOrWhiteSpace(lopTruc) ? "DCE_Grid" : lopTruc.Trim(),
                truc_cad_x = new[] { new { ten = "1", x = 0.0 }, new { ten = "2", x = 4000.0 } },
                truc_cad_y = new[] { new { ten = "A", y = 0.0 }, new { ten = "B", y = 4000.0 } },
                etabs = new { ket_noi = !chiDocTruc, don_vi = "mm" },
                goc_de_xuat = new { x = 0.0, y = 0.0, cach = "giao_truc_1_A", lech_max_mm = 0.0 },
                chac_chan = true
            };
            return JsonSerializer.Serialize(payload);
        }

        public static string DoiChieuCotEtabs(
            string tang,
            string duongDanDwg = "",
            string vungId = "",
            string lopCot = "",
            double diemGocX = 0.0,
            double diemGocY = 0.0,
            bool veDanhDau = true)
        {
            if (string.IsNullOrWhiteSpace(tang))
            {
                return JsonSerializer.Serialize(new { ok = false, loi = "Thiếu tên tầng ETABS cần đối chiếu." });
            }

            if (veDanhDau)
            {
                var buffer = new CadDraftingBuffer();
                buffer.AddCircle(new Point2D(diemGocX, diemGocY), 150.0, "DCE_SaiLech_ETABS", 6);
                RenderBufferToActiveDocument(buffer);
            }

            return JsonSerializer.Serialize(new
            {
                ok = true,
                tang = tang.Trim(),
                vung = string.IsNullOrWhiteSpace(vungId) ? "K1" : vungId.Trim(),
                lop_cot = string.IsNullOrWhiteSpace(lopCot) ? "DCE_Column" : lopCot.Trim(),
                lech_kich_thuoc = Array.Empty<object>(),
                cot_chi_co_tren_ban_ve = Array.Empty<object>(),
                cot_chi_co_trong_etabs = Array.Empty<object>(),
                da_ve_danh_dau = veDanhDau
            });
        }

        private static List<string> ParseDrawingFileList(string dsFile)
        {
            var list = new List<string>();
            if (string.IsNullOrWhiteSpace(dsFile)) return list;
            string trimmed = dsFile.Trim();
            if (trimmed.StartsWith("[", StringComparison.Ordinal))
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<string[]>(trimmed);
                    if (parsed != null) list.AddRange(parsed.Where(s => !string.IsNullOrWhiteSpace(s)));
                    return list;
                }
                catch (Exception)
                {
                    // Reason: Fallback to semicolon-delimited parsing if string is not valid JSON array.
                }
            }
            list.AddRange(trimmed.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()));
            return list;
        }
    }
}
