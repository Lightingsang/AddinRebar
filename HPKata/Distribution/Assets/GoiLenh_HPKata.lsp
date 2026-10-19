(vl-load-com)
(setq *hpkata-dir* (vl-filename-directory (findfile "GoiLenh_HPKata.lsp")))
(if (not (vl-symbol-value 'c:setupkata1))
  (progn
    (setq *hpkata-dll* (strcat *hpkata-dir* "\\HPKata.AutoCad.dll"))
    (if (not (findfile *hpkata-dll*))
      (setq *hpkata-dll* "C:\\HPKata\\HPKata.AutoCad.dll")
    )
    (if (findfile *hpkata-dll*)
      (command "_.NETLOAD" *hpkata-dll*)
      (command "netload" "HPKata.AutoCad.dll")
    )
  )
)
;;; End HPKata .NETLOAD
(setq tinhtrangmenutienich (menugroup "menukata"))
(cond
  ((= tinhtrangmenutienich nil)
    (cond
      ((>= (atof (getvar 'AcadVer)) 18.0)
        (setq *cuix* (findfile "menukata.cuix"))
        (if *cuix* (command "_cuiload" *cuix*))
      )
      (
        (setq *cui* (findfile "menukata.cui"))
        (if *cui* (command "_cuiload" *cui*))
      )
    )
    (menucmd "P13=+menukata.pop1")
  )
  ((/= tinhtrangmenutienich nil))
)
(defun c:tstt()(command "tang_stt"))
(defun c:rthep()(command "rthep"))
(defun c:lv()(command "ve_level"))
(defun c:lv1()(command "ve_level1"))
(defun c:sst()(command "thong_ke_cap"))
(defun c:dao()(command "dao_so_hieu"))
(defun c:ad()(command "edit_att_data"))
(defun c:cht()(command "doi_text_blockcd"))
(defun c:setupkata()(command "setupkata1"))
;;;;;;;;;;;;;;;;HPKATA@20270924;;;;;;;;;;;;;;;;;;
(defun c:vg()(command "ve_grid"))
(defun c:raicot()(command "raicot"))
(defun c:dimcot()(command "dimcot"))
(defun c:raidam()(command "raidam"))
(defun c:dsan()(command "pick_diem_san"))
(defun c:ssan()(command "ssan"))
(defun c:giaodam()(command "giao_dam"))
(defun c:catdam()(command "cat_dam"))
(defun c:hideatt()(command "hideatt"))
(defun c:Unhideatt()(command "Unhideatt"))
(defun c:tkdam()(command "tkdam"))
(defun c:tkcot()(command "tkcot"))
(defun c:tim_ten_ck()(command "tim_ten_ck"))
(defun c:dten()(command "dten"))
(defun c:tkten()(command "tkten"))
(defun c:textblock()(command "textblock"))
(defun c:chiasan()(command "chia_san"))
(defun c:vmbm()(command "ve_mat_bang_mong"))
(defun c:rm()(command "rai_mong"))
(defun c:vmd()(command "ve_mong_don"))
(defun c:vmc()(command "ve_mong_coc"))
(defun c:vc()(command "ve_cot"))
(defun c:vcs()(command "ve_cot_shop"))
(defun c:vv()(command "ve_vach"))
(defun c:vvs()(command "ve_vach_shop"))
(defun c:vlv()(command "ve_loi_vach"))
(defun c:vd()(command "ve_dam"))
(defun c:vds()(command "ve_dam_shop"))
(defun c:vdm()(command "ve_dam_mong"))
(defun c:vgm()(command "ve_giang_mong"))
(defun c:vgms()(command "ve_giang_shop"))
(defun c:vdmc()(command "ve_dam_mc"))
(defun c:damgiao()(command "dam_giao"))
(defun c:vdx()(command "ve_dam_xien"))
(defun c:vdc()(command "ve_dam_cong"))
(defun c:vesan()(command "ve_san"))
(defun c:vesanpl()(command "vesanpl"))
(defun c:vesan4d()(command "vesan4d"))
(defun c:vemc()(command "vemc"))
(defun c:vethang()(command "ve_thang"))
(defun c:velanhto()(command "velanhto"))
(defun c:loadbeam()(command "loadbeam"))
(defun c:loadcolumn()(command "loadcolumn"))
(defun c:loadwall()(command "loadwall"))
(defun c:qd()(command "quet_dam"))
(defun c:qda()(command "quet_dam_all"))
(defun c:std()(command "Save_thep_dam"))
(defun c:stda()(command "Save_thep_dam_all"))
(defun c:atd()(command "Add_thep_dam"))
(defun c:shtd()(command "show_thep_dam"))
(defun c:vda()(command "ve_dam_all"))
(defun c:vdas()(command "ve_dam_all_shop"))
(defun c:vdmca()(command "ve_damMC_all"))
(defun c:Etabs()(command "Etabs"))
(defun c:vt()(command "ve_thep"))
(defun c:taothep()(command "taothep"))
(defun c:link()(command "tao_lien_ket"))
(defun c:sua()(command "sua_thep"))
(defun c:smd()(command "sua_moc_dai"))
(defun c:dai90()(command "dai90"))
(defun c:dai180()(command "dai180"))
(defun c:dai135()(command "dai135"))
(defun c:dai90135()(command "dai90135"))
(defun c:nhan()(command "nhan_thep"))
(defun c:neo()(command "neo_thep"))
(defun c:ud()(command "update_thep"))
(defun c:dmt()(command "doi_mau_thep"))
(defun c:cl()(command "copy_tao_lien_ket"))
(defun c:dl()(command "xoa_lien_ket"))
(defun c:tim()(command "xem_lien_ket"))
(defun c:xoa()(command "xoa_highlight"))
(defun c:gt()(command "ghi_thep"))
(defun c:cut()(command "cat_thep"))
(defun c:cattuyy()(command "cat_tuy_y"))
(defun c:timthep()(command "timthep"))
(defun c:dimthep()(command "dimthep"))
(defun c:stt0()(command "stt0"))
(defun c:stt1()(command "stt1"))
(defun c:setupthep()(command "setupthep"))
(defun c:tohopthep()(command "to_hop_thep"))
(defun c:tkt()(command "thong_ke_thep"))
(defun c:suatk()(command "sua_thong_ke"))
(defun c:sstk()(command "sstk"))
(defun c:ththep()(command "tong_hop_thep"))
(defun c:tkco()(command "thong_ke_coupler"))
(defun c:tksan()(command "thong_ke_block_san"))
(defun c:toexcel()(command "toexcel"))
(defun c:toexcelqh()(command "toexcelqh"))
(defun c:chuyen()(command "chuyen"))
(defun c:str()(command "show_thep_rai"))
(defun c:check_lap()(command "check_lap"))
(defun c:pukata()(command "pukata"))
(defun c:pdfkata()(command "pdfkata"))
(defun c:itt()(command "in_tem_thep"))
(defun c:QS_mbkc()(command "QS_mbkc"))
(defun c:QS_tru()(command "QS_tru"))
(defun c:QS()(command "QS"))
(defun c:xuatQS()(command "xuatQS"))
(defun c:timqs()(command "timqs"))
(defun c:chiatam()(command "chia_tam"))
(defun c:opgach()(command "op_gach"))
(defun c:locdim()(command "loc_dim"))
(defun c:pvn()(command "phan_vung_noi"))
(defun c:danhmuc()(command "danhmuc"))
(defun c:cat()(command "cat_doituong"))
(defun c:taomv()(command "taomv"))
(defun c:taomv1()(command "taomv1"))
(defun c:sbv()(command "sap_ban_ve"))
(defun c:ibv()(command "in_ban_ve"))
;kata_dim
;kata_net thay
;kata_net khung
;kata_net khuat
;kata_grid
;kata_thep chu
;kata_thep dai
;kata_thep dai
;kata_net manh
;kata_text
;kata_text
;a
;Ø
;Ø
;kata_dim_iso
;30
;135/180
;1/25
;16
;30
;1
;1
;1
;0
;1
;40
;1/6
;1
;0
;0
;Ø
;11700
;100
;0
;0.2
; mép
;0
;0
;0
;1a, 1b, 1c, 1d ...
;1
;5
;0
;-
;7.5
;25
;
;0.25
; mép
;8800
;Tiếng Việt
;0
;0
;
;Default
;
;Closed
;0
;0
;0
;
;0
;
;0
;mm
;50
;0
;0
;1
;0
;5f10a50
;1
;2f14
;150
;5f10a50
;1
;2f14
;150
;10
;50
;0
;None
;None
;0
;0
;0
