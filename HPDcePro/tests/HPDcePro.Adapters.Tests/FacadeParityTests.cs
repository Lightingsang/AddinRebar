using System;
using System.IO;
using HPDcePro.AutoCad.Facade;
using Xunit;

namespace HPDcePro.Adapters.Tests
{
    public class FacadeParityTests
    {
        [Fact]
        public void MbkcFacade_AuditAndTakeoffMethods_ReturnValidJsonPayloads()
        {
            string ver = MbkcFacade.PhienBan();
            Assert.Contains("\"mat_tien\":\"MatTienMbkc\"", ver);

            string summary = MbkcFacade.TomTatMatBang("MBKC_TANG2.dwg");
            Assert.Contains("\"ok\":true", summary);

            string audit = MbkcFacade.SoatMatBang("MBKC_TANG2.dwg");
            Assert.Contains("\"truc\"", audit);

            string openAudit = MbkcFacade.SoatMatBangDangMo();
            Assert.Contains("\"ban_ve_dang_mo\":true", openAudit);

            string part = MbkcFacade.LietKePhan("MBKC_TANG2.dwg", "", "cot", 0, 10);
            Assert.Contains("\"phan\":\"cot\"", part);

            string invalidPart = MbkcFacade.LietKePhan("MBKC_TANG2.dwg", "", "khong_hop_le");
            Assert.Contains("\"ok\":false", invalidPart);

            string warnings = MbkcFacade.CanhBaoMatBang("MBKC_TANG2.dwg");
            Assert.Contains("\"so_canh_bao\":0", warnings);

            string catProp = MbkcFacade.DeXuatTietDienTheoDanhMuc("[{\"vai\":\"chinh\",\"b\":220,\"h\":500}]", 16.0);
            Assert.Contains("\"de_xuat\"", catProp);

            string takeoff = MbkcFacade.ThongKeBeTong(0, 0, "DCE_Beam", "DCE_Column", "DCE_Slab", 120.0, "tat_ca_ban_ve");
            Assert.Contains("\"tong_be_tong_m3\"", takeoff);

            string texts = MbkcFacade.DocToanBoChuBanVe("Model", 50);
            Assert.Contains("\"tong_so_dong_chu\"", texts);
        }

        [Fact]
        public void MbkcFacade_NormalizeAutoStrAndExcelBeamMethods_ReturnValidJsonPayloads()
        {
            string rules = MbkcFacade.KiemTraLuatMatBang("MBKC_TANG2.dwg");
            Assert.Contains("\"so_loi\":0", rules);

            string fndRules = MbkcFacade.KiemTraLuatMong("project.autostr");
            Assert.Contains("\"so_loi\":0", fndRules);

            string recog = MbkcFacade.NhanDangMatBangKetCau("MBKC_TANG2.dwg", "K1");
            Assert.Contains("\"so_vung\":1", recog);

            string outDwg = Path.Combine(Path.GetTempPath(), $"norm_{Guid.NewGuid():N}.dwg");
            string norm = MbkcFacade.ChuanHoaMatBangTuVungChon("chon", outDwg, 10.0, 5, false);
            Assert.Contains("\"so_chu_bxh_da_quy_doi\":12", norm);

            string splitFile = MbkcFacade.TachMatBangRaFile("K1", outDwg, "MBKC_TANG2.dwg", false);
            Assert.Contains("\"vung\":\"K1\"", splitFile);

            string archView = MbkcFacade.XemTangKienTruc("KT_T1.dwg;KT_T2.dwg");
            Assert.Contains("\"so_tang\":2", archView);

            string archSplit = MbkcFacade.TachKienTruc("KT_T1.dwg;KT_T2.dwg", "DuAnA", Path.GetTempPath(), "MauAutoStr", false);
            Assert.Contains("\"thu_muc_du_an\"", archSplit);

            string etabsOrigin = MbkcFacade.TimGocToaDoEtabs("MBKC_TANG2.dwg", "K1", "DCE_Grid", "DCE_Column", true);
            Assert.Contains("\"goc_de_xuat\"", etabsOrigin);

            string etabsCol = MbkcFacade.DoiChieuCotEtabs("T2", "MBKC_TANG2.dwg", "K1", "DCE_Column", 0, 0, true);
            Assert.Contains("\"da_ve_danh_dau\":true", etabsCol);

            string loadBeam = MbkcFacade.NapMotDamVaoSheetVe("T2B1");
            Assert.Contains("\"ten_dam_yeu_cau\":\"T2B1\"", loadBeam);

            string beamList = MbkcFacade.DocDanhSachDamVe("");
            Assert.Contains("\"van_tay\"", beamList);

            string exportBeam = MbkcFacade.XuatDamDeVe("tat_ca_ban_ve", "T2B1", 1);
            Assert.Contains("\"ten_dam_yeu_cau\":\"T2B1\"", exportBeam);

            string drawLoaded = MbkcFacade.VeDamDangNap(100.0, 200.0, "ZOOM E");
            Assert.Contains("\"so_doi_tuong_da_ve\"", drawLoaded);
        }

        [Fact]
        public void DceSteelFacade_MetaQueryAndSettingsMethods_ReturnValidJsonPayloads()
        {
            string ver = DceSteelFacade.PhienBan();
            Assert.Contains("\"mat_tien\":\"MatTienThep\"", ver);

            string built = DceSteelFacade.DungChuoiXDataThongKe("D1", "1", "T1", "18", "1", "4");
            Assert.Contains("(0)_D1(1)_1", built);

            string cell3 = DceSteelFacade.LayOXData("(0)_D1(1)_1(2)_T1(3)_18(4)_1(5)_4", 3);
            Assert.Equal("18", cell3);

            string decoded = DceSteelFacade.GiaiMaChuoiXData("(0)_D1(1)_1(2)_T1(3)_18(4)_1(5)_4");
            Assert.Contains("\"ten_cau_kien\":\"D1\"", decoded);

            string xdataThep = DceSteelFacade.DocXDataThep("2A3F");
            Assert.Contains("\"co_xdata\":true", xdataThep);

            string xdataTk = DceSteelFacade.DocXDataThietKe("2A3F");
            Assert.Contains("\"co_xdata\":true", xdataTk);

            string xdataAll = DceSteelFacade.DocXDataThoTatCaRegapp("2A3F");
            Assert.Contains("\"so_regapp\":2", xdataAll);

            string tagText = DceSteelFacade.DocChuTagThep("D1", 10, "");
            Assert.Contains("\"so_tag\":1", tagText);

            string colorStats = DceSteelFacade.ThongKeTheoDuongKinhVaMau(10, "");
            Assert.Contains("\"co_bang_mau\":true", colorStats);

            string settingsList = DceSteelFacade.LietKeCaiDat("LopBaoVe", "Chung", 10);
            Assert.Contains("DceSetup_LopBaoVeDam", settingsList);

            string writeSetting = DceSteelFacade.GhiCaiDat("DceSetup_LopBaoVeDam", "30");
            Assert.Contains("\"gia_tri\":\"30\"", writeSetting);

            string readSetting = DceSteelFacade.DocCaiDat("DceSetup_LopBaoVeDam");
            Assert.Contains("\"gia_tri\":\"30\"", readSetting);

            string previewReplace = DceSteelFacade.XemTruocTimThayTheXData("10A,10B", "(0)_D1(1)_", "(0)_D2(1)_", 10);
            Assert.Contains("\"so_se_thay\":2", previewReplace);
        }

        [Fact]
        public void DceSteelFacade_BatchEditingAndLayoutMethods_ReturnValidJsonPayloads()
        {
            Assert.Contains("\"ten_moi\":\"D2\"", DceSteelFacade.DoiTenCauKien("10A,10B", "D2"));
            Assert.Contains("\"dk_moi\":\"20\"", DceSteelFacade.SuaDuongKinhThep("10A,10B", "18", "20"));
            Assert.Contains("\"so_luong_cau_kien_moi\":\"3\"", DceSteelFacade.SuaSoLuongCauKien("10A,10B", "3"));
            Assert.Contains("\"kr_moi\":\"150\"", DceSteelFacade.SuaKhoangRaiThep("10A,10B", "200", "150"));
            Assert.Contains("\"gia_tri\":\"5600\"", DceSteelFacade.SuaChieuDaiChamThep("10A", "L", "5600"));
            Assert.Contains("\"so_doi_tuong\":2", DceSteelFacade.CapNhapSoLuongDai("10A,10B"));
            Assert.Contains("\"ok\":true", DceSteelFacade.TimThayTheXData("10A,10B", "(0)_D1(1)_", "(0)_D2(1)_"));
            Assert.Contains("\"xoa_thep_kep\":false", DceSteelFacade.DatThepKep("10A,10B", "2"));
            Assert.Contains("\"so_doi_tuong_lien_ket\":2", DceSteelFacade.XoaLienKetThep("10A", "20A,20B"));
            Assert.Contains("\"lenh_goc\":\"CopySoHieuThep\"", DceSteelFacade.CopySoHieuThep("10A", "10B,10C"));
            Assert.Contains("\"lenh_goc\":\"MaTTThep\"", DceSteelFacade.CopyDuLieuThep("10A", "10B,10C"));
            Assert.Contains("\"lenh_goc\":\"CopyKhoangRaiThep\"", DceSteelFacade.CopyKhoangRaiThep("10A", "10B,10C"));
            Assert.Contains("\"loai\":\"T\"", DceSteelFacade.DoiLoaiThep("10A,10B", "T"));
            Assert.Contains("\"ten_moi\":\"D3\"", DceSteelFacade.SuaTenCauKien("10A,10B", "D3"));
            Assert.Contains("\"so_thanh_cong\":2", DceSteelFacade.ResetPolyline("10A,10B"));
            Assert.Contains("\"lenh_goc\":\"SuaCKThep\"", DceSteelFacade.SuaThepDoc("10A", "D1", "1", "20", "1", "4", ""));
            Assert.Contains("\"nut_goc\":\"RebarColorBylayer\"", DceSteelFacade.TraMauThepVeByLayer());
            Assert.Contains("\"nut_goc\":\"ApplyToRebar\"", DceSteelFacade.DoiMauThepTheoDuongKinh());
            Assert.Contains("\"lenh_goc\":\"GTS2_DCE\"", DceSteelFacade.SuaLoiLienKetThepSan("10A,10B"));
            Assert.Contains("\"lenh_goc\":\"GTextThep_DCE\"", DceSteelFacade.GanLienKetTagThep("10A,20A", true));
            Assert.Contains("\"so_thanh_tag\":3", DceSteelFacade.TagNhieuThep("10A,10B,10C", "FULL", 25.0));
            Assert.Contains("\"so_thanh\":2", DceSteelFacade.TagThepTuDong("10A,10B", 25.0));
            Assert.Contains("\"so_doi_tuong\":2", DceSteelFacade.RebarLayoutTheoHandle("10A,30A"));
            Assert.Contains("\"so_cau_kien\":2", DceSteelFacade.LietKeCauKienTrongBanVe(10, ""));
            Assert.Contains("\"than_va_tag\"", DceSteelFacade.LietKeThepTheoCauKien("D1", 10, ""));
            Assert.Contains("\"so_block\":1", DceSteelFacade.LietKeBlockThongKeThep(10));
            Assert.Contains("\"da_chay\":true", DceSteelFacade.ChayThongKeThep(false, true, true, 0, "tat_ca_ban_ve"));
            Assert.Contains("\"da_chay\":true", DceSteelFacade.TongHopThep("3B10", 0, 0));
            Assert.Contains("\"ok\":true", DceSteelFacade.XuatThongKeExcel("3B10", "AThongKeThep", "True", false));
            Assert.Contains("\"so_block_dung_lai\":1", DceSteelFacade.ResetBangThongKe("3B10"));
        }
    }
}
