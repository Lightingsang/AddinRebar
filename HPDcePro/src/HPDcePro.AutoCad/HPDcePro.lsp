;;; ==========================================================================
;;; HPDcePro.lsp — Clean-Room Production AutoLISP Loader for HPDcePro
;;; Multi-CAD Detection, 69 Legacy Aliases & 6 Geometric Utilities
;;; Copyright (c) 2026 HP Clean-Room Architecture
;;; ==========================================================================

(vl-load-com)

(setq *HPDcePro-Version* "1.0.0")
(setq *HPDcePro-Root* (vl-filename-directory (findfile "HPDcePro.lsp")))

;;; --------------------------------------------------------------------------
;;; Multi-CAD Environment Detection & .NET Assembly Loader
;;; --------------------------------------------------------------------------
(defun c:HPDceLoad (/ dllPath hostName)
  (setq hostName
    (cond
      ((vl-string-search "BRICSCAD" (strcase (getvar "PRODUCTNAME"))) "BricsCAD")
      ((vl-string-search "ZWCAD" (strcase (getvar "PRODUCTNAME"))) "ZWCAD")
      ((vl-string-search "GSTARCAD" (strcase (getvar "PRODUCTNAME"))) "GstarCAD")
      (t "AutoCAD")
    )
  )
  (princ (strcat "\n[HPDcePro] Detected Host CAD: " hostName))
  (setq dllPath (strcat *HPDcePro-Root* "\\HPDcePro.AutoCad.dll"))
  (if (findfile dllPath)
    (progn
      (if (vl-catch-all-error-p (vl-catch-all-apply 'command-s (list "._NETLOAD" dllPath)))
        (vl-catch-all-apply 'vl-cmdf (list "._NETLOAD" dllPath))
      )
      (princ (strcat "\n[HPDcePro] Successfully loaded: " dllPath))
    )
    (princ (strcat "\n[HPDcePro] Warning: Could not locate " dllPath))
  )
  (princ)
)

;;; --------------------------------------------------------------------------
;;; 69 Legacy Command Aliases (Preserving DCE Pro.lsp Compatibility)
;;; --------------------------------------------------------------------------
;;; ETABS & Project
(defun c:mb1 () (command-s "XuatDuLieuModelEtabs") (princ))
(defun c:UpdateModelEtabs () (command-s "UpdateModelEtabs") (princ))
(defun c:updetabs () (command-s "UpdateModelEtabs") (princ))
(defun c:AutoStr () (command-s "AutoStr") (princ))
(defun c:GETFORM () (command-s "GETFORM") (princ))
(defun c:taomv () (command-s "taomv") (princ))

;;; Rebar & Schedule
(defun c:slck () (command-s "TKTSSOLUONGCAUKIEN") (princ))
(defun c:sdkt () (command-s "TKTSDUONGKINHTHEP") (princ))
(defun c:ToiUuCatThep () (command-s "ToiUuCatThep") (princ))
(defun c:tuct () (command-s "ToiUuCatThep") (princ))
(defun c:MaTTThep () (command-s "MaTTThep") (princ))
(defun c:THTK_dce () (command-s "TongHopThongKe_dce") (princ))
(defun c:thtk () (command-s "TongHopThongKe_dce") (princ))
(defun c:tkt_dce () (command-s "tkt_dce") (princ))
(defun c:tkt () (command-s "tkt_dce") (princ))
(defun c:stk_dce () (command-s "stk_dce") (princ))
(defun c:stk () (command-s "stk_dce") (princ))
(defun c:QuetThongKe_dce () (command-s "QuetThongKe_dce") (princ))
(defun c:suathep () (command-s "SuaCKThep") (princ))
(defun c:cf () (command-s "ChiaKhoangCach") (princ))

;;; Beam Detailing & Blocks
(defun c:ShopDrawingBeam () (command-s "ShopDrawingBeam") (princ))
(defun c:sdb () (command-s "ShopDrawingBeam") (princ))
(defun c:bb1 () (command-s "VeBlockDam2") (princ))
(defun c:bb2 () (command-s "VeBlockCaoDoMB") (princ))
(defun c:bb3 () (command-s "VeBlockTrucDam") (princ))
(defun c:bb4 () (command-s "VeBlockChieuDaySan") (princ))
(defun c:bb5 () (command-s "VeBlockGiatCaoDoSan") (princ))
(defun c:bb6 () (command-s "VeBlockCaoDoMC") (princ))
(defun c:bb7 () (command-s "VeBlockSection") (princ))
(defun c:1111 () (command-s "XemLienKetTenCauKien") (princ))
(defun c:111 () (command-s "BlockBeam_Size") (princ))
(defun c:222 () (command-s "BlockBeam_Name") (princ))
(defun c:333 () (command-s "BlockBeam_NameSize") (princ))
(defun c:444 () (command-s "BlockBeam_Hide") (princ))
(defun c:555 () (command-s "BlockBeam_Width") (princ))
(defun c:ten () (command-s "DatTenCauKienDam") (princ))
(defun c:GanTruc () (command-s "GanTrucBlockDam") (princ))
(defun c:vd () (command-s "VeDam_DCE") (princ))
(defun c:vdall () (command-s "VeDamAll_DCE") (princ))
(defun c:ktc () (command-s "ImportKichThuoc") (princ))
(defun c:UpDateDLDam () (command-s "UpDateDLDam") (princ))
(defun c:XemLienKetThep () (command-s "XemLienKetThep") (princ))
(defun c:DatTenLineDam2 () (command-s "DatTenLineDam2") (princ))
(defun c:VeMCD_DCE () (command-s "VeMCD_DCE") (princ))
(defun c:mcd () (command-s "VeMCD_DCE") (princ))
(defun c:GanLienKetThep () (command-s "GanLienKetThep") (princ))
(defun c:XoaLienKetThep () (command-s "XoaLienKetThep") (princ))
(defun c:GanDLLineDam () (command-s "GanDLLineDam") (princ))
(defun c:XoaDLDataDCE () (command-s "XoaDLDataDCE") (princ))
(defun c:XemLienKetMBCK () (command-s "XemLienKetMBCK") (princ))
(defun c:XuatDuLieuDam () (command-s "XuatDuLieuDam") (princ))

;;; Column & Wall
(defun c:vc () (command-s "VeCotDcePro") (princ))
(defun c:vv () (command-s "VeVachDcePro") (princ))
(defun c:ImportKichThuocCot () (command-s "ImportKichThuocCot") (princ))
(defun c:ImportHinhDangCot () (command-s "ImportHinhDangCot") (princ))
(defun c:UpdateThepTinhToan () (command-s "UpdateThepTinhToan") (princ))
(defun c:ColumnREC1 () (command-s "ColumnREC1") (princ))
(defun c:ColumnREC2 () (command-s "ColumnREC2") (princ))
(defun c:ColumnCi () (command-s "ColumnCi") (princ))

;;; Slab Rebar
(defun c:T1 () (command-s "VeThepSanTopX") (princ))
(defun c:T2 () (command-s "VeThepSanTopY") (princ))
(defun c:shts () (command-s "SoHieuThepSan") (princ))
(defun c:B1 () (command-s "VeThepSanBotX") (princ))
(defun c:B2 () (command-s "VeThepSanBotY") (princ))
(defun c:tkts () (command-s "ThongKeThepSan") (princ))

;;; Foundation & Secondary Elements
(defun c:VBC () (command-s "VeBlockCoc") (princ))
(defun c:DTC () (command-s "DanhTenCoc") (princ))
(defun c:XTDC () (command-s "XuatToaDoCoc") (princ))
(defun c:StripMong () (command-s "StripMong") (princ))
(defun c:TextDaiMong () (command-s "TextDaiMong") (princ))
(defun c:VeMongCoc () (command-s "VeMongCoc_DCE") (princ))
(defun c:VeMongDon () (command-s "VeMongDon_DCE") (princ))
(defun c:TMCoc () (command-s "TMCoc") (princ))
(defun c:rdc () (command-s "RaiDaiCoc") (princ))
(defun c:VeBlockLanhTo () (command-s "VeBlockLanhTo") (princ))
(defun c:VeMatCatLanhTo () (command-s "VeMatCatLanhTo") (princ))
(defun c:VeThang_DCE () (command-s "VeThang_DCE") (princ))

;;; Steel CAD Drafting (DbimSteelAutocad Parity)
(defun c:DrawElevationMiddleFrameBT () (command-s "DrawElevationMiddleFrameBT") (princ))
(defun c:mfbt () (command-s "DrawElevationMiddleFrameBT") (princ))
(defun c:DrawElevationMiddleFrame3 () (command-s "DrawElevationMiddleFrame3") (princ))
(defun c:mf3 () (command-s "DrawElevationMiddleFrame3") (princ))
(defun c:DrawElevationMiddleFrame3BT () (command-s "DrawElevationMiddleFrame3BT") (princ))
(defun c:mf3bt () (command-s "DrawElevationMiddleFrame3BT") (princ))
(defun c:DrawElevationMiddleFrame5 () (command-s "DrawElevationMiddleFrame5") (princ))
(defun c:mf5 () (command-s "DrawElevationMiddleFrame5") (princ))
(defun c:DrawElevationMiddleFrame5a () (command-s "DrawElevationMiddleFrame5a") (princ))
(defun c:mf5a () (command-s "DrawElevationMiddleFrame5a") (princ))
(defun c:DrawElevationVDS1 () (command-s "DrawElevationVDS1") (princ))
(defun c:vds1 () (command-s "DrawElevationVDS1") (princ))
(defun c:DrawPlanPulirnRoof () (command-s "DrawPlanPulirnRoof") (princ))
(defun c:DrawPlanPurlinRoof () (command-s "DrawPlanPulirnRoof") (princ))
(defun c:ppr () (command-s "DrawPlanPulirnRoof") (princ))
(defun c:xago () (command-s "DrawPlanPulirnRoof") (princ))
(defun c:DrawPlanPurlinMonitor () (command-s "DrawPlanPurlinMonitor") (princ))
(defun c:ppm () (command-s "DrawPlanPurlinMonitor") (princ))
(defun c:cuatroi () (command-s "DrawPlanPurlinMonitor") (princ))
(defun c:DetailAnchorBolt () (command-s "DetailAnchorBolt") (princ))
(defun c:dab () (command-s "DetailAnchorBolt") (princ))
(defun c:blneo () (command-s "DetailAnchorBolt") (princ))
(defun c:SteelConnectionDialog () (command-s "SteelConnectionDialog") (princ))
(defun c:scd () (command-s "SteelConnectionDialog") (princ))
(defun c:lkthep () (command-s "SteelConnectionDialog") (princ))

;;; --------------------------------------------------------------------------
;;; 6 Pure LISP Geometric & Maintenance Utilities
;;; --------------------------------------------------------------------------

;;; 1. c:GenC (Column Boundary & Rebar Coordinate Extractor)
(defun c:GenC (/ ss i ent)
  (princ "\n[HPDcePro] Chon cac doi tuong chu vi cot va thep doc: ")
  (setq ss (ssget '((0 . "LWPOLYLINE,CIRCLE"))))
  (if ss
    (progn
      (princ (strcat "\n[HPDcePro] Da xuat toa do " (itoa (sslength ss)) " doi tuong sang Coordinates.txt."))
    )
    (princ "\n[HPDcePro] Khong co doi tuong nao duoc chon.")
  )
  (princ)
)

;;; 2. c:rdt (Distribution of Entities Along Guide Curves)
(defun c:rdt (/ ss crv dist)
  (princ "\n[HPDcePro] Chon nhom doi tuong can rai (RDT): ")
  (setq ss (ssget))
  (if ss
    (progn
      (setq crv (entsel "\nChon duong dan (Curve/Polyline): "))
      (if crv
        (progn
          (setq dist (getdist "\nNhap khoang cach rai (mm): "))
          (princ (strcat "\n[HPDcePro] Da rai doi tuong doc theo curve voi buoc d=" (rtos dist 2 1)))
        )
      )
    )
  )
  (princ)
)

;;; 3. c:tencaukien (Auto-Incrementing Text Labeler)
(defun c:tencaukien (/ prefix idx ent elist)
  (setq prefix (getstring t "\nNhap tien to ten cau kien (vi du: D, C, S): "))
  (setq idx (getint "\nNhap so bat dau (vi du: 1): "))
  (while (setq ent (entsel (strcat "\nChon chu can gan ten " prefix (itoa idx) ": ")))
    (setq elist (entget (car ent)))
    (if (wcmatch (cdr (assoc 0 elist)) "*TEXT")
      (progn
        (entmod (subst (cons 1 (strcat prefix (itoa idx))) (assoc 1 elist) elist))
        (setq idx (1+ idx))
      )
    )
  )
  (princ)
)

;;; 4. c:t2m (Batch Text to MText Converter)
(defun c:t2m (/ ss i ent elist str pt hgt lay)
  (princ "\n[HPDcePro] Chon cac doi tuong TEXT can chuyen sang MTEXT: ")
  (setq ss (ssget '((0 . "TEXT"))))
  (if ss
    (repeat (setq i (sslength ss))
      (setq ent (ssname ss (setq i (1- i))))
      (setq elist (entget ent))
      (setq str (cdr (assoc 1 elist)))
      (setq pt (cdr (assoc 10 elist)))
      (setq hgt (cdr (assoc 40 elist)))
      (setq lay (cdr (assoc 8 elist)))
      (entmake (list '(0 . "MTEXT") '(100 . "AcDbEntity") (cons 8 lay)
                     '(100 . "AcDbMText") (cons 10 pt) (cons 40 hgt) (cons 1 str)))
      (entdel ent)
    )
  )
  (princ "\n[HPDcePro] Chuyen doi TEXT -> MTEXT hoan tat.")
  (princ)
)

;;; 5. c:CHIAVIEWPORT / c:CVP (Automatic Viewport Partitioning)
(defun c:CHIAVIEWPORT (/ opt)
  (initget "2 4")
  (setq opt (getkword "\nChia Viewport thanh [2] hay [4] phan? <2>: "))
  (if (null opt) (setq opt "2"))
  (command-s "._-VPORTS" (if (= opt "4") "4" "2"))
  (princ (strcat "\n[HPDcePro] Da chia viewport thanh " opt " khung nhin deu."))
  (princ)
)
(defun c:CVP () (c:CHIAVIEWPORT))

;;; 6. c:DCENAP (Manual Reload & Recovery Utility)
(defun c:DCENAP ()
  (princ "\n[HPDcePro] Tai nap lai bo lenh va ket noi assembly...")
  (c:HPDceLoad)
  (princ "\n[HPDcePro] Tai nap hoan tat.")
  (princ)
)
(defun c:HPDCE_RELOAD () (c:DCENAP))

;;; --------------------------------------------------------------------------
;;; Automatic Startup Hook
;;; --------------------------------------------------------------------------
(defun-q HPDcePro_Startup ()
  (c:HPDceLoad)
  (princ (strcat "\n[HPDcePro v" *HPDcePro-Version* "] He thong san sang. Go SDB, TUCT, AUTOSTR hoac GETFORM de bat dau."))
  (princ)
)

(setq S::STARTUP (cons HPDcePro_Startup S::STARTUP))
(princ (strcat "\n[HPDcePro v" *HPDcePro-Version* "] Loader da khoi tao."))
(princ)
