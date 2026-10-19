(defun c:dump_tables ( / fn f tbl item name)
  (setq fn "g:\\09-PROJECT AI\\01_Revit\\02_CshapRevit\\01_AddinRebar\\.agents\\teamwork\\teamwork_preview_explorer_m1_3\\dwg_dump.txt")
  (setq f (open fn "w"))
  
  (write-line "=== LAYERS ===" f)
  (setq tbl (tblnext "LAYER" t))
  (while tbl
    (write-line (strcat "LAYER: " (cdr (assoc 2 tbl)) " | Color: " (itoa (cdr (assoc 62 tbl))) " | Linetype: " (cdr (assoc 6 tbl))) f)
    (setq tbl (tblnext "LAYER"))
  )
  
  (write-line "=== DIMSTYLES ===" f)
  (setq tbl (tblnext "DIMSTYLE" t))
  (while tbl
    (write-line (strcat "DIMSTYLE: " (cdr (assoc 2 tbl))) f)
    (setq tbl (tblnext "DIMSTYLE"))
  )
  
  (write-line "=== TEXT STYLES ===" f)
  (setq tbl (tblnext "STYLE" t))
  (while tbl
    (write-line (strcat "STYLE: " (cdr (assoc 2 tbl)) " | Font: " (cdr (assoc 3 tbl)) " | Height: " (rtos (cdr (assoc 40 tbl)) 2 2) " | Width: " (rtos (cdr (assoc 41 tbl)) 2 2)) f)
    (setq tbl (tblnext "STYLE"))
  )
  
  (write-line "=== BLOCKS ===" f)
  (setq tbl (tblnext "BLOCK" t))
  (while tbl
    (setq name (cdr (assoc 2 tbl)))
    (write-line (strcat "BLOCK: " name) f)
    (setq tbl (tblnext "BLOCK"))
  )
  
  (close f)
  (princ "\nDONE DUMPING TABLES\n")
  (command "QUIT" "Y")
)
(c:dump_tables)
